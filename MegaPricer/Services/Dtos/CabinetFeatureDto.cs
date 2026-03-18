namespace MegaPricer.Dtos;

public record CabinetFeatureDto
{
    public int FeatureId;
    public int ColorId;
    public string FeatureSKU;
    public int Quantity;
    public float FeatureHeight;
    public float FeatureWidth;
    public decimal FeatureCost;
    public decimal ThisTotalFeatureCost;
    public string FeatureColorName;
    public float WholesalePrice;
}