namespace arkitektur.Application;

using arkitektur.Application.Events;
using arkitektur.Application.Interfaces;
using arkitektur.Domain.Models;

public class TodoService(ITodoRepository repository, IEventPublisher eventPublisher)
{
    private readonly Lock createLock = new();

    public async Task<Todo> Create(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title kan inte vara tom.", nameof(title));
        }

        Todo todo;
        lock (createLock)
        {
            var highestId = repository.GetAll()
                .Select(todo => todo.Id)
                .DefaultIfEmpty(0)
                .Max();

            if (highestId == int.MaxValue)
            {
                throw new InvalidOperationException("Det går inte att skapa fler todos eftersom ID-numret har nått maxgränsen.");
            }

            todo = new Todo
            {
                Id = highestId + 1,
                Title = title,
                IsCompleted = false
            };

            repository.Add(todo);
        }

        await eventPublisher.Publish(new TodoCreated(todo));

        return todo;
    }

    public Todo? GetById(int id)
    {
        return repository.GetById(id);
    }

    public List<Todo> GetAll()
    {
        return repository.GetAll();
    }

    public async Task<bool> Complete(int id)
    {
        var todo = repository.GetById(id);
        if (todo == null)
        {
            return false;
        }
        if (todo.IsCompleted)
        {
            return true;
        }
        todo.IsCompleted = true;
        repository.Update(todo);
        await eventPublisher.Publish(new TodoUpdated(todo));
        return true;
    }

    public async Task<bool> Uncomplete(int id)
    {
        var todo = repository.GetById(id);
        if (todo == null)
        {
            return false;
        }
        if (!todo.IsCompleted)
        {
            return true;
        }

        todo.IsCompleted = false;
        repository.Update(todo);
        await eventPublisher.Publish(new TodoUpdated(todo));
        return true;
    }

    public async Task<bool> Delete(int id)
    {
        var todo = repository.GetById(id);
        if (todo == null)
        {
            return false;
        }
        repository.Delete(todo);
        await eventPublisher.Publish(new TodoDeleted(todo));
        return true;
    }
}
