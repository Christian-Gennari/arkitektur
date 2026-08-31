namespace arkitektur.Application;

using arkitektur.Application.Events;
using arkitektur.Application.Interfaces;
using arkitektur.Domain.Models;

public class TodoService(ITodoRepository repository, IEventPublisher eventPublisher)
{
    public async Task<Todo> Create(string title)
    {
        var allTodos = repository.GetAll();
        var nextId = allTodos.Count > 0 ? allTodos.Max(t => t.Id) + 1 : 1;
        var todo = new Todo
        {
            Id = nextId,
            Title = title,
            IsCompleted = false
        };

        repository.Add(todo);

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
