using LogiFlow.Domain.Orders;
using LogiFlow.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace LogiFlow.Application.Common.Interfaces;

/// <summary>
/// Persistence seam the Application layer depends on. The concrete
/// implementation lives in Infrastructure; this keeps handlers free of any
/// EF Core provider or the outbox wiring.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Order> Orders { get; }
    DbSet<Product> Products { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
