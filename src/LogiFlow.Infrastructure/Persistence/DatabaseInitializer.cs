using LogiFlow.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LogiFlow.Infrastructure.Persistence;

/// <summary>
/// Applies pending migrations at startup and seeds a small product catalogue so
/// the app is demonstrable immediately after <c>docker compose up</c>.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");

        if (db.Database.IsRelational())
        {
            logger.LogInformation("Applying database migrations...");
            await db.Database.MigrateAsync(ct);
        }

        if (!await db.Products.AnyAsync(ct))
        {
            logger.LogInformation("Seeding product catalogue...");
            db.Products.AddRange(
                Product.Create("PALLET-EU", "Euro pallet 1200x800", 18.50m, 500),
                Product.Create("BOX-M", "Shipping box medium", 1.20m, 2000),
                Product.Create("BOX-L", "Shipping box large", 1.80m, 1500),
                Product.Create("WRAP-500", "Stretch wrap 500mm", 6.90m, 300),
                Product.Create("LABEL-A6", "Thermal label A6 (roll)", 12.00m, 50));
            await db.SaveChangesAsync(ct);
        }
    }
}
