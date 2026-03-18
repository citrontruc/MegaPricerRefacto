using MegaPricer.Dtos;
using Microsoft.EntityFrameworkCore;

namespace MegaPricer.Data;

public class FeatureRepository : IFeatureRepository
{
    private ApplicationDbContext _context;
    public FeatureRepository(ApplicationDbContext applicationDbContext)
    {
        _context = applicationDbContext;
    }

    public async Task<IEnumerable<CabinetFeatureDto>> RetrieveCabinetFeaturesAsync(int cabinetId)
  {
    return await _context.Features.Where(x => x.CabinetId == cabinetId).Select(
        x => new CabinetFeatureDto()
            {
              FeatureId = x.FeatureId,
              ColorId = x.Color,
              FeatureSKU = x.SKU,
              Quantity = x.Quantity,
              FeatureHeight = x.Height,
              FeatureWidth = x.Width,
              FeatureCost = 0,
              ThisTotalFeatureCost = 0,
              FeatureColorName = "",
              WholesalePrice = 0,
            }
        ).ToListAsync();
  }
}
