// Minimal stubs so the analyzers that scope to test contexts (ARCH003-ARCH006/005)
// can run in this SampleApp without adding external dependencies.
//
// IMPORTANT: These are NOT production implementations.

namespace Xunit;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class FactAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class TheoryAttribute : Attribute
{
}
