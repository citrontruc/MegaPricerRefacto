using MegaPricer.Services;

namespace MegaPricer.Entities;

public record PriceResult
{
  public decimal Subtotal;
  public decimal SubtotalFlat;
  public decimal SubtotalPlus;

  public PriceResult(decimal subtotal, decimal subtotalFlat, decimal subtotalPlus)
  {
    Subtotal = subtotal;
    SubtotalFlat = subtotalFlat;
    SubtotalPlus = subtotalPlus;
  }

  public override string ToString()
  {
    return $"${GlobalHelpers.Format(Subtotal).ToString("F2")}|${GlobalHelpers.Format(SubtotalFlat).ToString("F2")}|${GlobalHelpers.Format(SubtotalPlus).ToString("F2")}";
  }

}
