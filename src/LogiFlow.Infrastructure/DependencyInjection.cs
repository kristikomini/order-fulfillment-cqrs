using LogiFlow.Application.Common.Interfaces;
using LogiFlow.Infrastructure.Outbox;
using LogiFlow.Infrastructure.Persistence;
using LogiFlow.Infrastructure.Telemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LogiFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

        services.AddSingleton<ConvertDomainEventsToOutboxInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString, sql =>
                sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null));
            options.AddInterceptors(sp.GetRequiredService<ConvertDomainEventsToOutboxInterceptor>());
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddMetrics();
        services.AddSingleton<ILogiFlowMetrics, LogiFlowMetrics>();

        services.AddHostedService<OutboxProcessor>();

        return services;
    }
}
