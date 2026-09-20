using System.Net.Http.Json;
using LogiFlow.Application.Orders;
using LogiFlow.Application.Products;

namespace LogiFlow.Web.Services;

/// <summary>Typed client over the LogiFlow REST API.</summary>
public sealed class LogiFlowApiClient
{
    private readonly HttpClient _http;
    public LogiFlowApiClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<ProductDto>>("api/products", ct) ?? new();

    public async Task<IReadOnlyList<OrderDto>> GetOrdersAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<OrderDto>>("api/orders", ct) ?? new();

    public async Task<OrderDto?> GetOrderAsync(Guid id, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<OrderDto>($"api/orders/{id}", ct);

    public async Task<OrderDto?> PlaceOrderAsync(
        string customerName, IEnumerable<(Guid productId, int quantity)> items, CancellationToken ct = default)
    {
        var payload = new
        {
            customerName,
            items = items.Select(i => new { productId = i.productId, quantity = i.quantity })
        };
        var response = await _http.PostAsJsonAsync("api/orders", payload, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderDto>(ct);
    }
}
