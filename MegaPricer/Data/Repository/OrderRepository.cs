using MegaPricer.Dtos;
using Microsoft.EntityFrameworkCore;

namespace MegaPricer.Data;

public class OrderRepository : IOrderRepository
{
    private ApplicationDbContext _context;
    public OrderRepository(ApplicationDbContext applicationDbContext)
    {
        _context = applicationDbContext;
    }

    public async Task<int> StoreOrderAsync(OrderDto orderDto)
    {
        Order order = new Order()
        {
            KitchenId = orderDto.KitchenId,
            OrderDate = orderDto.OrderDate,
            OrderStatus = orderDto.OrderStatus,
            OrderType = orderDto.OrderType
        };
        _context.Orders.Add(order);
        return await _context.SaveChangesAsync();
    }
}
