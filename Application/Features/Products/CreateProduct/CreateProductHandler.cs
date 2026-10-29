using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Products.CreateProduct;

public class CreateProductHandler(IAppDbContext db, IValidator<CreateProductCommand> validator)
    : ICommandHandler<CreateProductCommand, Result<CreateProductResponse>>
{
    public async Task<Result<CreateProductResponse>> HandleAsync(
        CreateProductCommand command, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            var message = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<CreateProductResponse>.Failure(
                Error.Validation("Product.Validation", message));
        }

        var skuExists = await db.Products.AnyAsync(p => p.Sku == command.Sku, ct);
        if (skuExists)
            return Result<CreateProductResponse>.Failure(
                Error.Conflict("Product.SkuConflict", "SKU already exists."));

        var product = new Product
        {
            Name = command.Name,
            Sku = command.Sku,
            Description = command.Description,
            Price = command.Price,
            StockQuantity = command.StockQuantity,
            CreatedAt = DateTime.UtcNow
        };

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);

        return Result<CreateProductResponse>.Success(new CreateProductResponse(
            product.Id, product.Name, product.Sku, product.Description,
            product.Price, product.StockQuantity, product.CreatedAt));
    }
}
