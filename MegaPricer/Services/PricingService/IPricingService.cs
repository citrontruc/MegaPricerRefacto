using MegaPricer.Common;
using MegaPricer.Entities;

namespace MegaPricer.Services;

public interface IPricingService
{
    public Task<Result<PriceResult>> CalculatePrice(CustomerOrder customerOrder, IRefTypeWriter? writer);
}
