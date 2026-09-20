using LogiFlow.Domain.Products;

namespace LogiFlow.Application.Products;

public sealed record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    decimal UnitPrice,
    int StockOnHand,
    int StockReserved,
    int AvailableToPromise)
{
    public static ProductDto From(Product p) =>
        new(p.Id, p.Sku, p.Name, p.UnitPrice, p.StockOnHand, p.StockReserved, p.AvailableToPromise);
}
