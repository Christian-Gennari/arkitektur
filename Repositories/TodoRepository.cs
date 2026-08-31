using arkitektur.Models;

namespace arkitektur.Repositories;

public class TodoRepository
{
    private readonly List<Todo> todos = [];

    #region Get methods
    public List<Todo> GetAll()
    {
        return todos;
    }

    public Todo? GetById(int id)
    {
        return todos.FirstOrDefault(t => t.Id == id);
    }

    public void Add(Todo todo)
    {
        todos.Add(todo);
    }

    #endregion

    #region Update and Delete Methods
    public void Update(Todo todo)
    {
        var existingTodo = todos.FirstOrDefault(t => t.Id == todo.Id);
        if (existingTodo != null)
        {
            existingTodo.Title = todo.Title;
            existingTodo.IsCompleted = todo.IsCompleted;
        }
    }

    public void Delete(Todo todo)
    {
        todos.Remove(todo);
    }

    #endregion
}
