using System.Diagnostics;

namespace MusicLabel;

public static class ResultExtensions
{
    public static Result<U> Map<T, U>(this Result<T> result, Func<T, U> f) => result switch
    {
        Result<T>.Success(var value) => Result<U>.Ok(f(value)),
        Result<T>.Failure(var error) => Result<U>.Fail(error),
        _ => throw new UnreachableException()
    };

    public static Result<U> Bind<T, U>(this Result<T> result, Func<T, Result<U>> f) => result switch
    {
        Result<T>.Success(var value) => f(value),
        Result<T>.Failure(var error) => Result<U>.Fail(error),
        _ => throw new UnreachableException()
    };

    public static Result<T> ToResult<T>(this Maybe<T> maybe, Func<Error> onMissing) => maybe switch
    {
        Maybe<T>.Some(var value) => Result<T>.Ok(value),
        _ => Result<T>.Fail(onMissing())
    };
}