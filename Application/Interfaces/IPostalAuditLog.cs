namespace arkitektur.Application.Interfaces;

public interface IPostalAuditLog
{
    Task WriteAsync(string eventName, string message);
}
