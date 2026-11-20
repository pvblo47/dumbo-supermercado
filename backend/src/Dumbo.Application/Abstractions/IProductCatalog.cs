namespace Dumbo.Application.Abstractions;

public sealed record ProductSummary(int Id, string Sku, string Name, string Category,
    string Presentation, decimal Price, int Stock, string? ImageUrl);

public interface IProductCatalog
{
    Task<IReadOnlyList<ProductSummary>> ListAsync(CancellationToken cancellationToken);
}
