using MegaPricer.Dtos;
using Microsoft.EntityFrameworkCore;

namespace MegaPricer.Data;

public class WallRepository : IWallRepository
{
    private ApplicationDbContext _context;
    public WallRepository(ApplicationDbContext applicationDbContext)
    {
        _context = applicationDbContext;
    }

    public async Task<IEnumerable<WallPricerDto>> RetrieveKitchenWallAsync(int kitchenId, int wallOrderNum)
    {
        return await _context.Walls.Where(x => x.KitchenId == kitchenId && x.WallOrder == wallOrderNum).Select(
        x => new WallPricerDto()
            {
                cabinetColorId = x.CabinetColor,
                wallId = x.WallId,
                isIsland = x.IsIsland,
                wallHeight = x.Height
            }
        ).ToListAsync();
    }
}
