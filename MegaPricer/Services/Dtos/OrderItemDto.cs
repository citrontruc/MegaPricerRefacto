namespace MegaPricer.Dtos;

public record OrderItemDto
{
    public int OrderId;
    public string OrderSku;
    public int OrderQuantity;
    public decimal Cost;
    public float MarkUp;
    public float UserMarkup;
}
