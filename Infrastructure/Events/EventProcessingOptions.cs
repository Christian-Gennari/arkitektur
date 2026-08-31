namespace arkitektur.Infrastructure.Events;

public sealed class EventProcessingOptions
{
    public const string SectionName = "EventProcessing";

    public int QueueCapacity { get; set; } = 100;
    public int DemoDelayMilliseconds { get; set; }
}
