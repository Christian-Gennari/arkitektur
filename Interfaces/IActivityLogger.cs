namespace arkitektur.Interfaces;

public interface IActivityLogger
{
    Task LogAsync(string activity, string message);
}
