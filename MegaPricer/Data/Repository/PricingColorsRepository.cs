using MegaPricer.Dtos;
using Microsoft.EntityFrameworkCore;

namespace MegaPricer.Data;

public class PricingColorsRepository : IPricingColorsRepository
{
    private ApplicationDbContext _context;
    public PricingColorsRepository(ApplicationDbContext applicationDbContext)
    {
        _context = applicationDbContext;
    }

    public async Task<IEnumerable<PricingColorsDto>> RetrievePricingColorsAsync(int pricingColorsId)
    {
        return await _context.PricingColors
            .AsNoTracking()
            .Where(x => x.PricingColorId == pricingColorsId)
            .Select(
            x => new PricingColorsDto()
                {
                    PricingsColorsId = x.PricingColorId,
                    ColorName = x.Name,
                    ColorMarkup = x.PercentMarkup,
                    ColorSquareFoot = x.ColorPerSquareFoot,
                    WholesalePrice = x.WholesalePrice
                }
            ).ToListAsync();
    }
}
