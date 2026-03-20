using MegaPricer.Common;
using MegaPricer.Entities;

namespace MegaPricer.Services;

public class PricingServiceErrorHandling : IPricingService
{
    private PricingService _pricingService;
    public PricingServiceErrorHandling(PricingService pricingService)
    {
        _pricingService = pricingService;
    }
  public Task<Result<PriceResult>> CalculatePrice(CustomerOrder customerOrder, IRefTypeWriter? writer)
    {
        try
        {
            return _pricingService.CalculatePrice(customerOrder, writer);
        }
        catch (Exception ex)
        {
            GlobalHelpers.SendErrorEmail("CalcPrice", ex.Message, ex.StackTrace);
            throw;
        }
        finally
        {
            if (writer != null)
                {
                    writer.Dispose();
                }
        }
    }

}
