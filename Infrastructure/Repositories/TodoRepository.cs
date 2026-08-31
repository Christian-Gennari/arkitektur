using arkitektur.Application.Interfaces;
using arkitektur.Domain.Models;

namespace arkitektur.Infrastructure.Repositories;

public class TodoRepository : ITodoRepository
{
    private readonly List<Todo> todos = [];
    private readonly Lock syncRoot = new();

    #region Get methods
    public List<Todo> GetAll()
    {
        lock (syncRoot)
        {
            return todos.Select(Clone).ToList();
        }
    }

    public Todo? GetById(int id)
    {
        lock (syncRoot)
        {
            var todo = todos.FirstOrDefault(t => t.Id == id);
            return todo is null ? null : Clone(todo);
        }
    }

    public void Add(Todo todo)
    {
        lock (syncRoot)
        {
            todos.Add(Clone(todo));
        }
    }

    #endregion

    #region Update and Delete Methods
    public void Update(Todo todo)
    {
        lock (syncRoot)
        {
            var existingTodo = todos.FirstOrDefault(t => t.Id == todo.Id);
            if (existingTodo != null)
            {
                existingTodo.Title = todo.Title;
                existingTodo.IsCompleted = todo.IsCompleted;
            }
        }
    }

    public void Delete(Todo todo)
    {
        lock (syncRoot)
        {
            todos.RemoveAll(item => item.Id == todo.Id);
        }
    }

    private static Todo Clone(Todo todo)
    {
        return new Todo
        {
            Id = todo.Id,
            Title = todo.Title,
            IsCompleted = todo.IsCompleted
        };
    }

    #endregion
}
