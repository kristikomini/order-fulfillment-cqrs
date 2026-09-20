using LogiFlow.Application.Products;
using LogiFlow.Application.Products.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LogiFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProductsController : ControllerBase
{
    private readonly ISender _sender;
    public ProductsController(ISender sender) => _sender = sender;

    /// <summary>Lists the product catalogue with live stock and available-to-promise.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProductDto>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<ProductDto>> Get(CancellationToken ct) =>
        await _sender.Send(new GetProductsQuery(), ct);
}
