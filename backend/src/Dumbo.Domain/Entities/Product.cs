namespace Dumbo.Domain.Entities;

public sealed class Product
{
    public int Id { get; init; }
    public required string Sku { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }
    public required string Presentation { get; init; }
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public string? ImageUrl { get; init; }
}
