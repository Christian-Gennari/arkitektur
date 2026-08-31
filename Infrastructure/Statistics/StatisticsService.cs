using arkitektur.Application.Interfaces;

namespace arkitektur.Infrastructure.Statistics;

public sealed class StatisticsService : IStatisticsService
{
    private readonly Lock syncRoot = new();
    private int createdCount;
    private int completedCount;
    private int deletedCount;

    public int CreatedCount
    {
        get
        {
            lock (syncRoot)
            {
                return createdCount;
            }
        }
    }

    public int CompletedCount
    {
        get
        {
            lock (syncRoot)
            {
                return completedCount;
            }
        }
    }

    public int DeletedCount
    {
        get
        {
            lock (syncRoot)
            {
                return deletedCount;
            }
        }
    }

    public void RecordCreated()
    {
        lock (syncRoot)
        {
            createdCount++;
        }
    }

    public void RecordCompleted()
    {
        lock (syncRoot)
        {
            completedCount++;
        }
    }

    public void RecordDeleted()
    {
        lock (syncRoot)
        {
            deletedCount++;
        }
    }
}
