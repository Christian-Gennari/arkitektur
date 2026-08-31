using arkitektur.Application.Interfaces;

namespace arkitektur.Infrastructure.Logging;

public sealed class FilePostalAuditLog(IHostEnvironment environment) : IPostalAuditLog
{
    private readonly SemaphoreSlim fileLock = new(1, 1);
    private readonly string filePath = Path.Combine(environment.ContentRootPath, "postal-audit.log");

    public async Task WriteAsync(string eventName, string message)
    {
        var safeMessage = message.Replace('\r', ' ').Replace('\n', ' ');
        var line = $"{DateTimeOffset.UtcNow:O} | {eventName} | {safeMessage}";

        await fileLock.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(filePath, line + Environment.NewLine);
        }
        finally
        {
            fileLock.Release();
        }
    }
}
