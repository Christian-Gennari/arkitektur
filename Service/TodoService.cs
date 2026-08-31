namespace arkitektur.Service;

using arkitektur.Models;
using arkitektur.Models.Events;
using arkitektur.Repositories;
using arkitektur.Shared;

public class TodoService(TodoRepository repository, EventBus eventBus)
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
        
        await eventBus.Publish(new TodoCreated(todo));

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
        return true;
    }
}