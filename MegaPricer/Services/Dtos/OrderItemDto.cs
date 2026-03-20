namespace MegaPricer.Dtos;

public record OrderItemDto
{
    public int OrderId;
    public string OrderSku;
    public int OrderQuantity;
    public decimal Cost;
    public float MarkUp;
    public float UserMarkup;
    public string ItemColorName;

    public float ItemHeight;
    public float ItemWidth;
    public float ItemDepth;
    public float LinearFootCost;
    public decimal TotalPartCost;
    public PricingColorsDto pricingColorsDto;
}
