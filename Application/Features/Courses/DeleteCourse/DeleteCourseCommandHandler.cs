namespace Application.Features.Courses.DeleteCourse;

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

public sealed class DeleteCourseCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<DeleteCourseCommand, Result>
{
    public async Task<Result> HandleAsync(
        DeleteCourseCommand command,
        CancellationToken cancellationToken = default)
    {
     
        var course = await dbContext.Courses
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (course is null)
        {
            return Result.Failure(
                Error.NotFound("Course.NotFound", "Course not found."));
        }

        var inUse = await dbContext.AdmissionCourses
            .AnyAsync(ac => ac.CourseId == command.Id, cancellationToken);

        if (inUse)
        {
            return Result.Failure(Error.Conflict(
                "Course.InUse",
                "Cannot delete a course that is part of an existing admission."));
        }

        dbContext.Courses.Remove(course);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}