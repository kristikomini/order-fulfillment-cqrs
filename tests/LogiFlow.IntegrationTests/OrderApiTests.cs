using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LogiFlow.Application.Orders;
using LogiFlow.Application.Products;
using Xunit;

namespace LogiFlow.IntegrationTests;

public class OrderApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public OrderApiTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_ready_returns_healthy()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health/ready");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Catalogue_is_seeded()
    {
        var client = _factory.CreateClient();
        var products = await client.GetFromJsonAsync<List<ProductDto>>("/api/products");
        products.Should().NotBeNull();
        products!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Placing_an_order_confirms_then_gets_fulfilled_by_the_outbox()
    {
        var client = _factory.CreateClient();
        var products = await client.GetFromJsonAsync<List<ProductDto>>("/api/products");
        var box = products!.First(p => p.Sku == "BOX-M");

        var placeResponse = await client.PostAsJsonAsync("/api/orders", new
        {
            customerName = "ACME Logistics",
            items = new[] { new { productId = box.Id, quantity = 3 } }
        });

        placeResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var placed = await placeResponse.Content.ReadFromJsonAsync<OrderDto>();
        placed!.Status.Should().Be("Confirmed");

        // The outbox processor should react to OrderConfirmed and fulfill the order.
        OrderDto? current = null;
        for (var attempt = 0; attempt < 25; attempt++)
        {
            current = await client.GetFromJsonAsync<OrderDto>($"/api/orders/{placed.Id}");
            if (current!.Status == "Fulfilled") break;
            await Task.Delay(250);
        }

        current!.Status.Should().Be("Fulfilled", "the outbox processor publishes OrderConfirmed which triggers fulfillment");
    }

    [Fact]
    public async Task Ordering_more_than_available_is_rejected()
    {
        var client = _factory.CreateClient();
        var products = await client.GetFromJsonAsync<List<ProductDto>>("/api/products");
        var labels = products!.First(p => p.Sku == "LABEL-A6"); // seeded with only 50

        var response = await client.PostAsJsonAsync("/api/orders", new
        {
            customerName = "Greedy Corp",
            items = new[] { new { productId = labels.Id, quantity = 999_999 } }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        order!.Status.Should().Be("Rejected");
    }
}
