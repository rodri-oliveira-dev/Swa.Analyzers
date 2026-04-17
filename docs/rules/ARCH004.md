# ARCH004: Enforce _sut naming in unit tests

## Objective
Enforce the `_sut` naming convention for the primary *system under test* (SUT) field in unit test types.

## Motivation
Using a consistent name for the main subject under test reduces cognitive load when reading tests:

- `_sut` is easy to recognize quickly
- it avoids bikeshedding on variable names for the main object under test
- it makes test setup patterns more uniform across the codebase

## Non-compliant code examples

```csharp
public sealed class Calculator { }

public sealed class CalculatorTests
{
    private readonly Calculator _calculator = new();

    [Fact]
    public void Adds() { }
}
```

## Compliant code examples

```csharp
public sealed class Calculator { }

public sealed class CalculatorTests
{
    private readonly Calculator _sut = new();

    [Fact]
    public void Adds() { }
}
```

## Configuration
This rule does not expose custom `.editorconfig` options in the first version.

Severity can be configured normally:

```ini
[*.cs]
dotnet_diagnostic.ARCH004.severity = info
```

## Known limitations
- The analyzer is intentionally limited to **test projects** (heuristic: the compilation must reference known test-framework attributes such as `Xunit.FactAttribute`, `NUnit.Framework.TestAttribute`, or `Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute`).
- The analyzer is intentionally conservative to avoid noise:
  - it only analyzes **test types** (types that contain at least one known test method)
  - it only reports when it can infer a clear single SUT candidate field
- SUT identification heuristic (current behavior):
  1. Infer the expected SUT type name from the test type name by stripping a supported suffix (`Tests`, `Test`, `Specs`, `Spec`).
     - Example: `OrderServiceTests` -> inferred SUT type name `OrderService`.
  2. Inside that test type, find instance fields whose **type name** matches the inferred SUT type name.
  3. If there is exactly one such field and it is not named `_sut`, report `ARCH004`.

## When not to use
- If your team uses a different SUT naming convention (for example `sut` or `subject`).
- If your tests intentionally involve multiple equally-important subjects under test per test type.

## Expected impact
- More uniform and easier-to-scan test code
- Lower naming churn in code reviews

## Notes about false positives, heuristics, or exceptions
- This rule intentionally does **not** provide a code fix. Renaming a field may require updating many references and can be disruptive.
- If the test class name does not follow a supported suffix convention, the analyzer stays silent.
