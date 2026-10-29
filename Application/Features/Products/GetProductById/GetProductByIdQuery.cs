using Application.Abstractions.Messaging;
using Domain.Common;

public record GetProductByIdQuery(Guid Id) : IQuery<Result<GetProductByIdResponse>>;

public record GetProductByIdResponse(
    Guid Id,
    string Name,
    string Sku,
    string? Description,
    decimal Price,
    int StockQuantity,
    DateTime CreatedAt);