using LogiFlow.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LogiFlow.Application.Products.Queries;

public sealed record GetProductsQuery : IRequest<IReadOnlyList<ProductDto>>;

public sealed class GetProductsHandler : IRequestHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    private readonly IApplicationDbContext _db;
    public GetProductsHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _db.Products
            .AsNoTracking()
            .OrderBy(p => p.Sku)
            .ToListAsync(cancellationToken);

        return products.Select(ProductDto.From).ToList();
    }
}
