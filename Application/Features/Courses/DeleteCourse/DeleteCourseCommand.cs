namespace Application.Features.Courses.DeleteCourse;

using Application.Abstractions.Messaging;
using Domain.Common;

public sealed record
    DeleteCourseCommand(Guid Id) : ICommand<Result>;


