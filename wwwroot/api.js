async function request(url, options = {}) {
  const response = await fetch(url, options);

  if (!response.ok) {
    throw new Error(`API request failed: ${response.status}`);
  }

  return response.status === 204 ? null : response.json();
}

export function getTodos() {
  return request("/todos");
}

export function createTodo(title) {
  return request("/todos", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ title }),
  });
}

export function completeTodo(id) {
  return request(`/todos/${id}/complete`, { method: "PUT" });
}

export function deleteTodo(id) {
  return request(`/todos/${id}`, { method: "DELETE" });
}

export function getStatistics() {
  return request("/statistics");
}
