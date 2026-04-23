// Minimal stubs so ARCH005 can detect NSubstitute-like APIs
// without referencing the real NSubstitute package.
//
// IMPORTANT: These are NOT production implementations.

namespace NSubstitute;

public static class Substitute
{
    public static T For<T>() where T : class
    {
        return default!;
    }
}

public static class Arg
{
    // ARCH005 targets this method name in the NSubstitute.Arg type.
    public static T Any<T>()
    {
        return default!;
    }

    // ARCH014 targets this method name in the NSubstitute.Arg type.
    public static T Is<T>(T value) => default!;
    public static T Is<T>(Func<T, bool> predicate) => default!;
}

public static class SubstituteExtensions
{
    // ARCH005 allows Arg.Any() only when the receiving call is preceded by one of these methods.
    public static T DidNotReceive<T>(this T substitute) where T : class
    {
        return substitute;
    }

    public static T DidNotReceiveWithAnyArgs<T>(this T substitute) where T : class
    {
        return substitute;
    }
}
