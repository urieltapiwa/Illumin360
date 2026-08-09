using Illumin360.SharedKernel;
using Microsoft.AspNetCore.Http;

namespace Illumin360.Web;

/// <summary>
/// Translates a <see cref="Result{T}"/> into an HTTP response using RFC 9457
/// Problem Details for failures (charter Part 7).
/// </summary>
public static class ResultExtensions
{
    /// <summary>Maps a result to <c>200 OK</c> on success or a ProblemDetails response on failure.</summary>
    /// <typeparam name="T">The success payload type.</typeparam>
    /// <param name="result">The operation result.</param>
    /// <returns>An <see cref="IResult"/> for a Minimal API endpoint.</returns>
    public static IResult ToHttpResult<T>(this Result<T> result)
        => result.IsSuccess ? Results.Ok(result.Value) : ToProblem(result.Error!);

    /// <summary>
    /// Maps a result to <c>201 Created</c> (with a Location header) on success or a
    /// ProblemDetails response on failure.
    /// </summary>
    /// <typeparam name="T">The success payload type.</typeparam>
    /// <param name="result">The operation result.</param>
    /// <param name="locationFactory">Builds the resource location URI from the created value.</param>
    /// <returns>An <see cref="IResult"/> for a Minimal API endpoint.</returns>
    public static IResult ToCreatedResult<T>(this Result<T> result, Func<T, string> locationFactory)
    {
        ArgumentNullException.ThrowIfNull(locationFactory);
        return result.IsSuccess
            ? Results.Created(locationFactory(result.Value!), result.Value)
            : ToProblem(result.Error!);
    }

    private static IResult ToProblem(Error error)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        return Results.Problem(
            title: error.Code,
            detail: error.Message,
            statusCode: status,
            type: $"https://docs.illumin360.local/errors/{error.Code}");
    }
}
