namespace MusicLabel;

public abstract record Result<T>
{
    private Result() {}
    
    public static Result<T> Ok(T value) => new Success(value);
    public static Result<T> Fail(Error error) => new Failure(error);
    
    public sealed record Success : Result<T>
    {
        public T Value { get; }

        public Success(T value)
        {
            ArgumentNullException.ThrowIfNull(value);
            Value = value;
        }

        public void Deconstruct(out T value) => value = Value;
    }
    
    public sealed record Failure : Result<T>
    {
        public Error Error { get; }

        public Failure(Error error)
        {
            ArgumentNullException.ThrowIfNull(error);
            Error = error;
        }

        public void Deconstruct(out Error error) => error = Error;
    }
};