namespace Api.Features.Courses;

using Api.Common.Authorization;
using Api.Common.Extensions;
using Application.Abstractions.Messaging;
using Application.Features.Courses.CreateCourse;
using Application.Features.Courses.DeleteCourse;
using Application.Features.Courses.GetAllCourses;
using Application.Features.Courses.GetCourseById;
using Application.Features.Courses.UpdateCourse;
using Domain.Common;

public static class CourseEndpoints
{
    private const string GetByIdRouteName = "GetCourseById";

    public static void MapCourseEndpoints(this IEndpointRouteBuilder app)
    {
        
        var group = app.MapGroup("/api/courses")
            .WithTags("Courses")
            .RequireAuthorization();

        
        group.MapGet("/", GetAll)
            .WithName("GetAllCourses")
            .WithSummary("Get all courses");

        group.MapGet("/{id:guid}", GetById)
            .WithName(GetByIdRouteName)
            .WithSummary("Get course by ID");

       
        group.MapPost("/", Create)
            .RequireAuthorization(Policies.CanManageCourses)
            .WithName("CreateCourse")
            .WithSummary("Create a new course");

        group.MapPut("/{id:guid}", Update)
            .RequireAuthorization(Policies.CanManageCourses)
            .WithName("UpdateCourse")
            .WithSummary("Update an existing course");

        group.MapDelete("/{id:guid}", Delete)
            .RequireAuthorization(Policies.CanManageCourses)
            .WithName("DeleteCourse")
            .WithSummary("Delete a course");
    }

    public sealed record UpdateCourseRequest(string Title, string Code, int Credits);

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllCoursesQuery, Result<GetAllCoursesResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllCoursesQuery(), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetCourseByIdQuery, Result<GetCourseByIdResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetCourseByIdQuery(id), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateCourseCommand command,
        ICommandHandler<CreateCourseCommand, Result<CreateCourseResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, GetByIdRouteName, new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateCourseRequest request,
        ICommandHandler<UpdateCourseCommand, Result<UpdateCourseResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCourseCommand(id, request.Title, request.Code, request.Credits);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteCourseCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteCourseCommand(id), cancellationToken);
        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.ToProblemDetails();
    }
}
