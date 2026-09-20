using LogiFlow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogiFlow.Application.Orders.Queries;

public sealed record GetOrderByIdQuery(Guid Id) : IRequest<OrderDto?>;

public sealed record GetOrdersQuery : IRequest<IReadOnlyList<OrderDto>>;

public sealed class GetOrderByIdHandler : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    private readonly IApplicationDbContext _db;
    public GetOrderByIdHandler(IApplicationDbContext db) => _db = db;

    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        return order is null ? null : OrderDto.From(order);
    }
}

public sealed class GetOrdersHandler : IRequestHandler<GetOrdersQuery, IReadOnlyList<OrderDto>>
{
    private readonly IApplicationDbContext _db;
    public GetOrdersHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        return orders.Select(OrderDto.From).ToList();
    }
}
