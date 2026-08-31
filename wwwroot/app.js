import { completeTodo, createTodo, deleteTodo, getStatistics, getTodos } from "./api.js";

let tasks = [];
let currentFilter = "all";

const taskList = document.querySelector("#task-list");
const taskInput = document.querySelector("#task-title");

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

  document.querySelectorAll(".task-check").forEach((button) => button.addEventListener("click", () => completeTask(button.closest(".task-item").dataset.id)));
  document.querySelectorAll(".delete-task").forEach((button) => button.addEventListener("click", () => removeTask(button.closest(".task-item").dataset.id)));

  const completed = tasks.filter((task) => task.completed).length;
  document.querySelector("#task-count").textContent = `${visible.length} ${visible.length === 1 ? "task" : "tasks"}`;
  document.querySelector("#progress-label").textContent = `${tasks.length - completed} open · ${completed} done`;
}

async function completeTask(id) {
  const task = tasks.find((item) => String(item.id) === String(id));
  if (!task || task.completed) return;

  try {
    await completeTodo(id);
    await refresh();
    showToast("Task completed");
  } catch {
    showToast("Could not complete task");
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

const currentDate = new Date();
document.querySelector("#current-date").textContent = currentDate.toLocaleDateString("en-US", { weekday: "long", month: "long", day: "numeric" });
refresh();
