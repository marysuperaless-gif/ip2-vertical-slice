using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Products.GetProductById;

public class GetProductByIdHandler(IAppDbContext db)
    : IQueryHandler<GetProductByIdQuery, Result<GetProductByIdResponse>>
{
    public async Task<Result<GetProductByIdResponse>> HandleAsync(
        GetProductByIdQuery query, CancellationToken ct)
    {
        var product = await db.Products
            .FirstOrDefaultAsync(p => p.Id == query.Id, ct);

        if (product is null)
            return Result<GetProductByIdResponse>.Failure(
                Error.NotFound("Product.NotFound", "Product not found."));

        return Result<GetProductByIdResponse>.Success(new GetProductByIdResponse(
            product.Id,
            product.Name,
            product.Sku,
            product.Description,
            product.Price,
            product.StockQuantity,
            product.CreatedAt));
    }
}