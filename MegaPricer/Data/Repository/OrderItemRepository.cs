using MegaPricer.Dtos;
using Microsoft.EntityFrameworkCore;

namespace MegaPricer.Data;

public class OrderItemRepository : IOrderItemRepository
{
    private ApplicationDbContext _context;
    public OrderItemRepository(ApplicationDbContext applicationDbContext)
    {
        _context = applicationDbContext;
    }

    public async Task StoreOrderItemAsync(OrderItemDto orderItem)
    {
        OrderItem orderValue = new OrderItem()
        {
            OrderId = orderItem.OrderId,
            SKU = orderItem.OrderSku,
            Quantity = orderItem.OrderQuantity,
            BasePrice = (float)orderItem.Cost,
            Markup = orderItem.MarkUp,
            UserMarkup = orderItem.UserMarkup
        };
        _context.OrderItem.Add(orderValue);
        await _context.SaveChangesAsync();
    }
}
