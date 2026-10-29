using Application.Abstractions.Messaging;
using Domain.Common;

namespace Application.Features.Products.DeleteProduct;

public record DeleteProductCommand(Guid Id) 
    : ICommand<Result<Guid>>;