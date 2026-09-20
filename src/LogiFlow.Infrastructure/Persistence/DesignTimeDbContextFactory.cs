using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LogiFlow.Infrastructure.Persistence;

/// <summary>
/// Used only by the EF Core tools (<c>dotnet ef migrations</c>) so schema
/// changes never require booting the API host. The connection string here is
/// a design-time placeholder; migrations are SQL-generated, not executed.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=localhost,1433;Database=LogiFlow;User Id=sa;Password=Your_strong_Pass123;TrustServerCertificate=True;Encrypt=False")
            .Options;

        return new ApplicationDbContext(options);
    }
}
