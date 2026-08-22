using Microsoft.AspNetCore.Http;
using Payflow.Shared.Kernel;

namespace Payflow.Shared.Api;

/// <summary>
/// Maps an Application-layer <see cref="Result"/> onto an HTTP response as an RFC 9457 problem
/// details document. Every service's minimal API endpoints funnel failures through this so a
/// client sees the same failure shape (title/status/code) no matter which service answered.
/// </summary>
public static class ResultExtensions
{
    public static IResult ToProblem(this Result result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("Cannot convert a successful result to a problem response.");

        return Problem(result.Error);
    }

    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : Problem(result.Error);

    private static IResult Problem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Failure => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            title: error.Code,
            detail: error.Message,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?> { ["errorCode"] = error.Code });
    }
}
