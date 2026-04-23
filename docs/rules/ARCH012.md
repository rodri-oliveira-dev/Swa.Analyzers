# ARCH012: Prefer DateTimeOffset over DateTime

## Objective
Encourage the use of `DateTimeOffset` instead of `DateTime` in type declarations that are controlled by the project, reducing ambiguity about time zone intent.

## Motivation
`System.DateTime` is ambiguous: it can represent local, UTC, or unspecified time, and its `Kind` property is often ignored or misinterpreted. This leads to real bugs in serialization, persistence, and distributed systems.

`DateTimeOffset` always carries an offset relative to UTC, making the intent explicit and eliminating a common class of timezone-related defects.

## Non-compliant

```csharp
using System;

public sealed class Order
{
    // Ambiguous: is this local, UTC, or unspecified?
    public DateTime PlacedAt { get; set; }
}

public sealed class Processor
{
    public void Process(DateTime timestamp) { }

    public DateTime GetTimestamp() => DateTime.UtcNow;
}
```

## Compliant

```csharp
using System;

public sealed class Order
{
    // Explicit offset; always unambiguous
    public DateTimeOffset PlacedAt { get; set; }
}

public sealed class Processor
{
    public void Process(DateTimeOffset timestamp) { }

    public DateTimeOffset GetTimestamp() => DateTimeOffset.UtcNow;
}
```

## Configuration
This rule does not expose custom `.editorconfig` options in the first version. Future versions may support an allow-list for specific type names or namespaces where `DateTime` is intentionally used.

Severity can be configured normally:

```ini
[*.cs]
dotnet_diagnostic.ARCH012.severity = info
```

## Known limitations
- The analyzer does not report `DateTime` when used with `var` (e.g., `var dt = DateTime.UtcNow;`). This is intentional: without an explicit type annotation, there is no clear declaration site to flag.
- `DateTime` inside types that derive from `System.Attribute` is not reported, because attributes often serialize values through framework mechanisms that may require `DateTime`.
- `DateTime` in explicit interface implementations, implicit interface implementations, and overrides is not reported, because the type is imposed by an external contract.
- `DateTime` in `this` parameters of extension methods is not reported when the `this` parameter is `DateTime` (e.g., `public static void DoWork(this DateTime dt)`), because the analyzer targets the parameter declaration only when the team controls the type choice.
- No code fix is provided because changing `DateTime` to `DateTimeOffset` may break callers, serialization contracts, or implicit conversions.

## When not to use
You may intentionally keep `DateTime` when:

- A framework API or external contract explicitly requires `DateTime` (e.g., legacy Entity Framework mappings, some serializer configurations).
- You need `DateTime` for interop with unmanaged code or specific serialization formats.
- The codebase has a documented convention that `DateTime` is always UTC and the convention is strictly enforced by other means.

In those cases, suppress the diagnostic with a clear justification comment.

## Expected impact
- Fewer timezone-related bugs caused by ambiguous `DateTime` values.
- Clearer data contracts when persisting or transmitting timestamps.
- Improved correctness in distributed systems where offset-aware values are essential.

## Notes about false positives / heuristics
The analyzer is intentionally conservative:

- It skips interface implementations and overrides because the type is dictated by the contract.
- It skips attribute-derived types because attributes are tightly coupled to runtime serialization.
- It skips `var` declarations to avoid flagging inferred usages where the developer did not explicitly choose the type.
- It flags `DateTime[]` and `DateTime?` because the same ambiguity applies to arrays and nullable wrappers.
