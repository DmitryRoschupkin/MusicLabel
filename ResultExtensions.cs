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

    public static (IReadOnlyList<T> Values, IReadOnlyList<Error> Errors) Partition<T>(
        this IEnumerable<Result<T>> results)
    {
        var values = new List<T>();
        var errors = new List<Error>();
        foreach (var result in results)
        {
            switch (result)
            {
                case Result<T>.Success(var value): values.Add(value); break;
                case Result<T>.Failure(var error): errors.Add(error); break;
            }
        }

        return (values, errors);
    }

    public static Result<T> MapError<T>(this Result<T> result, Func<Error, Error> f) => result switch
    {
        Result<T>.Success => result,
        Result<T>.Failure(var error) => Result<T>.Fail(f(error)),
        _ => throw new UnreachableException()
    };
}