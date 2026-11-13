namespace Api.Features.Students;

using Api.Common.Authorization;
using Api.Common.Extensions;
using Application.Abstractions.Messaging;
using Application.Features.Students.CreateStudent;
using Application.Features.Students.GetAllStudents;
using Application.Features.Students.GetStudentById;
using Domain.Common;

public static class StudentEndpoints
{
    private const string GetByIdRouteName = "GetStudentById";

    public static void MapStudentEndpoints(this IEndpointRouteBuilder app)
    {
        // Kailangan ng token ang lahat
        var group = app.MapGroup("/api/students")
            .WithTags("Students")
            .RequireAuthorization();

        // Registrar lang ang makakapamahala ng students
        group.MapGet("/", GetAll)
            .RequireAuthorization(Policies.CanManageStudents)
            .WithName("GetAllStudents")
            .WithSummary("Get all students");

        group.MapPost("/", Create)
            .RequireAuthorization(Policies.CanManageStudents)
            .WithName("CreateStudent")
            .WithSummary("Create a new student");

        group.MapGet("/{id:guid}", GetById)
            .RequireAuthorization(Policies.CanManageStudents)
            .WithName(GetByIdRouteName)
            .WithSummary("Get student by ID");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllStudentsQuery, Result<GetAllStudentsResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllStudentsQuery(), cancellationToken);
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
            ? TypedResults.CreatedAtRoute(result.Value, GetByIdRouteName, new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetStudentByIdQuery, Result<GetStudentByIdResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetStudentByIdQuery(id), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }
}