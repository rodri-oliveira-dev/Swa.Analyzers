# ARCH014: Prefer Is.Equivalent over NSubstitute Arg.Is

## Objective

Encourage the use of the team's standard assertion library (`Is.Equivalent`) instead of NSubstitute's `Arg.Is` for value matching in test assertions.

## Motivation

Using a standardized assertion library across the team provides several benefits:

- **Consistency**: All tests use the same assertion patterns, making them easier to read and maintain
- **Better error messages**: The team's standard library typically provides more descriptive failure messages
- **Reduced coupling**: Tests become less dependent on NSubstitute-specific APIs
- **Improved maintainability**: Centralized assertion logic is easier to update and evolve

## Non-compliant Code

```csharp
// Using NSubstitute Arg.Is for value matching
substitute.Received().Do(NSubstitute.Arg.Is<int>(x => x > 0));

// Using Arg.Is with simple value matching
substitute.Received().Process(NSubstitute.Arg.Is(42));
```

## Compliant Code

```csharp
// Using the team's standard library
substitute.Received().Do(Is.Equivalent(42));

// Using the team's standard library with predicates
substitute.Received().Do(Is.Equivalent(x => x > 0));
```

## Configuration

This rule does not support any configuration options.

## Known Limitations

- The rule only detects `Arg.Is` calls from the `NSubstitute` namespace
- The rule only reports diagnostics within test types (classes that contain test methods)
- The rule does not provide a code fix because the appropriate replacement depends on the specific use case and the team's standard library API

## When Not to Use

This rule may not be suitable if:

- Your team does not have a standardized assertion library
- Your team explicitly prefers NSubstitute's `Arg.Is` API
- You are working on legacy code where migration would be too costly

## Expected Impact

- **Code Quality**: Improved consistency and readability across test suites
- **Maintainability**: Easier to update assertion patterns centrally
- **Team Standards**: Enforces adoption of team-wide testing conventions

## Notes

- This rule reports diagnostics for any `Arg.Is` usage within a test type, regardless of whether the specific method has a test attribute
- The rule detects both `Arg.Is<T>(predicate)` and `Arg.Is(value)` overloads
- The rule works with all common test frameworks (xUnit, NUnit, MSTest)
