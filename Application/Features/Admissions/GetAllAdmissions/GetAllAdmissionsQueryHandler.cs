namespace Application.Features.Admissions.GetAllAdmissions;

using
Application.Abstractions.Data;
using
Application.Abstractions.Messaging;
using Application.Features.Admissions.GetAllAdmissions;
using
Domain.Common;
using
Microsoft.EntityFrameworkCore;

public sealed class GetAllAdmissionsQueryHandler(IAppDbContext dbContext)
    :IQueryHandler<GetAllAdmissionsQuery, Result<GetAllAdmissionsResponse>>
{
    public async Task<Result<GetAllAdmissionsResponse>>HandleAsync(
        GetAllAdmissionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var admissions = await dbContext.Admissions
        .Select(a => new AdmissionSummaryResponse(
            a.Id,
            a.StudentId,
            a.Student.FirstName + " " + a.Student.LastName,
            a.AcademicYear,
            a.AdmissionDate,
            a.AdmissionCourses.Count))
        .ToListAsync(cancellationToken);
    return Result<GetAllAdmissionsResponse>.Success(
        new GetAllAdmissionsResponse(admissions));
    }
}