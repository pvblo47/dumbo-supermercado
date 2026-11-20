using Dumbo.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Dumbo.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(IProductCatalog catalog) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ProductSummary>> List(CancellationToken cancellationToken) =>
        catalog.ListAsync(cancellationToken);
}
