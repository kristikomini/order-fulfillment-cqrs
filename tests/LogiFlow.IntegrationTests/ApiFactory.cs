using System.Linq;
using LogiFlow.Infrastructure.Outbox;
using LogiFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LogiFlow.IntegrationTests;

/// <summary>
/// Boots the real API (controllers, MediatR pipeline, outbox processor) but
/// swaps SQL Server for a shared in-memory database so the full flow runs
/// without external infrastructure. The interceptor is preserved so the outbox
/// still fills on save.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"logiflow-api-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove every SQL Server-bound EF registration for the context: the
            // options, the options-configuration delegate, and the context itself.
            // Leaving any of them causes "two providers registered".
            var toRemove = services.Where(d =>
                    d.ServiceType == typeof(ApplicationDbContext) ||
                    (d.ServiceType.FullName?.Contains("DbContextOptions") ?? false))
                .ToList();
            foreach (var d in toRemove)
                services.Remove(d);

            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase(_dbName);
                options.AddInterceptors(sp.GetRequiredService<ConvertDomainEventsToOutboxInterceptor>());
            });
        });
    }
}
