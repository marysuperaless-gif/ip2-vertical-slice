using Application.Abstractions.Messaging;
using Application.Features.Products.GetProductById;
using Domain.Common;

namespace Application.Features.Products.UpdateProduct;

public record UpdateProductCommand(
    Guid Id,
    string Name,
    string Sku,
    string? Description,
    decimal Price,
    int StockQuantity) 
    : ICommand<Result<GetProductByIdResponse>>;
public record UpdateProductRequest(
    string Name,
    string Sku,
    string? Description,
    decimal Price,
    int StockQuantity);