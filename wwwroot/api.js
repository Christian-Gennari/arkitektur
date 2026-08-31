async function request(url, options = {}) {
  const response = await fetch(url, options);

  if (!response.ok) {
    throw new Error(`API request failed: ${response.status}`);
  }

  return response.status === 204 ? null : response.json();
}

export function getShipments() {
  return request("/shipments");
}

export function registerShipment(recipient, destination) {
  return request("/shipments", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ recipient, destination }),
  });
}

export function dispatchShipment(id) {
  return request(`/shipments/${id}/dispatch`, { method: "PUT" });
}

export function deliverShipment(id) {
  return request(`/shipments/${id}/deliver`, { method: "PUT" });
}

export function cancelShipment(id) {
  return request(`/shipments/${id}/cancel`, { method: "PUT" });
}

export function getOperationsMetrics() {
  return request("/operations/metrics");
}

export function getTracking(trackingNumber) {
  return request(`/tracking/${encodeURIComponent(trackingNumber)}`);
}
