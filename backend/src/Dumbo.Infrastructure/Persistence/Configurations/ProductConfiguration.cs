using Dumbo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dumbo.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products", table =>
        {
            table.HasCheckConstraint("ck_products_price", "price >= 0");
            table.HasCheckConstraint("ck_products_stock", "stock >= 0");
        });
        builder.HasIndex(product => product.Sku).IsUnique();
        builder.Property(product => product.Sku).HasMaxLength(80);
        builder.Property(product => product.Name).HasMaxLength(200);
        builder.Property(product => product.Category).HasMaxLength(100);
        builder.Property(product => product.Presentation).HasMaxLength(100);
        builder.Property(product => product.Price).HasPrecision(12, 2);
        builder.Property(product => product.ImageUrl).HasMaxLength(2048);
    }
}
