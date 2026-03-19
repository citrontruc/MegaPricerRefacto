namespace MegaPricer.Dtos;

public record OrderDto
{
    public int KitchenId;
    public DateTime OrderDate;
    public string OrderStatus;
    public string OrderType;
}
