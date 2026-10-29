using Api.Common.Extensions;
using Application.Abstractions.Messaging;
using Application.Features.Courses.UpdateCourse;
using Application.Features.Products.CreateProduct;
using Application.Features.Products.DeleteProduct;
using Application.Features.Products.GetAllProducts;
using Application.Features.Products.GetProductById;
using Application.Features.Products.UpdateProduct;
using Domain.Common;

namespace Api.Features.Products;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products").WithTags("Products");

        group.MapPost("/", async (
            CreateProductCommand command,
            ICommandHandler<CreateProductCommand, Result<CreateProductResponse>> handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(command, ct);
            return result.IsSuccess
                ? Results.CreatedAtRoute("GetProductById",
                    new { id = result.Value.Id }, result.Value)
                : result.ToProblemDetails();
        })
        .WithName("CreateProduct")
        .WithSummary("Create a new product");

        group.MapGet("/", async (
            IQueryHandler<GetAllProductsQuery, Result<List<GetProductByIdResponse>>> handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new GetAllProductsQuery(), ct);
            return Results.Ok(result.Value);
        })
        .WithName("GetAllProducts")
        .WithSummary("Get all products");

        group.MapGet("/{id:guid}", async (
            Guid id,
            IQueryHandler<GetProductByIdQuery, Result<GetProductByIdResponse>> handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new GetProductByIdQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GetProductById")
        .WithSummary("Get a product by id");

        group.MapPut("/{id:guid}", async (
            Guid id, UpdateProductRequest body,
            ICommandHandler<UpdateProductCommand, Result<GetProductByIdResponse>> handler,
            CancellationToken ct) =>
        {
            var command = new UpdateProductCommand(
                id, body.Name, body.Sku, body.Description,
                body.Price, body.StockQuantity);
            var result = await handler.HandleAsync(command, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("UpdateProduct")
        .WithSummary("Update an existing product");

        group.MapDelete("/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteProductCommand, Result<Guid>> handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new DeleteProductCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        })
        .WithName("DeleteProduct")
        .WithSummary("Delete a product");
    }
}
