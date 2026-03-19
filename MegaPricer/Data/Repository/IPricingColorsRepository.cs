using MegaPricer.Dtos;

namespace MegaPricer.Data;

public interface IPricingColorsRepository
{
    public Task<IEnumerable<PricingColorsDto>> RetrievePricingColorsAsync(int pricingColorsId);
}
