// Minimal stubs so ARCH013 can detect FakeItEasy-like APIs
// without referencing the real FakeItEasy package.
//
// IMPORTANT: These are NOT production implementations.

namespace FakeItEasy;

public static class A
{
    public static T Fake<T>() => default!;
}
