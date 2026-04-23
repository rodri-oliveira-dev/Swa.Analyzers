// Minimal stubs so ARCH013 can detect Moq-like APIs
// without referencing the real Moq package.
//
// IMPORTANT: These are NOT production implementations.

namespace Moq;

public sealed class Mock<T>
{
}

public static class It
{
    public static T IsAny<T>() => default!;
}
