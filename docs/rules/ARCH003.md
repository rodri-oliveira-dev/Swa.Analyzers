# ARCH003: Prohibit NotBeNull() in tests

## Objective
Detect the use of `NotBeNull()` in tests and encourage more specific assertions when possible.

## Motivation
`NotBeNull()` is often a weak assertion: it confirms only the absence of `null`, but usually does not communicate *what* is expected (type, content, emptiness, presence of a value, etc.).

More specific assertions tend to:

- make the test intent clearer
- produce better failure messages
- reduce the risk of “asserting too little”

## Non-compliant code examples

```csharp
using FluentAssertions;

[Fact]
public void Test()
{
    object? value = GetValue();
    value.Should().NotBeNull();
}
```

## Compliant code examples

```csharp
using FluentAssertions;

[Fact]
public void Test()
{
    string? value = GetValue();
    value.Should().NotBeNullOrEmpty();
}
```

```csharp
using FluentAssertions;

[Fact]
public void Test()
{
    object? value = GetValue();
    value.Should().BeOfType<ExpectedType>();
}
```

## Configuration
This rule does not expose custom `.editorconfig` options in the first version.

Severity can be configured normally:

```ini
[*.cs]
dotnet_diagnostic.ARCH003.severity = info
```

## Known limitations
- The analyzer targets `NotBeNull()` from **FluentAssertions** only.
- The analyzer is intentionally limited to **test projects** (heuristic: the compilation must reference known test-framework attributes such as `Xunit.FactAttribute`, `NUnit.Framework.TestAttribute`, or `Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute`).
- The analyzer reports when the invocation is inside a known test method (for example `[Fact]` / `[Theory]`) **or** inside a *test type* (a type that contains at least one known test method).

## When not to use
If your team intentionally standardizes on `NotBeNull()` as the only allowed null-check assertion, this rule may be too strict. Prefer to adjust severity instead of disabling the rule broadly.

## Expected impact
- More expressive tests
- Less “asserting too little”
- Better failure messages and debugging signals

## Notes about false positives, heuristics, or exceptions
- This rule intentionally does **not** provide a code fix. There is no universally safe and deterministic replacement for `NotBeNull()`.
- The test-project detection is heuristic to avoid noise in non-test projects. If a test project uses a different framework not covered by the built-in list, this rule will not run.
