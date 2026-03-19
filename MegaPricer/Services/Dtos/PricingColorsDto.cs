namespace MegaPricer.Dtos;

public record PricingColorsDto
{
    public int PricingsColorsId;
    public string ColorName = "";
    public float ColorMarkup = 0;
    public float ColorSquareFoot = 0;
    public float WholesalePrice;
}
