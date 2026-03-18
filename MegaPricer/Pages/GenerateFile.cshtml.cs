using MegaPricer.Data;
using MegaPricer.Entities;
using MegaPricer.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MegaPricer.Pages;

public class GenerateFileModel : PageModel
{
  private readonly ILogger<GenerateFileModel> _logger;
  private PricingService _pricingService;

  public GenerateFileModel(ILogger<GenerateFileModel> logger, PricingService pricingService)
  {
    _logger = logger;
    _pricingService = pricingService;
  }

  public async Task OnGet()
  {
    if (!(User is null) && User.Identity.IsAuthenticated)
    {
      if (!Context.Session.ContainsKey(User.Identity.Name))
      {
        Context.Session.Add(User.Identity.Name, new Dictionary<string, object>());
      }
      if (!Context.Session[User.Identity.Name].ContainsKey("CompanyShortName"))
      {
        Context.Session[User.Identity.Name].Add("CompanyShortName", "Acme");
      }
      if (!Context.Session[User.Identity.Name].ContainsKey("PricingOff"))
      {
        Context.Session[User.Identity.Name].Add("PricingOff", "N");
      }
    }

    string userName = User.Identity.Name;
    (await _pricingService.CalculatePrice(
      new CustomerOrder() 
        {
          kitchenId = 1,
          wallOrderNum = 1,
          userName = userName
        },
        RefType.PriceReport
      )
    ).Value.ToString();
  }
}
