using LogiFlow.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogiFlow.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Sku).HasMaxLength(64).IsRequired();
        builder.HasIndex(p => p.Sku).IsUnique();

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.UnitPrice).HasPrecision(18, 2);
        builder.Property(p => p.StockOnHand).IsRequired();
        builder.Property(p => p.StockReserved).IsRequired();

        // Optimistic concurrency: guards concurrent reservations against the same product.
        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.Ignore(p => p.AvailableToPromise);
        builder.Ignore(p => p.DomainEvents);
    }
}
