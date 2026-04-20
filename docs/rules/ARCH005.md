# ARCH005: Restrict usage of Arg.Any()

## Objective
Restrict the usage of `NSubstitute.Arg.Any<T>()` in test code, allowing it only in **explicitly accepted** negative-assertion conventions.

## Motivation
`Arg.Any<T>()` is a very broad argument matcher. Overuse tends to produce tests that are:

- less expressive (the test does not communicate which argument values matter)
- less strict (tests pass even when the system under test uses unexpected values)
- harder to refactor safely (weak expectations can hide regressions)

The project convention accepts `Arg.Any<T>()` only in a narrow scenario: negative assertions using NSubstitute's `DidNotReceive()` / `DidNotReceiveWithAnyArgs()` call chain.

## Non-compliant code examples

### Broad matcher in positive assertions

```csharp
substitute.Received().Do(Arg.Any<int>());
```

### Broad matcher in normal invocation/setup

```csharp
substitute.Do(Arg.Any<int>());
```

## Compliant code examples

### Allowed by convention: negative assertion chain

```csharp
substitute.DidNotReceive().Do(Arg.Any<int>());
```

```csharp
substitute.DidNotReceiveWithAnyArgs().Do(Arg.Any<int>());
```

## Configuration
This rule does not expose custom `.editorconfig` options in the first version.

Severity can be configured normally:

```ini
[*.cs]
dotnet_diagnostic.ARCH005.severity = info
```

## Known limitations
- The analyzer is intentionally limited to **test projects** (heuristic: the compilation must reference known test-framework attributes such as `Xunit.FactAttribute`, `NUnit.Framework.TestAttribute`, or `Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute`).
- The analyzer is intentionally limited to **test contexts** (test methods and helper methods inside *test types*), following the same approach as other test-quality rules in this package.

## When not to use
- If your team relies heavily on broad matchers as part of the test philosophy.
- If your test codebase uses a different mocking framework or conventions.

## Expected impact
- Tests become more precise and intention-revealing.
- Lower chance of accidentally allowing unexpected argument values.

## Notes about false positives, heuristics, or exceptions
### Semantic identification
The rule uses semantic analysis to ensure it only targets `NSubstitute.Arg.Any<T>()`.
This avoids false positives from unrelated APIs with the same name (for example `CustomSubstitute.Arg.Any<T>()`).

### Allowed exceptions (project convention)
`Arg.Any<T>()` is allowed only when:

1. It appears as the **direct argument value** of a method invocation (implicit conversions are ignored), and
2. That invocation is part of a call chain whose receiver is `DidNotReceive()` or `DidNotReceiveWithAnyArgs()`, and
3. The `DidNotReceive*` method is from the **NSubstitute** namespace (to avoid allowing lookalike APIs).

This means the analyzer will still report patterns like:

```csharp
substitute.DidNotReceive().Do(Arg.Any<int>() + 1); // reported
```

### No code fix
This rule does not provide a code fix because replacing `Arg.Any<T>()` requires test-specific intent and is not a deterministic or universally safe transformation.
