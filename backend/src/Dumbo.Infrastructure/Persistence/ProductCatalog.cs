using Dumbo.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dumbo.Infrastructure.Persistence;

internal sealed class ProductCatalog(DumboDbContext db) : IProductCatalog
{
    public async Task<IReadOnlyList<ProductSummary>> ListAsync(CancellationToken cancellationToken) =>
        await db.Set<Dumbo.Domain.Entities.Product>().AsNoTracking()
            .OrderBy(product => product.Name).ThenBy(product => product.Id)
            .Select(product => new ProductSummary(product.Id, product.Sku, product.Name,
                product.Category, product.Presentation, product.Price, product.Stock, product.ImageUrl))
            .ToListAsync(cancellationToken);
}
