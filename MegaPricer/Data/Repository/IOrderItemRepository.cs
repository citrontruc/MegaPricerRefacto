using MegaPricer.Dtos;

namespace MegaPricer.Data;

public interface IOrderItemRepository
{
    public Task StoreOrderItemAsync(OrderItemDto orderItem);
}
