namespace Application.Features.Courses.UpdateCourse;

using FluentValidation;

public sealed class UpdateCourseValidator : AbstractValidator<UpdateCourseCommand>
{
    public UpdateCourseValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Credits).GreaterThan(0).LessThanOrEqualTo(6);
    }
}
