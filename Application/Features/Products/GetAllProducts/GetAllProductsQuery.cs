using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

public record GetAllProductsQuery 
    : IQuery<Result<List<GetProductByIdResponse>>>;

public class GetAllProductsHandler(IAppDbContext db)
    : IQueryHandler<GetAllProductsQuery,Result<List<GetProductByIdResponse>>>
{ 
    public async Task<Result<List<GetProductByIdResponse>>> HandleAsync(
        GetAllProductsQuery query, CancellationToken ct)
    {
        var products = await db.Products
            .Select(p => new GetProductByIdResponse(
                p.Id,
                p.Name,
                p.Sku, 
                p.Description,
                p.Price,
                p.StockQuantity, 
                p.CreatedAt))
                .ToListAsync(ct);

        return 
        
        Result<List<GetProductByIdResponse>>.Success(products);
    }
}
