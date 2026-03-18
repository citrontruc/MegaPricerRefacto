using MegaPricer.Dtos;

namespace MegaPricer.Data;

public interface IFeatureRepository
{
    public Task<IEnumerable<CabinetFeatureDto>> RetrieveCabinetFeaturesAsync(int cabinetId);
}
