namespace arkitektur.Service;
using arkitektur.Repositories;
using arkitektur.Shared; 

public record TodoCreatedEvent(int Id, string Title, bool IsCompleted);
public record TodoCompletedEvent(int TodoId, string Title, DateTime CompletedAt);
public record TodoDeletedEvent(int TodoId);

public class TodoService(TodoRepository repository)
{
    public Todo Create(string title)
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
        // HÄNDELSE: TodoCreatedEvent
        // eventBus.Publish(new TodoCreatedEvent(todo.Id, todo.Title, todo.IsCompleted));
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
    public bool Complete(int id)
    {
        var todo = repository.GetById(id);
        if (todo == null)
        {
            return false;
        }
        todo.IsCompleted = true;
        repository.Update(todo);
        // HÄNDELSE: TodoCompletedEvent
        // eventBus.Publish(new TodoCompletedEvent(todo.Id, todo.Title, DateTime.UtcNow));
        return true;
    }
    public bool Delete(int id)
    {
        var todo = repository.GetById(id);
        if (todo == null)
        {
            return false;
        }
        repository.Delete(todo);
        // HÄNDELSE: TodoDeletedEvent
        // eventBus.Publish(new TodoDeletedEvent(id));
        return true;
    }
}



