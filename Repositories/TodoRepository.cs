using arkitektur.Models;

namespace arkitektur.Repositories;

public class TodoRepository
{
    private readonly List<Todo> todos = [];

    public void Add(Todo todo)
    {
        todos.Add(todo);
    }
}
