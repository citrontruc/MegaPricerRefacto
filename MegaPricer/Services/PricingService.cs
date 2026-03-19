using System.Data;
using MegaPricer.Common;
using MegaPricer.Data;
using MegaPricer.Entities;
using Microsoft.Data.Sqlite;
using MegaPricer.Dtos;

namespace MegaPricer.Services;

public class PricingService
{
  private IFeatureRepository _featureRepository;
  private ICabinetRepository _cabinetRepository;
  private IWallRepository _wallRepository;
  private IUserMarkupRepository _userMarkupRepository;
  private IOrderItemRepository _orderItemRepository;
  private IPricingColorsRepository _pricingColorsRepository;

  public PricingService(
    IFeatureRepository featureRepository,
    ICabinetRepository cabinetRepository,
    IWallRepository wallRepository,
    IUserMarkupRepository userMarkupRepository,
    IOrderItemRepository orderItemRepository,
    IPricingColorsRepository pricingColorsRepository
    )
  {
    _featureRepository = featureRepository;
    _cabinetRepository = cabinetRepository;
    _wallRepository = wallRepository;
    _userMarkupRepository = userMarkupRepository;
    _orderItemRepository = orderItemRepository;
    _pricingColorsRepository = pricingColorsRepository;
  }

  public async Task<Result<PriceResult>> CalculatePrice(CustomerOrder customerOrder, RefType refType)
  {
    if (Context.Session[customerOrder.userName]["PricingOff"].ToString() == "Y") return Result<PriceResult>.Success(new PriceResult(0, 0, 0));

    Kitchen kitchen = new Kitchen();
    Order order = new Order();
    CabinetDto lastPart = new();
    PricingColorsDto pricingColorsDto = new();
    PriceResult priceResult = new(0, 0, 0);
    string thisPartColorName = "";
    float thisColorMarkup = 0;
    float thisColorSquareFoot = 0;
    float thisLinearFootCost = 0;
    int thisPartQty = 0;
    decimal thisTotalPartCost = 0;
    float thisUserMarkup = 0;
    StreamWriter sr = null;

    Context.Session[customerOrder.userName]["WallWeight"] = 0;

    try
    {
      if (customerOrder.wallOrderNum == 0)
      {
        return Result<PriceResult>.Failure(new Error("Session Expired", "Session expired: Log in again."));
      }
      if (customerOrder.kitchenId <= 0)
      {
        return Result<PriceResult>.Failure(new Error("Invalid ID", "Invalid KitchenId"));
      }
      kitchen.GetCustomerKitchen(customerOrder.kitchenId, customerOrder.userName);
      var WallDtoValues = await _wallRepository.RetrieveKitchenWallAsync(customerOrder.kitchenId, customerOrder.wallOrderNum);

      if (WallDtoValues.Count() == 0)
      {
        return Result<PriceResult>.Failure(new Error("Invalid Value", "Invalid wallOrderNum"));
      }

      if (refType == RefType.PriceReport)
      {
        // Start writing to the report file
        string baseDirectory = AppContext.BaseDirectory;
        string path = baseDirectory + "Orders.csv";
        sr = new StreamWriter(path);
        sr.WriteLine($"{kitchen.Name} ({kitchen.KitchenId}) - Run time: {DateTime.Now:T} ");
        sr.WriteLine("");
        sr.WriteLine("Part Name,Part SKU,Height,Width,Depth,Color,Sq Ft $, Lin Ft $,Per Piece $,# Needed,Part Price,Add On %,Total Part Price");
      }
      else if (refType == RefType.Order)
      {
        // create a new order
        order.KitchenId = customerOrder.kitchenId;
        using (var conn = new SqliteConnection(ConfigurationSettings.ConnectionString))
        {
          var cmd = conn.CreateCommand();
          cmd.CommandText = "INSERT INTO ORDERS (KitchenId,OrderDate,OrderStatus,OrderType) VALUES (@kitchenId,@orderDate,@orderStatus,@orderType)";
          cmd.Parameters.AddWithValue("@kitchenId", order.KitchenId);
          cmd.Parameters.AddWithValue("@orderDate", order.OrderDate);
          cmd.Parameters.AddWithValue("@orderStatus", order.OrderStatus);
          cmd.Parameters.AddWithValue("@orderType", order.OrderType);
          conn.Open();
          cmd.ExecuteNonQuery();
          var cmd2 = conn.CreateCommand();
          cmd2.CommandText = "SELECT last_insert_rowid();";
          order.OrderId = Convert.ToInt32(cmd2.ExecuteScalar());
        }
      }

      int defaultColorId = WallDtoValues.First().cabinetColorId;
      int wallId = WallDtoValues.First().wallId;
      bool isIsland = WallDtoValues.First().isIsland;
      float wallHeight = WallDtoValues.First().wallHeight;

      var cabinetWithCorrectWallId = await _cabinetRepository.RetrieveCabinetOnWallId(wallId);

      float totalCabinetHeight = 0;
      foreach (CabinetDto cabinetValue in cabinetWithCorrectWallId) // each cabinet
      {
        lastPart = cabinetValue;
        totalCabinetHeight += cabinetValue.thisPartHeight;

        if (!string.IsNullOrEmpty(cabinetValue.thisPartSku))
        {
          using (var conn = new SqliteConnection(ConfigurationSettings.ConnectionString))
          {
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM PricingSkus WHERE SKU = @sku";
            cmd.Parameters.AddWithValue("@sku", cabinetValue.thisPartSku);
            conn.Open();
            using (SqliteDataReader dr = cmd.ExecuteReader())
            {
              if (dr.HasRows && dr.Read())
              {
                cabinetValue.thisPartCost = dr.GetDecimal("WholesalePrice");
              }
            }
          }
          var pricingColors = await _pricingColorsRepository.RetrievePricingColorsAsync(cabinetValue.thisPartColorId);
          if (pricingColors.Any())
          {
            pricingColorsDto = pricingColors.First();
            thisPartColorName = pricingColorsDto.ColorName;
            thisColorMarkup = pricingColorsDto.ColorMarkup;
            thisColorSquareFoot = pricingColorsDto.ColorSquareFoot;
          }

          thisTotalPartCost = cabinetValue.thisPartCost * (decimal)(1 + thisColorMarkup / 100);
          priceResult.Subtotal += thisTotalPartCost;
          priceResult.SubtotalFlat += cabinetValue.thisPartCost;

          var queryResultUserMarkup = await _userMarkupRepository.RetrieveUserMarkupAsync(customerOrder.userName);
          if (queryResultUserMarkup.Any())
          {
            thisUserMarkup = queryResultUserMarkup.First().UserMarkup;
          }
          priceResult.SubtotalPlus = thisTotalPartCost * (decimal)(1 + thisUserMarkup / 100);
        }

        if (refType == RefType.Order)
        {
          OrderItemDto orderItemDto = new OrderItemDto()
          {
            OrderId = order.OrderId,
            OrderSku = cabinetValue.thisPartSku,
            OrderQuantity = thisPartQty == 0 ? 1 : thisPartQty,
            Cost = cabinetValue.thisPartCost,
            MarkUp = (float)(thisTotalPartCost - cabinetValue.thisPartCost),
            UserMarkup = (float)thisTotalPartCost * (thisUserMarkup / 100)
          };
          await _orderItemRepository.StoreOrderItemAsync(orderItemDto);
        }
        if (refType == RefType.PriceReport)
        {
          // write out required part(s) to the report file
          sr.WriteLine($"{cabinetValue.thisPartSku},{cabinetValue.thisPartHeight},{cabinetValue.thisPartWidth},{cabinetValue.thisPartDepth},{thisPartColorName},{thisColorSquareFoot},{thisLinearFootCost},{cabinetValue.thisPartCost},{thisPartQty},{cabinetValue.thisPartCost * thisPartQty},{thisColorMarkup},{GlobalHelpers.Format(thisTotalPartCost)}");
        }

        // get feature cost
        var allCabinetFeature = await _featureRepository.RetrieveCabinetFeaturesAsync(cabinetValue.cabinetId);

          foreach (CabinetFeatureDto cabinetFeature in allCabinetFeature)
          {
            if (cabinetFeature.ColorId > 0)
            {
              var pricingColors = await _pricingColorsRepository.RetrievePricingColorsAsync(cabinetFeature.ColorId);
              if (pricingColors.Any())
              {
                pricingColorsDto = pricingColors.First();
                cabinetFeature.FeatureColorName = pricingColorsDto.ColorName;
                thisColorSquareFoot = pricingColorsDto.ColorSquareFoot;
                cabinetFeature.WholesalePrice = pricingColorsDto.WholesalePrice;

                float areaInSf = cabinetFeature.FeatureHeight * cabinetFeature.FeatureWidth / 144;
                cabinetFeature.FeatureCost = (decimal)(areaInSf * thisColorSquareFoot);
                if (cabinetFeature.FeatureCost == 0)
                {
                  cabinetFeature.FeatureCost = (decimal)(cabinetFeature.Quantity * cabinetFeature.WholesalePrice);
                }
                cabinetFeature.ThisTotalFeatureCost = cabinetFeature.FeatureCost * (decimal)(1 + thisColorMarkup / 100);
                priceResult.Subtotal += cabinetFeature.ThisTotalFeatureCost;
                priceResult.SubtotalFlat += cabinetFeature.FeatureCost;
                priceResult.SubtotalPlus += cabinetFeature.ThisTotalFeatureCost * (decimal)(1 + thisUserMarkup / 100);
              }
              if (refType == RefType.Order)
              {
                OrderItemDto orderItemDto = new OrderItemDto()
                {
                  OrderId = order.OrderId,
                  OrderSku = cabinetFeature.FeatureSKU,
                  OrderQuantity = cabinetFeature.Quantity == 0 ? 1 : cabinetFeature.Quantity,
                  Cost = cabinetFeature.FeatureCost,
                  MarkUp = (float)(cabinetFeature.ThisTotalFeatureCost - cabinetFeature.FeatureCost),
                  UserMarkup = (float)cabinetFeature.ThisTotalFeatureCost * thisUserMarkup / 100
                };
                await _orderItemRepository.StoreOrderItemAsync(orderItemDto);

              }
              else if (refType == RefType.PriceReport)
              {
                // write out required part(s) to the report file
                sr.WriteLine($"{cabinetFeature.FeatureSKU},{cabinetFeature.FeatureHeight},{cabinetFeature.FeatureWidth},{cabinetFeature.FeatureColorName},{thisColorSquareFoot},{thisLinearFootCost},{cabinetFeature.WholesalePrice},{cabinetFeature.Quantity},{cabinetFeature.WholesalePrice * cabinetFeature.Quantity},{thisColorMarkup},{GlobalHelpers.Format(cabinetFeature.ThisTotalFeatureCost)}");
              }
            }
          }
      }

      if (!isIsland)
      {
        float remainingWallHeight = wallHeight - totalCabinetHeight;
        // price wall color backing around cabinets
        if (remainingWallHeight > 0)
        {
          // get width from last cabinet
          float width = lastPart.thisPartWidth;
          float area = remainingWallHeight * width;
          var pricingColors = await _pricingColorsRepository.RetrievePricingColorsAsync(defaultColorId);
          if (pricingColors.Any())
          {
            pricingColorsDto = pricingColors.First();
            lastPart.thisPartSku = "PAINT";
            thisPartColorName = pricingColorsDto.ColorName;
            thisColorMarkup = pricingColorsDto.ColorMarkup;
            thisColorSquareFoot =  pricingColorsDto.ColorSquareFoot;

            lastPart.thisPartCost = (decimal)(area * thisColorSquareFoot / 144);
            thisTotalPartCost = lastPart.thisPartCost * (decimal)(1 + thisColorMarkup / 100);
            priceResult.Subtotal += thisTotalPartCost;
            priceResult.SubtotalFlat += lastPart.thisPartCost;
            priceResult.SubtotalPlus += thisTotalPartCost * (decimal)(1 + thisUserMarkup / 100);
          }

          if (refType == RefType.Order)
          {
            OrderItemDto orderItemDto = new OrderItemDto()
              {
                OrderId = order.OrderId,
                OrderSku = lastPart.thisPartSku,
                OrderQuantity = thisPartQty == 0 ? 1 : thisPartQty,
                Cost = lastPart.thisPartCost,
                MarkUp = (float)(thisTotalPartCost - lastPart.thisPartCost),
                UserMarkup = (float)thisTotalPartCost * thisUserMarkup / 100
              };
              await _orderItemRepository.StoreOrderItemAsync(orderItemDto);
          }
          else if (refType == RefType.PriceReport)
          {
            // write out required part(s) to the report file
            sr.WriteLine($"{lastPart.thisPartSku},{remainingWallHeight},{width},{thisPartColorName},{thisColorSquareFoot},{thisLinearFootCost},{lastPart.thisPartCost},{thisPartQty},{lastPart.thisPartCost * thisPartQty},{thisColorMarkup},{GlobalHelpers.Format(thisTotalPartCost)}");
          }
        }
      }


      return Result<PriceResult>.Success(priceResult);
    }
    catch (Exception ex)
    {
      GlobalHelpers.SendErrorEmail("CalcPrice", ex.Message, ex.StackTrace);
      throw;
    }
    finally
    {
      // clean up
      if (sr != null)
      {
        sr.Close();
        sr.Dispose();
      }
    }
  }
}
