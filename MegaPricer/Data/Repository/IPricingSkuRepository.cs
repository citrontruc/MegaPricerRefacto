using MegaPricer.Dtos;

namespace MegaPricer.Data;

public interface IPricingSkuRepository
{
    public Task<IEnumerable<PricingSkuDto>> RetrievePricingSkuAsync(string sku);
}
