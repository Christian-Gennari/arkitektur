namespace arkitektur.Application.Interfaces;

public interface IActivityLogger
{
    Task LogAsync(string activity, string message);
}
