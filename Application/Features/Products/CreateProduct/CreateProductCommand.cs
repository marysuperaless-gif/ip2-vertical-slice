using Application.Abstractions.Messaging;
using Domain.Common;


namespace Application.Features.Products.CreateProduct;
public record CreateProductCommand(
    string Name,
    string Sku,
    string? Description,
    decimal Price,
    int StockQuantity)
    : ICommand<Result<CreateProductResponse>>;
public record CreateProductResponse(
    Guid Id,
    string Name,
    string Sku,
    string? Description,
    decimal Price,
    int StockQuantity,
    DateTime CreatedAt);
