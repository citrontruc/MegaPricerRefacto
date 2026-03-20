using MegaPricer.Data;
using MegaPricer.Dtos;

namespace MegaPricer.Services;

public interface IRefTypeWriter : IDisposable
{
    public Task InitializeWriter(Order order, Kitchen kitchen);
    public Task WriteCabinetItem(OrderItemDto orderItemDto);
}
