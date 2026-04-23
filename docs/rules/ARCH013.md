# ARCH013: Restrict mocking frameworks to NSubstitute

## Objective
Detect and discourage the use of mocking frameworks other than **NSubstitute** (for example **Moq** and **FakeItEasy**) when the project policy standardizes on NSubstitute.

## Motivation
Allowing multiple mocking frameworks in the same codebase tends to:

- increase cognitive load for developers and reviewers
- make test utilities harder to reuse
- fragment conventions (naming, argument matching, verification styles)
- increase maintenance cost when upgrading test dependencies

Standardizing on a single framework (NSubstitute) keeps tests more consistent.

## Non-compliant

### Moq

```csharp
using Moq;

public sealed class Tests
{
    public void Test()
    {
        var mock = new Moq.Mock<IMyService>();
        _ = Moq.It.IsAny<int>();
    }
}
```

### FakeItEasy

```csharp
public sealed class Tests
{
    public void Test()
    {
        var fake = FakeItEasy.A.Fake<IMyService>();
    }
}
```

## Compliant

```csharp
public sealed class Tests
{
    public void Test()
    {
        var substitute = NSubstitute.Substitute.For<IMyService>();
    }
}
```

## Configuration
This rule does not expose custom `.editorconfig` options in the first version.

Severity can be configured normally:

```ini
[*.cs]
dotnet_diagnostic.ARCH013.severity = info
```

Future versions may introduce an allow-list / deny-list configuration (for example, adding additional mocking frameworks to detect).

## Known limitations
- **Initial detection scope is intentionally narrow**. Version 1 detects only these frameworks:
  - Moq (root namespace: `Moq`)
  - FakeItEasy (root namespace: `FakeItEasy`)
- The analyzer relies on semantic symbols and the **root namespace** of the referenced symbol to avoid false positives from lookalike APIs.
- No code fix is provided because changing a mocking framework is not deterministic and often requires rewriting test logic.

## When not to use
- You intentionally allow multiple mocking frameworks (for example, during a migration period).
- You maintain shared libraries intended to be consumed by projects that use different mocking frameworks.

In those cases, consider suppressing the diagnostic or disabling it via `.editorconfig`.

## Expected impact
- More consistent tests across repositories and teams.
- Reduced fragmentation in test utilities and conventions.
- Clearer guidance for new code: use NSubstitute.

## Notes about false positives / heuristics
- The analyzer is designed to avoid false positives by checking **semantic namespaces** (not just text matching).
- It reports on common usage sites (using directives, invocations, object creation, and type declarations).
- It intentionally skips reporting inside the mocking framework namespace itself (useful for analyzer tests that stub framework APIs in source).
