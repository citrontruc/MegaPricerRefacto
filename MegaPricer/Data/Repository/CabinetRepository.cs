using MegaPricer.Dtos;
using Microsoft.EntityFrameworkCore;

namespace MegaPricer.Data;


public class CabinetRepository : ICabinetRepository
{
    private ApplicationDbContext _context;
    public CabinetRepository(ApplicationDbContext applicationDbContext)
    {
        _context = applicationDbContext;
    }

    public async Task<IEnumerable<CabinetDto>> RetrieveCabinetOnWallId(int WallId)
  {
    return await _context.Cabinets.Where(x => x.WallId == WallId).Select(x => new CabinetDto()
    {
        cabinetId = x.CabinetId,
        thisPartWidth = x.Width,
        thisPartDepth = x.Depth,
        thisPartHeight = x.Height,
        thisPartColorId = x.Color,
        thisPartSku = x.SKU,
        thisPartCost = 0,
        thisSectionWidth = 0
    }
    ).ToListAsync();
  }
}
