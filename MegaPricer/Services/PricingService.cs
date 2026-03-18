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

  public PricingService(IFeatureRepository featureRepository, ICabinetRepository cabinetRepository)
  {
    _featureRepository = featureRepository;
    _cabinetRepository = cabinetRepository;
  }

  public async Task<Result<PriceResult>> CalculatePrice(int kitchenId, int wallOrderNum, string userName, RefType refType)
  {
    if (Context.Session[userName]["PricingOff"] == "Y") return Result<PriceResult>.Success(new PriceResult(0, 0, 0));

    Kitchen kitchen = new Kitchen();
    Order order = new Order();
    CabinetDto cabinetDto = new();
    PriceResult priceResult = new(0, 0, 0);
    int defaultColor = 0;
    string thisPartColorName = "";
    float thisColorMarkup = 0;
    float thisColorSquareFoot = 0;
    float thisLinearFootCost = 0;
    float thisUserMarkup = 0;
    int thisPartQty = 0;
    decimal thisTotalPartCost = 0;
    bool isIsland = false;
    float wallHeight = 0;
    DataTable dt = new DataTable();
    StreamWriter sr = null;

    Context.Session[userName]["WallWeight"] = 0;

    try
    {
      if (wallOrderNum == 0)
      {
        return Result<PriceResult>.Failure(new Error("Session Expired", "Session expired: Log in again."));
      }
      if (kitchenId <= 0)
      {
        return Result<PriceResult>.Failure(new Error("Invalid ID", "Invalid KitchenId"));
      }
      kitchen.GetCustomerKitchen(kitchenId, userName);
      using (var conn = new SqliteConnection(ConfigurationSettings.ConnectionString))
      {
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Walls WHERE KitchenId = @kitchenId AND WallOrder = @wallOrderNum";
        cmd.Parameters.AddWithValue("@kitchenId", kitchenId);
        cmd.Parameters.AddWithValue("@wallOrderNum", wallOrderNum);
        conn.Open();
        using (SqliteDataReader dr = cmd.ExecuteReader())
        {
          do
          {
            dt = new DataTable();
            dt.BeginLoadData();
            dt.Load(dr);
            dt.EndLoadData();

          } while (!dr.IsClosed && dr.NextResult());
        }
      }

      if (dt.Rows.Count == 0)
      {
        return Result<PriceResult>.Failure(new Error("Invalid Value", "Invalid wallOrderNum"));
      }

      if (refType == RefType.PriceReport)
      {
        // Start writing to the report file
        string baseDirectory = AppContext.BaseDirectory;
        string path = baseDirectory + "Orders.csv";
        sr = new StreamWriter(path);
        sr.WriteLine($"{kitchen.Name} ({kitchen.KitchenId}) - Run time: {DateTime.Now.ToLongTimeString()} ");
        sr.WriteLine("");
        sr.WriteLine("Part Name,Part SKU,Height,Width,Depth,Color,Sq Ft $, Lin Ft $,Per Piece $,# Needed,Part Price,Add On %,Total Part Price");
      }
      else if (refType == RefType.Order)
      {
        // create a new order
        order.KitchenId = kitchenId;
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

      defaultColor = Convert.ToInt32(dt.Rows[0]["CabinetColor"]);// dt.Rows[0].Field<int>("CabinetColor");
      int wallId = Convert.ToInt32(dt.Rows[0]["WallId"]);
      isIsland = Convert.ToBoolean(dt.Rows[0]["IsIsland"]);
      wallHeight = Convert.ToSingle(dt.Rows[0]["Height"]);

      var cabinetWithCorrectWallId = await _cabinetRepository.RetrieveCabinetOnWallId(wallId);

      float totalCabinetHeight = 0;
      foreach (CabinetDto cabinetValue in cabinetWithCorrectWallId) // each cabinet
      {
        cabinetDto = cabinetValue;
        totalCabinetHeight += cabinetDto.thisPartHeight;

        if (!String.IsNullOrEmpty(cabinetDto.thisPartSku))
        {
          using (var conn = new SqliteConnection(ConfigurationSettings.ConnectionString))
          {
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM PricingSkus WHERE SKU = @sku";
            cmd.Parameters.AddWithValue("@sku", cabinetDto.thisPartSku);
            conn.Open();
            using (SqliteDataReader dr = cmd.ExecuteReader())
            {
              if (dr.HasRows && dr.Read())
              {
                cabinetDto.thisPartCost = dr.GetDecimal("WholesalePrice");
              }
            }
          }
          using (var conn = new SqliteConnection(ConfigurationSettings.ConnectionString))
          {
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM PricingColors WHERE PricingColorId = @pricingColorId";
            cmd.Parameters.AddWithValue("@pricingColorId", cabinetDto.thisPartColorId);
            conn.Open();
            using (SqliteDataReader dr = cmd.ExecuteReader())
            {
              if (dr.HasRows && dr.Read())
              {
                thisPartColorName = dr.GetString("Name");
                thisColorMarkup = dr.GetFloat("PercentMarkup");
                thisColorSquareFoot = dr.GetFloat("ColorPerSquareFoot");
              }
            }
          }
          thisTotalPartCost = cabinetDto.thisPartCost * (decimal)(1 + thisColorMarkup / 100);
          priceResult.Subtotal += thisTotalPartCost;
          priceResult.SubtotalFlat += cabinetDto.thisPartCost;

          using (var conn = new SqliteConnection(ConfigurationSettings.ConnectionString))
          {
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM UserMarkups WHERE UserName = @userName";
            cmd.Parameters.AddWithValue("@userName", userName);
            conn.Open();
            using (SqliteDataReader dr = cmd.ExecuteReader())
            {
              if (dr.HasRows && dr.Read())
              {
                thisUserMarkup = dr.GetFloat("MarkupPercent");
              }
            }
          }
          priceResult.SubtotalPlus = thisTotalPartCost * (decimal)(1 + thisUserMarkup / 100);
        }

        if (refType == RefType.Order)
        {
          // add this part to the order
          using (var conn = new SqliteConnection(ConfigurationSettings.ConnectionString))
          {
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO ORDERITEM (OrderId,SKU,Quantity,BasePrice,Markup,UserMarkup) VALUES (@orderId,@sku,@quantity,@basePrice,@markup,@userMarkup)";
            cmd.Parameters.AddWithValue("@orderId", order.OrderId);
            cmd.Parameters.AddWithValue("@sku", cabinetDto.thisPartSku);
            cmd.Parameters.AddWithValue("@quantity", thisPartQty == 0 ? 1 : thisPartQty);
            cmd.Parameters.AddWithValue("@basePrice", GlobalHelpers.Format(cabinetDto.thisPartCost));
            cmd.Parameters.AddWithValue("@markup", GlobalHelpers.Format(thisTotalPartCost - cabinetDto.thisPartCost));
            cmd.Parameters.AddWithValue("@userMarkup", GlobalHelpers.Format(thisTotalPartCost * (decimal)(1 + thisUserMarkup / 100) - thisTotalPartCost));
            conn.Open();
            cmd.ExecuteNonQuery();
          }
        }
        else if (refType == RefType.PriceReport)
        {
          // write out required part(s) to the report file
          sr.WriteLine($"{cabinetDto.thisPartSku},{cabinetDto.thisPartHeight},{cabinetDto.thisPartWidth},{cabinetDto.thisPartDepth},{thisPartColorName},{thisColorSquareFoot},{thisLinearFootCost},{cabinetDto.thisPartCost},{thisPartQty},{cabinetDto.thisPartCost * thisPartQty},{thisColorMarkup},{GlobalHelpers.Format(thisTotalPartCost)}");
        }
        else
        {
          // Just get the cost
        }

        // get feature cost
        var allCabinetFeature = await _featureRepository.RetrieveCabinetFeaturesAsync(cabinetDto.cabinetId);

        using (var conn = new SqliteConnection(ConfigurationSettings.ConnectionString)){
          foreach (CabinetFeatureDto cabinetFeature in allCabinetFeature)
          {
            if (cabinetFeature.ColorId > 0)
            {
              var cmd = conn.CreateCommand();
              cmd.CommandText = "SELECT * FROM PricingColors WHERE PricingColorId = @pricingColorId";
              cmd.Parameters.AddWithValue("@pricingColorId", cabinetFeature.ColorId);
              conn.Open();
              using (SqliteDataReader dr = cmd.ExecuteReader())
              {
                if (dr.HasRows && dr.Read())
                {
                  cabinetFeature.FeatureColorName = dr.GetString("Name");
                  float colorMarkup = dr.GetFloat("PercentMarkup");
                  thisColorSquareFoot = dr.GetFloat("ColorPerSquareFoot");
                  cabinetFeature.WholesalePrice = dr.GetFloat("WholesalePrice");

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
              }
              if (refType == RefType.Order)
              {
                // add this part to the order
                using (var conn2 = new SqliteConnection(ConfigurationSettings.ConnectionString))
                {
                  cmd = conn2.CreateCommand();
                  cmd.CommandText = "INSERT INTO ORDERITEM (OrderId,SKU,Quantity,BasePrice,Markup,UserMarkup) VALUES (@orderId,@sku,@quantity,@basePrice,@markup,@userMarkup)";
                  cmd.Parameters.AddWithValue("@orderId", order.OrderId);
                  cmd.Parameters.AddWithValue("@sku", cabinetFeature.FeatureSKU);
                  cmd.Parameters.AddWithValue("@quantity", cabinetFeature.Quantity == 0 ? 1 : cabinetFeature.Quantity);
                  cmd.Parameters.AddWithValue("@basePrice", GlobalHelpers.Format(cabinetFeature.FeatureCost));
                  cmd.Parameters.AddWithValue("@markup", GlobalHelpers.Format(cabinetFeature.ThisTotalFeatureCost - cabinetFeature.FeatureCost));
                  cmd.Parameters.AddWithValue("@userMarkup", GlobalHelpers.Format(cabinetFeature.ThisTotalFeatureCost * (decimal)(1 + thisUserMarkup / 100) - cabinetFeature.ThisTotalFeatureCost));
                  conn2.Open();
                  cmd.ExecuteNonQuery();
                }

              }
              else if (refType == RefType.PriceReport)
              {
                // write out required part(s) to the report file
                sr.WriteLine($"{cabinetFeature.FeatureSKU},{cabinetFeature.FeatureHeight},{cabinetFeature.FeatureWidth},{cabinetFeature.FeatureColorName},{thisColorSquareFoot},{thisLinearFootCost},{cabinetFeature.WholesalePrice},{cabinetFeature.Quantity},{cabinetFeature.WholesalePrice * cabinetFeature.Quantity},{thisColorMarkup},{GlobalHelpers.Format(cabinetFeature.ThisTotalFeatureCost)}");
              }

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
          float width = cabinetDto.thisPartWidth;
          float area = remainingWallHeight * width;
          using (var conn = new SqliteConnection(ConfigurationSettings.ConnectionString))
          {
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM PricingColors WHERE PricingColorId = @pricingColorId";
            cmd.Parameters.AddWithValue("@pricingColorId", defaultColor);
            conn.Open();
            using (SqliteDataReader dr = cmd.ExecuteReader())
            {
              if (dr.HasRows && dr.Read())
              {
                cabinetDto.thisPartSku = "PAINT";
                thisPartColorName = dr.GetString("Name");
                thisColorMarkup = dr.GetFloat("PercentMarkup");
                thisColorSquareFoot = dr.GetFloat("ColorPerSquareFoot");

                cabinetDto.thisPartCost = (decimal)(area * thisColorSquareFoot / 144);
                thisTotalPartCost = cabinetDto.thisPartCost * (decimal)(1 + thisColorMarkup / 100);
                priceResult.Subtotal += thisTotalPartCost;
                priceResult.SubtotalFlat += cabinetDto.thisPartCost;
                priceResult.SubtotalPlus += thisTotalPartCost * (decimal)(1 + thisUserMarkup / 100);
              }
            }
          }
          if (refType == RefType.Order)
          {
            // add this part to the order
            using (var conn = new SqliteConnection(ConfigurationSettings.ConnectionString))
            {
              var cmd = conn.CreateCommand();
              cmd.CommandText = "INSERT INTO ORDERITEM (OrderId,SKU,Quantity,BasePrice,Markup,UserMarkup) VALUES (@orderId,@sku,@quantity,@basePrice,@markup,@userMarkup)";
              cmd.Parameters.AddWithValue("@orderId", order.OrderId);
              cmd.Parameters.AddWithValue("@sku", cabinetDto.thisPartSku);
              cmd.Parameters.AddWithValue("@quantity", thisPartQty == 0 ? 1 : thisPartQty);
              cmd.Parameters.AddWithValue("@basePrice", GlobalHelpers.Format(cabinetDto.thisPartCost));
              cmd.Parameters.AddWithValue("@markup", GlobalHelpers.Format(thisTotalPartCost - cabinetDto.thisPartCost));
              cmd.Parameters.AddWithValue("@userMarkup", GlobalHelpers.Format(thisTotalPartCost * (decimal)(1 + thisUserMarkup / 100) - thisTotalPartCost));
              conn.Open();
              cmd.ExecuteNonQuery();
            }

          }
          else if (refType == RefType.PriceReport)
          {
            // write out required part(s) to the report file
            sr.WriteLine($"{cabinetDto.thisPartSku},{remainingWallHeight},{width},{thisPartColorName},{thisColorSquareFoot},{thisLinearFootCost},{cabinetDto.thisPartCost},{thisPartQty},{cabinetDto.thisPartCost * thisPartQty},{thisColorMarkup},{GlobalHelpers.Format(thisTotalPartCost)}");
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
