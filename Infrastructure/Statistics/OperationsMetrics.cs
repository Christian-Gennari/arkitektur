using arkitektur.Application.Interfaces;

namespace arkitektur.Infrastructure.Statistics;

public sealed class OperationsMetrics : IOperationsMetrics
{
    private readonly Lock syncRoot = new();
    private int registeredCount;
    private int dispatchedCount;
    private int deliveredCount;
    private int cancelledCount;

    public int RegisteredCount { get { lock (syncRoot) return registeredCount; } }
    public int DispatchedCount { get { lock (syncRoot) return dispatchedCount; } }
    public int DeliveredCount { get { lock (syncRoot) return deliveredCount; } }
    public int CancelledCount { get { lock (syncRoot) return cancelledCount; } }

    public void RecordRegistered() { lock (syncRoot) registeredCount++; }
    public void RecordDispatched() { lock (syncRoot) dispatchedCount++; }
    public void RecordDelivered() { lock (syncRoot) deliveredCount++; }
    public void RecordCancelled() { lock (syncRoot) cancelledCount++; }
}
