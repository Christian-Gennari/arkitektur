namespace arkitektur.Application.Interfaces;

public interface IOperationsMetrics
{
    int RegisteredCount { get; }
    int DispatchedCount { get; }
    int DeliveredCount { get; }
    int CancelledCount { get; }

    void RecordRegistered();
    void RecordDispatched();
    void RecordDelivered();
    void RecordCancelled();
}
