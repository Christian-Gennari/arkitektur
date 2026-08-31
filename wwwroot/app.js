const STORAGE_KEY = "tasks-v1";

const starterTasks = [
  { id: 1, title: "Review project brief and define next steps", due: "Today", project: "Work", completed: false },
  { id: 2, title: "Book a table for Friday dinner", due: "Today", project: "Personal", completed: false },
  { id: 3, title: "Read chapter 4 of Atomic Habits", due: "Tomorrow", project: "Learning", completed: false },
  { id: 4, title: "Send the updated presentation to the team", due: "Today", project: "Work", completed: true },
  { id: 5, title: "Plan meals and make a grocery list", due: "This week", project: "Personal", completed: true },
];

let tasks = loadTasks();
let currentFilter = "all";
const taskList = document.querySelector("#task-list");
const taskInput = document.querySelector("#task-title");

function loadTasks() {
  try { return JSON.parse(localStorage.getItem(STORAGE_KEY)) || starterTasks; } catch { return starterTasks; }
}

function saveTasks() { localStorage.setItem(STORAGE_KEY, JSON.stringify(tasks)); }

function visibleTasks() {
  return tasks.filter((task) => currentFilter === "all" || (currentFilter === "open" && !task.completed) || (currentFilter === "done" && task.completed));
}

function checkIcon() { return '<svg aria-hidden="true" viewBox="0 0 24 24"><path d="m5 12 4.2 4.2L19 6.5" /></svg>'; }
function calendarIcon() { return '<svg aria-hidden="true" viewBox="0 0 24 24"><rect x="3.5" y="5" width="17" height="16" rx="2" /><path d="M7.5 3v4M16.5 3v4M3.5 10h17" /></svg>'; }

function render() {
  const visible = visibleTasks();
  taskList.innerHTML = visible.length ? visible.map((task) => `
    <article class="task-item ${task.completed ? "is-completed" : ""}" data-id="${task.id}">
      <button class="task-check ${task.completed ? "checked" : ""}" type="button" aria-label="${task.completed ? "Mark incomplete" : "Mark complete"}: ${escapeHtml(task.title)}">${task.completed ? checkIcon() : ""}</button>
      <div class="task-details"><span class="task-title">${escapeHtml(task.title)}</span><div class="task-meta"><span>${calendarIcon()}${task.due}</span><span class="task-project">${escapeHtml(task.project)}</span></div></div>
      <div class="task-actions"><button class="delete-task" type="button" aria-label="Delete ${escapeHtml(task.title)}" title="Delete task">×</button></div>
    </article>`).join("") : '<div class="empty-state"><strong>Nothing here</strong><span>Add a task above and keep your day moving.</span></div>';

  document.querySelectorAll(".task-check").forEach((button) => button.addEventListener("click", () => toggleTask(button.closest(".task-item").dataset.id)));
  document.querySelectorAll(".delete-task").forEach((button) => button.addEventListener("click", () => deleteTask(button.closest(".task-item").dataset.id)));

  const completed = tasks.filter((task) => task.completed).length;
  document.querySelector("#task-count").textContent = `${visible.length} ${visible.length === 1 ? "task" : "tasks"}`;
  document.querySelector("#progress-label").textContent = `${tasks.length - completed} open · ${completed} done`;
}

function toggleTask(id) {
  const task = tasks.find((item) => String(item.id) === String(id));
  if (!task) return;
  task.completed = !task.completed;
  saveTasks(); render(); showToast(task.completed ? "Task completed" : "Task reopened");
}

function deleteTask(id) {
  tasks = tasks.filter((item) => String(item.id) !== String(id));
  saveTasks(); render(); showToast("Task deleted");
}

document.querySelector("#quick-add").addEventListener("submit", (event) => {
  event.preventDefault();
  const title = taskInput.value.trim();
  if (!title) return;
  tasks.unshift({ id: Date.now(), title, due: "Today", project: "Personal", completed: false });
  saveTasks(); render(); taskInput.value = ""; showToast("Task added");
});

document.querySelectorAll(".filter").forEach((button) => button.addEventListener("click", () => {
  currentFilter = button.dataset.filter;
  document.querySelectorAll(".filter").forEach((item) => {
    const active = item === button;
    item.classList.toggle("active", active); item.setAttribute("aria-selected", active);
  });
  render();
}));

document.querySelector("#clear-completed").addEventListener("click", () => {
  const count = tasks.filter((task) => task.completed).length;
  if (!count) return showToast("Nothing to clear");
  tasks = tasks.filter((task) => !task.completed); saveTasks(); render(); showToast(`${count} ${count === 1 ? "task" : "tasks"} cleared`);
});

document.addEventListener("keydown", (event) => {
  if (event.key.toLowerCase() === "n" && document.activeElement.tagName !== "INPUT") { event.preventDefault(); taskInput.focus(); }
});

function showToast(message) {
  const toast = document.querySelector("#toast");
  toast.textContent = message; toast.classList.add("show"); clearTimeout(showToast.timeout);
  showToast.timeout = setTimeout(() => toast.classList.remove("show"), 1800);
}

function escapeHtml(value) {
  return String(value).replace(/[&<>'"]/g, (character) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", "'": "&#039;", '"': "&quot;" })[character]);
}

const currentDate = new Date();
document.querySelector("#current-date").textContent = currentDate.toLocaleDateString("en-US", { weekday: "long", month: "long", day: "numeric" });
render();
