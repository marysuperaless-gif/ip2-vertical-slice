namespace Api.Features.Students;

using Api.Common.Extensions;
using Application.Abstractions.Messaging;
using Application.Features.Students.CreateStudent;
using Application.Features.Students.GetAllStudents;
using Application.Features.Students.GetStudentById;
using Domain.Common;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

public static class StudentEndpoints
{
    private const string GetByIdRouteName = "GetStudentById";
    public static void MapStudentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/students")
            .WithTags("Students");

        group.MapGet("/", GetAll)
            .WithName("GetAllStudents")
            .WithSummary("Get all students");

        group.MapPost("/", Create)
            .WithName("CreateStudent")
            .WithSummary("Create a new student");

        group.MapGet("/{id:guid}", GetById)
            .WithName(GetByIdRouteName)
            .WithSummary("Get student by ID");

    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllStudentsQuery, Result<GetAllStudentsResponse>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetAllStudentsQuery();
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateStudentCommand command,
        ICommandHandler<CreateStudentCommand, Result<CreateStudentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetByIdRouteName", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id, 
        IQueryHandler<GetStudentByIdQuery, Result<GetStudentByIdResponse>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetStudentByIdQuery(id);
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();

  
    }
}
