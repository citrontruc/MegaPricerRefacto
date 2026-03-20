using MegaPricer.Data;
using MegaPricer.Dtos;

namespace MegaPricer.Services;

public class OrderWriter : IRefTypeWriter
{
    private IOrderItemRepository _orderItemRepository;
    private IOrderRepository _orderRepository;
    public OrderWriter(
        IOrderItemRepository orderItemRepository,
        IOrderRepository orderRepository
    )
    {
        _orderItemRepository = orderItemRepository;
        _orderRepository = orderRepository;
    }

  public async Task InitializeWriter(Order order, Kitchen Kitchen)
    {
        // create a new order
        order.KitchenId = Kitchen.KitchenId;
        OrderDto orderDto = new OrderDto()
        {
          KitchenId = order.KitchenId,
          OrderDate = order.OrderDate,
          OrderStatus = order.OrderStatus,
          OrderType = order.OrderType
        };
        order.OrderId = await _orderRepository.StoreOrderAsync(orderDto);
    }

    public async Task WriteCabinetItem(OrderItemDto orderItemDto)
    {
        await _orderItemRepository.StoreOrderItemAsync(orderItemDto);
    }

    public void Dispose() {}
}
