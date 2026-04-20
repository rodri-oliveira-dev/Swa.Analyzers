// Minimal stubs so ARCH003/ARCH006 can detect FluentAssertions-like APIs
// without referencing the real FluentAssertions package.
//
// IMPORTANT: These are NOT production implementations.

namespace FluentAssertions;

public static class AssertionExtensions
{
    public static ObjectAssertions Should(this object? subject) => new(subject);
}

public sealed class ObjectAssertions
{
    private readonly object? _subject;

    public ObjectAssertions(object? subject)
    {
        _subject = subject;
    }

    // ARCH003 targets this method name in the FluentAssertions namespace.
    public void NotBeNull()
    {
    }

    public void NotBeNullOrEmpty()
    {
    }

    public void BeOfType<T>()
    {
    }

    // ARCH006 targets BeEquivalentTo + Excluding* calls inside the options delegate.
    public void BeEquivalentTo(
        object? expected,
        Func<global::FluentAssertions.Equivalency.EquivalencyAssertionOptions, global::FluentAssertions.Equivalency.EquivalencyAssertionOptions>? options = null)
    {
        _ = expected;
        _ = options;
    }
}
