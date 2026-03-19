using MegaPricer.Dtos;

namespace MegaPricer.Data;

public interface IOrderRepository
{
    public Task<int> StoreOrderAsync(OrderDto orderDto);
}
