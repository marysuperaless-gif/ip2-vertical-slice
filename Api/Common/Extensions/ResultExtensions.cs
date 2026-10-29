namespace Api.Common.Extensions;

using Domain.Common;
using Microsoft.AspNetCore.Mvc;

public static class ResultExtensions
{
    public static IResult ToProblemDetails<T>(this Result<T> result)
        => ToProblem(result.Error!);

    public static IResult ToProblemDetails(this Result result)
        => ToProblem(result.Error!);

    private static IResult ToProblem(Error error)
    {
        var statusCode = GetStatusCode(error.Type);

        return Results.Problem(new ProblemDetails
        {
            Title = GetTitle(error.Type),
            Detail = error.Message,
            Status = statusCode,
            Type = $"https://httpstatuses.io/{statusCode}",
            Extensions = { ["errorCode"] = error.Code }
        });
    }

    private static int GetStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _                  => StatusCodes.Status500InternalServerError
    };

    private static string GetTitle(ErrorType type) => type switch
    {
        ErrorType.Validation => "Validation failed",
        ErrorType.Unauthorized => "Unauthorized",
        ErrorType.Forbidden => "Forbidden",
        ErrorType.NotFound => "Resource not found",
        ErrorType.Conflict => "Conflict",
        _                  => "An unexpected error occurred"
    };
}