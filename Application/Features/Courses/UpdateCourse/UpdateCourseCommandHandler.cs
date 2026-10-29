namespace Application.Features.Courses.UpdateCourse;

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

public sealed class UpdateCourseCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<UpdateCourseCommand, Result<UpdateCourseResponse>>
{
    public async Task<Result<UpdateCourseResponse>> HandleAsync(
        UpdateCourseCommand command,
        CancellationToken cancellationToken = default)
    {
       
        var course = await dbContext.Courses
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (course is null)
        {
            return Result<UpdateCourseResponse>.Failure(
                Error.NotFound("Course.NotFound", "Course not found."));
        }

        
        var codeTaken = await dbContext.Courses
            .AnyAsync(c => c.Code == command.Code && c.Id != command.Id, cancellationToken);

        if (codeTaken)
        {
            return Result<UpdateCourseResponse>.Failure(
                Error.Conflict("Course.CodeAlreadyExists",
                    $"A course with code '{command.Code}' already exists."));
        }

        
        course.Title = command.Title;
        course.Code = command.Code;
        course.Credits = command.Credits;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<UpdateCourseResponse>.Success(new UpdateCourseResponse(
            course.Id, course.Title, course.Code, course.Credits));
    }
}