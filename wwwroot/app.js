import {
  cancelShipment,
  deliverShipment,
  dispatchShipment,
  getOperationsMetrics,
  getShipments,
  getTracking,
  registerShipment,
} from "./api.js";

let shipments = [];
let trackingSnapshots = {};
let currentFilter = "all";
let observedEvents = [];

const shipmentList = document.querySelector("#shipment-list");
const recipientInput = document.querySelector("#shipment-recipient");
const destinationInput = document.querySelector("#shipment-destination");
const eventList = document.querySelector("#event-list");
const eventConnection = document.querySelector("#event-connection");
const flowCaption = document.querySelector("#flow-caption");
const flowNodeIds = [
  "flow-command",
  "flow-producer",
  "flow-queue",
  "flow-dispatcher",
  "flow-tracking",
  "flow-notifications",
  "flow-metrics",
  "flow-audit",
];

async function loadShipments() {
  try {
    shipments = await getShipments();
    await loadTrackingSnapshots();
    renderShipments();
  } catch {
    showToast("Could not load shipments");
  }
}

async function loadTrackingSnapshots() {
  const snapshots = await Promise.all(shipments.map(async (shipment) => {
    try {
      return [shipment.trackingNumber, await getTracking(shipment.trackingNumber)];
    } catch {
      return [shipment.trackingNumber, null];
    }
  }));
  trackingSnapshots = Object.fromEntries(snapshots);
  renderShipments();
}

async function loadMetrics() {
  try {
    const metrics = await getOperationsMetrics();
    document.querySelector("#registered-stat").textContent = metrics.registeredCount;
    document.querySelector("#dispatched-stat").textContent = metrics.dispatchedCount;
    document.querySelector("#delivered-stat").textContent = metrics.deliveredCount;
    document.querySelector("#cancelled-stat").textContent = metrics.cancelledCount;
  } catch {
    showToast("Could not load operations metrics");
  }
}

async function refresh() {
  await Promise.all([loadShipments(), loadMetrics()]);
}

function visibleShipments() {
  return shipments.filter((shipment) => currentFilter === "all" || shipment.status === currentFilter);
}

function statusLabel(status) {
  return ({
    Registered: "Registered",
    InTransit: "In transit",
    Delivered: "Delivered",
    Cancelled: "Cancelled",
  })[status] || status;
}

function shipmentActions(shipment) {
  if (shipment.status === "Registered") {
    return `<button class="primary-action" type="button" data-action="dispatch">Dispatch</button><button type="button" data-action="cancel">Cancel</button>`;
  }
  if (shipment.status === "InTransit") {
    return `<button class="primary-action" type="button" data-action="deliver">Mark delivered</button><button type="button" data-action="cancel">Cancel</button>`;
  }
  return "";
}

function renderShipments() {
  const visible = visibleShipments();
  shipmentList.innerHTML = visible.length
    ? visible.map((shipment) => {
      const tracking = trackingSnapshots[shipment.trackingNumber];
      const trackingLabel = tracking ? statusLabel(tracking.status) : "Waiting for event…";
      const isLagging = !tracking || tracking.status !== shipment.status;
      return `
      <article class="shipment-item" data-id="${shipment.id}">
        <div class="shipment-main">
          <div class="shipment-identity">
            <code>${escapeHtml(shipment.trackingNumber)}</code>
            <span class="status-badge is-${shipment.status.toLowerCase()}">${escapeHtml(statusLabel(shipment.status))}</span>
          </div>
          <strong>${escapeHtml(shipment.recipient)}</strong>
          <span class="shipment-destination">→ ${escapeHtml(shipment.destination)}</span>
          <span class="projection-state ${isLagging ? "is-lagging" : ""}"><i></i>Public tracking: ${escapeHtml(trackingLabel)}</span>
        </div>
        <div class="shipment-actions">${shipmentActions(shipment)}</div>
      </article>`;
    }).join("")
    : '<div class="empty-state"><span class="empty-envelope" aria-hidden="true"></span><strong>No shipments here</strong><span>Register a parcel to publish the first event.</span></div>';

  document.querySelectorAll(".shipment-actions button").forEach((button) => {
    button.addEventListener("click", () => transitionShipment(
      button.closest(".shipment-item").dataset.id,
      button.dataset.action));
  });

  document.querySelector("#shipment-count").textContent = `${visible.length} ${visible.length === 1 ? "shipment" : "shipments"}`;
  const active = shipments.filter((shipment) => shipment.status === "Registered" || shipment.status === "InTransit").length;
  document.querySelector("#shipment-summary").textContent = `${active} active · ${shipments.length} total`;
}

async function transitionShipment(id, action) {
  const operations = {
    dispatch: [dispatchShipment, "Shipment dispatched"],
    deliver: [deliverShipment, "Shipment delivered"],
    cancel: [cancelShipment, "Shipment cancelled"],
  };
  const [operation, successMessage] = operations[action];

  try {
    markCommandInFlight(`${action[0].toUpperCase()}${action.slice(1)} command sent`);
    await operation(id);
    await loadShipments();
    showToast(successMessage);
  } catch {
    showToast("That shipment transition is not allowed");
  }
}

document.querySelector("#shipment-form").addEventListener("submit", async (event) => {
  event.preventDefault();
  const recipient = recipientInput.value.trim();
  const destination = destinationInput.value.trim();
  if (!recipient || !destination) return;

  try {
    markCommandInFlight("Register command sent");
    await registerShipment(recipient, destination);
    recipientInput.value = "";
    destinationInput.value = "";
    await loadShipments();
    showToast("Shipment registered");
  } catch {
    showToast("Could not register shipment");
  }
});

document.querySelectorAll(".filter").forEach((button) => button.addEventListener("click", () => {
  currentFilter = button.dataset.filter;
  document.querySelectorAll(".filter").forEach((item) => {
    const active = item === button;
    item.classList.toggle("active", active);
    item.setAttribute("aria-selected", active);
  });
  renderShipments();
}));

document.querySelector("#clear-events").addEventListener("click", () => {
  observedEvents = [];
  renderEventMonitor();
  resetArchitectureFlow();
});

document.addEventListener("keydown", (event) => {
  if (event.key.toLowerCase() === "n" && document.activeElement.tagName !== "INPUT") {
    event.preventDefault();
    recipientInput.focus();
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
    queued: "Accepted by queue",
    processing: "Dispatching to subscribers",
    "consumer-started": "Subscriber running",
    "consumer-completed": "Subscriber completed",
    "consumer-failed": "Subscriber failed",
    completed: "All subscribers completed",
    "completed-with-errors": "Completed with subscriber errors",
  })[stage] || stage;
}

function eventStageClass(stage) {
  if (stage === "completed") return "is-complete";
  if (stage === "consumer-failed" || stage === "completed-with-errors") return "is-error";
  if (stage === "queued") return "is-queued";
  return "is-processing";
}

function subscriberNodeId(consumerName) {
  const normalized = consumerName.toLowerCase();
  if (normalized.includes("tracking")) return "flow-tracking";
  if (normalized.includes("notification")) return "flow-notifications";
  if (normalized.includes("metrics")) return "flow-metrics";
  return "flow-audit";
}

function setFlowNodeState(id, state) {
  document.querySelector(`#${id}`)?.classList.add(state);
}

function clearFlowNodeStates() {
  flowNodeIds.forEach((id) => document.querySelector(`#${id}`)?.classList.remove("is-active", "is-complete", "is-error"));
}

function resetArchitectureFlow() {
  clearFlowNodeStates();
  document.querySelector("#flow-event-name").textContent = "DomainEvent";
  flowCaption.className = "flow-caption";
  flowCaption.innerHTML = '<i></i><span><strong>Ready.</strong> Register a shipment below to follow one event through the architecture.</span>';
}

function markCommandInFlight(label) {
  resetArchitectureFlow();
  setFlowNodeState("flow-command", "is-active");
  flowCaption.className = "flow-caption is-live";
  flowCaption.innerHTML = `<i></i><span><strong>${escapeHtml(label)}.</strong> The browser is waiting only for the Shipment API.</span>`;
}

function renderArchitectureTrace(event) {
  clearFlowNodeStates();
  setFlowNodeState("flow-command", "is-complete");
  setFlowNodeState("flow-producer", "is-complete");
  document.querySelector("#flow-event-name").textContent = event.eventType;

  const consumers = Object.entries(event.consumers);
  const finished = event.stage === "completed" || event.stage === "completed-with-errors";

  if (event.stage === "queued") {
    setFlowNodeState("flow-queue", "is-active");
    flowCaption.className = "flow-caption is-live";
    flowCaption.innerHTML = `<i></i><span><strong>The event was accepted, so the API can return.</strong> ${escapeHtml(event.eventType)} is queued; subscribers have not updated their read models yet.</span>`;
    return;
  }

  setFlowNodeState("flow-queue", "is-complete");
  if (event.stage === "processing") {
    setFlowNodeState("flow-dispatcher", "is-active");
    flowCaption.className = "flow-caption is-live";
    flowCaption.innerHTML = `<i></i><span><strong>Background processing.</strong> The dispatcher dequeued ${escapeHtml(event.eventType)} and found four subscribers.</span>`;
    return;
  }

  setFlowNodeState("flow-dispatcher", "is-complete");
  consumers.forEach(([name, consumer]) => {
    setFlowNodeState(subscriberNodeId(name), consumer.state === "failed" ? "is-error" : consumer.state === "completed" ? "is-complete" : "is-active");
  });

  if (finished) {
    const slowest = consumers
      .filter(([, consumer]) => Number.isFinite(consumer.durationMs))
      .sort((left, right) => right[1].durationMs - left[1].durationMs)[0];
    const timing = slowest
      ? ` ${escapeHtml(slowest[0])} was slowest at ${formatDuration(slowest[1].durationMs)}.`
      : "";
    flowCaption.className = `flow-caption ${event.stage === "completed" ? "is-complete" : "is-live"}`;
    flowCaption.innerHTML = `<i></i><span><strong>Fan-out complete.</strong> Four services handled the same event at different speeds.${timing}</span>`;
  } else {
    flowCaption.className = "flow-caption is-live";
    flowCaption.innerHTML = '<i></i><span><strong>Subscribers are running independently.</strong> A failure in one does not prevent the others from handling the event.</span>';
  }
}

function recordEventTrace(trace) {
  let observed = observedEvents.find((item) => item.eventId === trace.eventId);
  if (!observed) {
    observed = { eventId: trace.eventId, eventType: trace.eventType, recordedAt: trace.recordedAt, stage: trace.stage, consumers: {} };
    observedEvents.unshift(observed);
    observedEvents = observedEvents.slice(0, 8);
  }

  observed.stage = trace.stage;
  observed.recordedAt = trace.recordedAt;
  if (trace.consumer) {
    const previous = observed.consumers[trace.consumer] || {};
    const state = trace.stage === "consumer-failed" ? "failed" : trace.stage === "consumer-completed" ? "completed" : "running";
    const startedAt = previous.startedAt || trace.recordedAt;
    observed.consumers[trace.consumer] = {
      state,
      startedAt,
      durationMs: state === "running" ? null : Math.max(0, new Date(trace.recordedAt) - new Date(startedAt)),
    };
  }

  renderEventMonitor();
  renderArchitectureTrace(observed);
  if (trace.stage === "completed" || trace.stage === "completed-with-errors") {
    loadMetrics();
    loadTrackingSnapshots();
  }
}

function renderEventMonitor() {
  if (!observedEvents.length) {
    eventList.innerHTML = '<div class="event-empty"><strong>No events published</strong><span>Register a shipment to start the event stream.</span></div>';
    return;
  }

  eventList.innerHTML = observedEvents.map((event) => {
    const consumers = Object.entries(event.consumers);
    const consumerMarkup = consumers.length
      ? consumers.map(([name, consumer]) => `<span class="consumer-state is-${consumer.state}"><i></i><b>${escapeHtml(name)}</b><em>${consumer.durationMs === null ? "running…" : formatDuration(consumer.durationMs)}</em></span>`).join("")
      : '<span class="consumer-state"><i></i><b>Waiting in the Channel</b><em>queued</em></span>';
    const time = new Date(event.recordedAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit", second: "2-digit" });

    return `<article class="event-item ${eventStageClass(event.stage)}">
      <div class="event-item-heading"><strong>${escapeHtml(event.eventType)}</strong><time>${time}</time></div>
      <div class="event-meta"><code>#${escapeHtml(event.eventId.slice(0, 8))}</code><span>${escapeHtml(eventStageLabel(event.stage))}</span></div>
      <div class="consumer-states">${consumerMarkup}</div>
    </article>`;
  }).join("");
}

function formatDuration(milliseconds) {
  return milliseconds >= 1000 ? `${(milliseconds / 1000).toFixed(2)}s` : `${Math.round(milliseconds)}ms`;
}

function connectEventMonitor() {
  const eventSource = new EventSource("/events/stream");
  eventSource.addEventListener("open", () => {
    eventConnection.className = "connection-status is-connected";
    eventConnection.innerHTML = '<i aria-hidden="true"></i>Live';
  });
  eventSource.addEventListener("event-trace", (message) => recordEventTrace(JSON.parse(message.data)));
  eventSource.addEventListener("error", () => {
    eventConnection.className = "connection-status is-connecting";
    eventConnection.innerHTML = '<i aria-hidden="true"></i>Reconnecting';
  });
}

document.querySelector("#current-date").textContent = new Date().toLocaleDateString("en-SE", { weekday: "long", month: "long", day: "numeric" });
connectEventMonitor();
refresh();
