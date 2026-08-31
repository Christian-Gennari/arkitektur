using arkitektur.Domain.Models;

namespace arkitektur.Application.Interfaces;

public interface ITodoRepository
{
    List<Todo> GetAll();
    Todo? GetById(int id);
    void Add(Todo todo);
    void Update(Todo todo);
    void Delete(Todo todo);
}
