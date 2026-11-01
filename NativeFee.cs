using System.Runtime.InteropServices;

namespace MusicLabel;

internal static class NativeMethods
{
    [DllImport("musiclabel",
        EntryPoint = "ml_service_fee_cents",
        CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern long ServiceFeeCents(long amountCents, int feeBps);
}

public static class NativeFee
{
    public const long MaxAmountCents = 100_000_000_000_000;
    public const int MaxFeeBps = 10000;
    
    public static Result<long> Calculate(long amountCents, int feeBps) => 
        CalculateWith(NativeMethods.ServiceFeeCents, amountCents, feeBps);

    public static Result<long> CalculateWith(Func<long, int, long> nativeCall, long amountCents, int feeBps)
    {
        if (amountCents < 0 || amountCents > MaxAmountCents)
            return Result<long>.Fail(Errors.InvalidFeeInput(
                $"amount {amountCents} cents is outside 0..{MaxAmountCents}."));
        if (feeBps < 0 || feeBps > MaxFeeBps)
            return Result<long>.Fail(Errors.InvalidFeeInput(
                $"rate {feeBps} bps is outside 0..{MaxFeeBps}."));
        
        try
        {
            long fee = nativeCall(amountCents, feeBps);
            return fee < 0
                ? Result<long>.Fail(Errors.NativeRejectedInput(amountCents, feeBps))
                : Result<long>.Ok(fee);
        }
        catch (DllNotFoundException ex)
        {
            return Result<long>.Fail(Errors.NativeLibraryMissing(ex.Message));
        }
        catch (Exception ex) when (ex is EntryPointNotFoundException or BadImageFormatException)
        {
            return Result<long>.Fail(Errors.NativeBindingFailed(ex.Message));
        }
    }
}