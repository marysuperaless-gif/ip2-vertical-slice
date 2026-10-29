using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Products.DeleteProduct;

public class DeleteProductHandler(IAppDbContext db, IValidator<DeleteProductCommand> validator)
    : ICommandHandler<DeleteProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> HandleAsync(
        DeleteProductCommand command, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            var message = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<Guid>.Failure(Error.Validation("Product.Validation", message));
        }

        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == command.Id, ct);
        if (product is null)
            return Result<Guid>.Failure(
                Error.NotFound("Product.NotFound", "Product not found."));

        db.Products.Remove(product);
        await db.SaveChangesAsync(ct);

        return Result<Guid>.Success(product.Id);
    }
}