namespace Application.Features.Admissions.CreateAdmission;

using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class CreateAdmissionCommandHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateAdmissionCommand, Result<CreateAdmissionResponse>>
{
    public async Task<Result<CreateAdmissionResponse>> HandleAsync(
        CreateAdmissionCommand command,
        CancellationToken cancellationToken = default)
    {
        var student = await dbContext.Students
            .FirstOrDefaultAsync(s => s.Id == command.StudentId, cancellationToken);

        if (student is null)
        {
            return Result<CreateAdmissionResponse>.Failure(
                Error.NotFound("Student.NotFound", "Student not found."));
        };

        var courses = await dbContext.Courses
            .Where(c => command.CourseIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        if (courses.Count != command.CourseIds.Count)
        {
            return Result<CreateAdmissionResponse>.Failure(
                Error.Validation("Course.NotFound", "One or more courses not found."));
        }

        var admission = new Admission
        {
            StudentId = command.StudentId,
            AcademicYear = command.AcademicYear,
            AdmissionDate = DateTime.UtcNow
        };

        foreach (var course in courses)
        {
            admission.AdmissionCourses.Add(new AdmissionCourse
            {
                CourseId = course.Id
            });
        }

        dbContext.Admissions.Add(admission);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new CreateAdmissionResponse(
            admission.Id,
            admission.StudentId,
            admission.AcademicYear,
            admission.AdmissionDate,
            courses.Select(c => new CourseResponse(c.Id, c.Title, c.Code)).ToList()
        );

        return Result<CreateAdmissionResponse>.Success(response);
    }
}
