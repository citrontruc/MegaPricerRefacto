using MegaPricer.Data;
using MegaPricer.Entities;
using MegaPricer.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MegaPricer.Pages;

public class PlaceOrderModel : PageModel
{
  private readonly ILogger<PlaceOrderModel> _logger;
  private IPricingService _pricingService;
  private OrderWriter _orderWriter;

  public PlaceOrderModel(ILogger<PlaceOrderModel> logger, IPricingService pricingService, OrderWriter orderWriter)
  {
    _logger = logger;
    _pricingService = pricingService;
    _orderWriter = orderWriter;
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
        _orderWriter
      )
    ).Value.ToString();
  }
}
