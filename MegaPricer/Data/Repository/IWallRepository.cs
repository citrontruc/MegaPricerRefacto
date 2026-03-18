using MegaPricer.Dtos;

namespace MegaPricer.Data;

public interface IWallRepository
{
    public Task<IEnumerable<WallPricerDto>> RetrieveKitchenWallAsync(int kitchenId, int wallOrderNum);
}
