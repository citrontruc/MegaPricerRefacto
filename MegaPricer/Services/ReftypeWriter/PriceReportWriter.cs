using MegaPricer.Data;
using MegaPricer.Dtos;

namespace MegaPricer.Services;

public class PriceReportWriter : IRefTypeWriter
{
    private StreamWriter _streamWriter;

  public async Task InitializeWriter(Order order, Kitchen kitchen)
    {
        // Start writing to the report file
        string baseDirectory = AppContext.BaseDirectory;
        string path = baseDirectory + "Orders.csv";
        _streamWriter = new StreamWriter(path);
        _streamWriter.WriteLine($"{kitchen.Name} ({kitchen.KitchenId}) - Run time: {DateTime.Now:T} ");
        _streamWriter.WriteLine("");
        _streamWriter.WriteLine("Part Name,Part SKU,Height,Width,Depth,Color,Sq Ft $, Lin Ft $,Per Piece $,# Needed,Part Price,Add On %,Total Part Price");
    }

    public async Task WriteCabinetItem(OrderItemDto orderItemDto)
    {
        _streamWriter.WriteLine($"{orderItemDto.OrderSku},{orderItemDto.ItemHeight},{orderItemDto.ItemWidth},{orderItemDto.ItemDepth},{orderItemDto.ItemColorName},{orderItemDto.pricingColorsDto.ColorSquareFoot},{orderItemDto.LinearFootCost},{orderItemDto.Cost},{orderItemDto.OrderQuantity},{orderItemDto.Cost * orderItemDto.OrderQuantity},{orderItemDto.pricingColorsDto.ColorMarkup},{GlobalHelpers.Format(orderItemDto.TotalPartCost)}");
    }

    public void Dispose()
    {
        if (_streamWriter != null)
        {
            _streamWriter.Close();
            _streamWriter.Dispose();
        }
    }
}
