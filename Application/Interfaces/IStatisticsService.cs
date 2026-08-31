namespace arkitektur.Application.Interfaces;

public interface IStatisticsService
{
    int CreatedCount { get; }
    int CompletedCount { get; }
    int DeletedCount { get; }

    void RecordCreated();
    void RecordCompleted();
    void RecordDeleted();
}
