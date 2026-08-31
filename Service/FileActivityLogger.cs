using arkitektur.Interfaces;

namespace arkitektur.Service;

public sealed class FileActivityLogger(IHostEnvironment environment) : IActivityLogger
{
    private readonly SemaphoreSlim fileLock = new(1, 1);
    private readonly string filePath = Path.Combine(environment.ContentRootPath, "log.txt");

    public async Task LogAsync(string activity, string message)
    {
        var safeMessage = message.Replace('\r', ' ').Replace('\n', ' ');
        var line = $"{DateTimeOffset.UtcNow:O} | {activity} | {safeMessage}";

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
