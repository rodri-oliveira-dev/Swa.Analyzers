// Minimal stubs so ARCH006 can detect equivalency exclusions (Excluding*)
// without referencing the real FluentAssertions package.
//
// IMPORTANT: These are NOT production implementations.

namespace FluentAssertions.Equivalency;

public sealed class EquivalencyAssertionOptions
{
    public EquivalencyAssertionOptions Excluding(Func<IMemberInfo, bool> predicate)
    {
        _ = predicate;
        return this;
    }

    public EquivalencyAssertionOptions ExcludingMissingMembers()
    {
        return this;
    }
}

public interface IMemberInfo
{
}
