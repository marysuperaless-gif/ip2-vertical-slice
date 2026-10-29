
namespace Application.Features.Admissions.GetAllAdmissions;

using Application.Abstractions.Messaging;
using Domain.Common;

public sealed record
    GetAllAdmissionsQuery() :
    IQuery<Result<GetAllAdmissionsResponse>>;

public sealed record
    GetAllAdmissionsResponse(

    List<AdmissionSummaryResponse> Admissions);

public sealed record
    AdmissionSummaryResponse(
        Guid ID,
        Guid StudentId,
        string StudentName,
        string AcademicYear,
        DateTime AdmissionDate,
        int CourseCount);
