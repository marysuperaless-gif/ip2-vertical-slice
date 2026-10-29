using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Features.Products.GetProductById;
using Domain.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Products.UpdateProduct;

public class UpdateProductHandler(IAppDbContext db, IValidator<UpdateProductCommand> validator)
    : ICommandHandler<UpdateProductCommand, Result<GetProductByIdResponse>>
{
    public async Task<Result<GetProductByIdResponse>> HandleAsync(
        UpdateProductCommand command, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            var message = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<GetProductByIdResponse>.Failure(
                Error.Validation("Product.Validation", message));
        }

        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == command.Id, ct);
        if (product is null)
            return Result<GetProductByIdResponse>.Failure(
                Error.NotFound("Product.NotFound", "Product not found."));

        var conflict = await db.Products
            .AnyAsync(p => p.Sku == command.Sku && p.Id != command.Id, ct);
        if (conflict)
            return Result<GetProductByIdResponse>.Failure(
                Error.Conflict("Product.SkuConflict", "SKU already exists."));

        product.Name = command.Name;
        product.Sku = command.Sku;
        product.Description = command.Description;
        product.Price = command.Price;
        product.StockQuantity = command.StockQuantity;

        await db.SaveChangesAsync(ct);

        return Result<GetProductByIdResponse>.Success(new GetProductByIdResponse(
            product.Id, product.Name, product.Sku, product.Description,
            product.Price, product.StockQuantity, product.CreatedAt));
    }
}