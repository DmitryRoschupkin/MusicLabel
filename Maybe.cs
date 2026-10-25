namespace MusicLabel;

public abstract record Maybe<T>
{
    private Maybe() {}

    public sealed record Some : Maybe<T>
    {
        public T Value { get; }
        public Some(T value)
        {
            ArgumentNullException.ThrowIfNull(value);
            Value = value;
        }
        public void Deconstruct(out T value) => value = Value;
    }

    public sealed record None : Maybe<T>;
}

public class Maybe
{
    public static Maybe<T> Some<T>(T value) => new Maybe<T>.Some(value);
    public static Maybe<T> None<T>() => new Maybe<T>.None();
}