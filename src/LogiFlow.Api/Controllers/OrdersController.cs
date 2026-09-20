using LogiFlow.Application.Orders;
using LogiFlow.Application.Orders.PlaceOrder;
using LogiFlow.Application.Orders.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LogiFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class OrdersController : ControllerBase
{
    private readonly ISender _sender;
    public OrdersController(ISender sender) => _sender = sender;

    /// <summary>Places an order and reserves stock. Returns the resulting order (confirmed or rejected).</summary>
    [HttpPost]
    [ProducesResponseType<OrderDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> Place([FromBody] PlaceOrderRequest request, CancellationToken ct)
    {
        var command = new PlaceOrderCommand(
            request.CustomerName,
            request.Items.Select(i => new PlaceOrderItem(i.ProductId, i.Quantity)).ToList());

        var order = await _sender.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    /// <summary>Gets a single order by id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken ct)
    {
        var order = await _sender.Send(new GetOrderByIdQuery(id), ct);
        return order is null ? NotFound() : Ok(order);
    }

    /// <summary>Lists all orders, newest first.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<OrderDto>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<OrderDto>> Get(CancellationToken ct) =>
        await _sender.Send(new GetOrdersQuery(), ct);
}

public sealed record PlaceOrderRequest(string CustomerName, IReadOnlyList<PlaceOrderRequestItem> Items);
public sealed record PlaceOrderRequestItem(Guid ProductId, int Quantity);
