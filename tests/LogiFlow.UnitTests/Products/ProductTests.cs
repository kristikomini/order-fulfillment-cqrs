using FluentAssertions;
using LogiFlow.Domain.Common;
using LogiFlow.Domain.Products;
using Xunit;

namespace LogiFlow.UnitTests.Products;

public class ProductTests
{
    [Fact]
    public void Create_normalizes_sku_and_starts_with_zero_reserved()
    {
        var p = Product.Create(" pallet-eu ", "Euro pallet", 18.5m, 100);

        p.Sku.Should().Be("PALLET-EU");
        p.StockReserved.Should().Be(0);
        p.AvailableToPromise.Should().Be(100);
    }

    [Fact]
    public void TryReserve_reduces_available_to_promise()
    {
        var p = Product.Create("BOX-M", "Box", 1m, 10);

        p.TryReserve(4).Should().BeTrue();

        p.StockReserved.Should().Be(4);
        p.AvailableToPromise.Should().Be(6);
        p.StockOnHand.Should().Be(10); // physical stock unchanged until shipment
    }

    [Fact]
    public void TryReserve_fails_and_does_not_mutate_when_insufficient()
    {
        var p = Product.Create("BOX-M", "Box", 1m, 3);

        p.TryReserve(5).Should().BeFalse();

        p.StockReserved.Should().Be(0);
        p.AvailableToPromise.Should().Be(3);
    }

    [Fact]
    public void ShipReserved_removes_stock_from_both_reserved_and_on_hand()
    {
        var p = Product.Create("BOX-M", "Box", 1m, 10);
        p.TryReserve(4);

        p.ShipReserved(4);

        p.StockReserved.Should().Be(0);
        p.StockOnHand.Should().Be(6);
    }

    [Fact]
    public void ShipReserved_throws_when_shipping_more_than_reserved()
    {
        var p = Product.Create("BOX-M", "Box", 1m, 10);
        p.TryReserve(2);

        var act = () => p.ShipReserved(3);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ReleaseReservation_returns_stock_to_available()
    {
        var p = Product.Create("BOX-M", "Box", 1m, 10);
        p.TryReserve(4);

        p.ReleaseReservation(4);

        p.StockReserved.Should().Be(0);
        p.AvailableToPromise.Should().Be(10);
    }
}
