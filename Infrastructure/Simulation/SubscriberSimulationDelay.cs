using Microsoft.Extensions.Options;

namespace arkitektur.Infrastructure.Simulation;

public sealed class SubscriberSimulationOptions
{
    public const string SectionName = "SubscriberSimulation";

    public DelayRange PublicTracking { get; set; } = new();
    public DelayRange CustomerNotifications { get; set; } = new();
    public DelayRange OperationsMetrics { get; set; } = new();
    public DelayRange PostalAudit { get; set; } = new();
}

public sealed class DelayRange
{
    public int MinMilliseconds { get; set; }
    public int MaxMilliseconds { get; set; }
}

public sealed class SubscriberSimulationDelay(IOptions<SubscriberSimulationOptions> options)
{
    private readonly SubscriberSimulationOptions settings = options.Value;

    public Task ForPublicTracking(CancellationToken cancellationToken) =>
        Wait(settings.PublicTracking, cancellationToken);

    public Task ForCustomerNotifications(CancellationToken cancellationToken) =>
        Wait(settings.CustomerNotifications, cancellationToken);

    public Task ForOperationsMetrics(CancellationToken cancellationToken) =>
        Wait(settings.OperationsMetrics, cancellationToken);

    public Task ForPostalAudit(CancellationToken cancellationToken) =>
        Wait(settings.PostalAudit, cancellationToken);

    private static Task Wait(DelayRange range, CancellationToken cancellationToken)
    {
        var minimum = Math.Max(0, range.MinMilliseconds);
        var maximum = Math.Max(minimum, range.MaxMilliseconds);
        var duration = maximum == minimum
            ? minimum
            : Random.Shared.Next(minimum, maximum + 1);

        return duration == 0
            ? Task.CompletedTask
            : Task.Delay(duration, cancellationToken);
    }
}
