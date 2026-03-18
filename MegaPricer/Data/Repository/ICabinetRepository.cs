using MegaPricer.Dtos;
namespace MegaPricer.Data;

public interface ICabinetRepository
{
    public Task<IEnumerable<CabinetDto>> RetrieveCabinetOnWallId(int wallId);
}
