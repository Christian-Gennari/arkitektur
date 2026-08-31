using arkitektur.Domain.Models;

namespace arkitektur.Application.Interfaces;

public interface IShipmentRepository
{
    List<Shipment> GetAll();
    Shipment? GetById(int id);
    void Add(Shipment shipment);
    void Update(Shipment shipment);
}
