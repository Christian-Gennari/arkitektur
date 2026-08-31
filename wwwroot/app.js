import { completeTodo, createTodo, deleteTodo, getStatistics, getTodos, uncompleteTodo } from "./api.js";

let tasks = [];
let currentFilter = "all";
let observedEvents = [];

const taskList = document.querySelector("#task-list");
const taskInput = document.querySelector("#task-title");
const eventList = document.querySelector("#event-list");
const eventConnection = document.querySelector("#event-connection");

function toUiTask(todo) {
  return {
    id: todo.id,
    title: todo.title,
    due: "Today",
    project: "Personal",
    completed: todo.isCompleted,
  };
}

async function loadTasks() {
  try {
    tasks = (await getTodos()).map(toUiTask);
    render();
  } catch {
    showToast("Could not load tasks");
  }
}

async function loadStatistics() {
  try {
    const statistics = await getStatistics();
    document.querySelector("#created-stat").textContent = statistics.createdCount;
    document.querySelector("#completed-stat").textContent = statistics.completedCount;
    document.querySelector("#deleted-stat").textContent = statistics.deletedCount;
  } catch {
    showToast("Could not load statistics");
  }
}

async function refresh() {
  await Promise.all([loadTasks(), loadStatistics()]);
}

function visibleTasks() {
  return tasks.filter((task) =>
    currentFilter === "all"
      || (currentFilter === "open" && !task.completed)
      || (currentFilter === "done" && task.completed));
}

function checkIcon() { return '<svg aria-hidden="true" viewBox="0 0 24 24"><path d="m5 12 4.2 4.2L19 6.5" /></svg>'; }
function calendarIcon() { return '<svg aria-hidden="true" viewBox="0 0 24 24"><rect x="3.5" y="5" width="17" height="16" rx="2" /><path d="M7.5 3v4M16.5 3v4M3.5 10h17" /></svg>'; }

function render() {
  const visible = visibleTasks();
  taskList.innerHTML = visible.length ? visible.map((task) => `
    <article class="task-item ${task.completed ? "is-completed" : ""}" data-id="${task.id}">
      <button class="task-check ${task.completed ? "checked" : ""}" type="button" aria-label="${task.completed ? "Completed" : "Mark complete"}: ${escapeHtml(task.title)}">${task.completed ? checkIcon() : ""}</button>
      <div class="task-details"><span class="task-title">${escapeHtml(task.title)}</span><div class="task-meta"><span>${calendarIcon()}${task.due}</span><span class="task-project">${escapeHtml(task.project)}</span></div></div>
      <div class="task-actions"><button class="delete-task" type="button" aria-label="Delete ${escapeHtml(task.title)}" title="Delete task">×</button></div>
    </article>`).join("") : '<div class="empty-state"><strong>Nothing here</strong><span>Add a task above and keep your day moving.</span></div>';

  document.querySelectorAll(".task-check").forEach((button) => button.addEventListener("click", () => toggleTask(button.closest(".task-item").dataset.id)));
  document.querySelectorAll(".delete-task").forEach((button) => button.addEventListener("click", () => removeTask(button.closest(".task-item").dataset.id)));

  const completed = tasks.filter((task) => task.completed).length;
  document.querySelector("#task-count").textContent = `${visible.length} ${visible.length === 1 ? "task" : "tasks"}`;
  document.querySelector("#progress-label").textContent = `${tasks.length - completed} open · ${completed} done`;
}

async function toggleTask(id) {
  const task = tasks.find((item) => String(item.id) === String(id));
  if (!task) return;

  try {
    if (task.completed) {
      await uncompleteTodo(id);
    } else {
      await completeTodo(id);
    }
    await refresh();
    showToast(task.completed ? "Task reopened" : "Task completed");
  } catch {
    showToast("Could not update task");
  }
}

async function removeTask(id) {
  try {
    await deleteTodo(id);
    await refresh();
    showToast("Task deleted");
  } catch {
    showToast("Could not delete task");
  }
}

document.querySelector("#quick-add").addEventListener("submit", async (event) => {
  event.preventDefault();
  const title = taskInput.value.trim();
  if (!title) return;

  try {
    await createTodo(title);
    taskInput.value = "";
    await refresh();
    showToast("Task added");
  } catch {
    showToast("Could not add task");
  }
});

document.querySelectorAll(".filter").forEach((button) => button.addEventListener("click", () => {
  currentFilter = button.dataset.filter;
  document.querySelectorAll(".filter").forEach((item) => {
    const active = item === button;
    item.classList.toggle("active", active);
    item.setAttribute("aria-selected", active);
  });
  render();
}));

document.querySelector("#clear-completed").addEventListener("click", async () => {
  const completed = tasks.filter((task) => task.completed);
  if (!completed.length) return showToast("Nothing to clear");

  try {
    await Promise.all(completed.map((task) => deleteTodo(task.id)));
    await refresh();
    showToast(`${completed.length} ${completed.length === 1 ? "task" : "tasks"} cleared`);
  } catch {
    showToast("Could not clear completed tasks");
  }
});

document.querySelector("#clear-events").addEventListener("click", () => {
  observedEvents = [];
  renderEventMonitor();
});

document.addEventListener("keydown", (event) => {
  if (event.key.toLowerCase() === "n" && document.activeElement.tagName !== "INPUT") {
    event.preventDefault();
    taskInput.focus();
  }
});

function showToast(message) {
  const toast = document.querySelector("#toast");
  toast.textContent = message;
  toast.classList.add("show");
  clearTimeout(showToast.timeout);
  showToast.timeout = setTimeout(() => toast.classList.remove("show"), 1800);
}

function escapeHtml(value) {
  return String(value).replace(/[&<>'"]/g, (character) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", "'": "&#039;", '"': "&quot;" })[character]);
}

function eventStageLabel(stage) {
  return ({
    queued: "Queued",
    processing: "Dispatching",
    "consumer-started": "Consumer running",
    "consumer-completed": "Consumer handled",
    "consumer-failed": "Consumer failed",
    completed: "Handled",
    "completed-with-errors": "Handled with errors",
  })[stage] || stage;
}

function eventStageClass(stage) {
  if (stage === "completed") return "is-complete";
  if (stage === "consumer-failed" || stage === "completed-with-errors") return "is-error";
  if (stage === "queued") return "is-queued";
  return "is-processing";
}

function recordEventTrace(trace) {
  let observed = observedEvents.find((item) => item.eventId === trace.eventId);

  if (!observed) {
    observed = {
      eventId: trace.eventId,
      eventType: trace.eventType,
      recordedAt: trace.recordedAt,
      stage: trace.stage,
      detail: trace.detail,
      consumers: {},
    };
    observedEvents.unshift(observed);
    observedEvents = observedEvents.slice(0, 8);
  }

  observed.stage = trace.stage;
  observed.detail = trace.detail;
  observed.recordedAt = trace.recordedAt;

  if (trace.consumer) {
    observed.consumers[trace.consumer] = trace.stage === "consumer-failed"
      ? "failed"
      : trace.stage === "consumer-completed"
        ? "completed"
        : "running";
  }

  renderEventMonitor();

  if (trace.stage === "completed" || trace.stage === "completed-with-errors") {
    loadStatistics();
  }
}

function renderEventMonitor() {
  if (!observedEvents.length) {
    eventList.innerHTML = '<div class="event-empty"><strong>Waiting for an event</strong><span>Create or update a task to see the pipeline.</span></div>';
    return;
  }

  eventList.innerHTML = observedEvents.map((event) => {
    const consumers = Object.entries(event.consumers);
    const consumerMarkup = consumers.length
      ? consumers.map(([name, state]) => `<span class="consumer-state is-${state}"><i></i>${escapeHtml(name)}</span>`).join("")
      : '<span class="consumer-state"><i></i>Waiting for dispatcher</span>';
    const time = new Date(event.recordedAt).toLocaleTimeString([], {
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit",
    });

    return `
      <article class="event-item ${eventStageClass(event.stage)}">
        <div class="event-item-heading">
          <strong>${escapeHtml(event.eventType)}</strong>
          <time>${time}</time>
        </div>
        <div class="event-meta">
          <code>#${escapeHtml(event.eventId.slice(0, 8))}</code>
          <span>${escapeHtml(eventStageLabel(event.stage))}</span>
        </div>
        <div class="consumer-states">${consumerMarkup}</div>
      </article>`;
  }).join("");
}

function connectEventMonitor() {
  const eventSource = new EventSource("/events/stream");

  eventSource.addEventListener("open", () => {
    eventConnection.className = "connection-status is-connected";
    eventConnection.innerHTML = '<i aria-hidden="true"></i>Live';
  });

  eventSource.addEventListener("event-trace", (message) => {
    recordEventTrace(JSON.parse(message.data));
  });

  eventSource.addEventListener("error", () => {
    eventConnection.className = "connection-status is-connecting";
    eventConnection.innerHTML = '<i aria-hidden="true"></i>Reconnecting';
  });
}

const currentDate = new Date();
document.querySelector("#current-date").textContent = currentDate.toLocaleDateString("en-US", { weekday: "long", month: "long", day: "numeric" });
connectEventMonitor();
refresh();
