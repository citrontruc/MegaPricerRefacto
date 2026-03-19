using MegaPricer.Dtos;
using Microsoft.EntityFrameworkCore;

namespace MegaPricer.Data;

public class PricingSkuRepository : IPricingSkuRepository
{
    private ApplicationDbContext _context;
    public PricingSkuRepository(ApplicationDbContext applicationDbContext)
    {
        _context = applicationDbContext;
    }

    public async Task<IEnumerable<PricingSkuDto>> RetrievePricingSkuAsync(string sku)
    {
        return await _context.PricingSkus
            .AsNoTracking()
            .Where(x => x.SKU == sku)
            .Select(
            x => new PricingSkuDto()
                {
                    Sku = x.SKU,
                    WholesalePrice = x.WholesalePrice
                }
            ).ToListAsync();
    }
}
