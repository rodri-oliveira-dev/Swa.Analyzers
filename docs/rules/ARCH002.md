# ARCH002: Avoid Task.ContinueWith

## Objective
Prevent the use of `Task.ContinueWith(...)` and encourage `await` as the preferred asynchronous flow.

## Motivation
`ContinueWith` tends to produce callback-style code that is harder to read and maintain than linear `async`/`await` code.

Using `await` usually provides:

- **Better readability** (code remains linear)
- **Better exception propagation** (exceptions flow naturally through the awaited `Task`)
- **Better maintainability** (less manual continuation wiring and fewer subtle scheduling pitfalls)

## Non-compliant

```csharp
using System.Threading.Tasks;

public sealed class Sample
{
    public Task ExecuteAsync()
    {
        return Task.Delay(10)
            .ContinueWith(_ => DoWork());
    }

    private static void DoWork() { }
}
```

```csharp
using System.Threading.Tasks;

public sealed class Sample
{
    public Task<int> ExecuteAsync()
    {
        return Task.FromResult(1)
            .ContinueWith(t => t.Result + 1);
    }
}
```

## Compliant

```csharp
using System.Threading.Tasks;

public sealed class Sample
{
    public async Task ExecuteAsync()
    {
        await Task.Delay(10);
        DoWork();
    }

    private static void DoWork() { }
}
```

```csharp
using System.Threading.Tasks;

public sealed class Sample
{
    public async Task<int> ExecuteAsync()
    {
        var value = await Task.FromResult(1);
        return value + 1;
    }
}
```

## Configuration
This rule does not expose custom `.editorconfig` options in the first version.

Severity can be configured normally:

```ini
[*.cs]
dotnet_diagnostic.ARCH002.severity = warning
```

## Known limitations
- This rule flags `ContinueWith` called on `Task` and `Task<T>`.
- It does not attempt to validate whether a particular `ContinueWith` usage is “safe” in a given context; it always recommends `await` as the default.
- No code fix is provided because replacing `ContinueWith` with `await` is not deterministic and may change semantics (return types, scheduling, cancellation behavior, synchronization context usage, etc.).

## When not to use
In rare cases, you may intentionally use `ContinueWith` for low-level task composition or to avoid `async` state machines in very hot paths.

If you keep `ContinueWith`, ensure you understand and review:

- TaskScheduler / synchronization context implications
- Exception observation and propagation
- Cancellation behavior

## Expected impact
- More consistent `async`/`await` usage across the codebase
- Fewer continuation chains and callback-style async code
- More predictable exception propagation patterns

## Notes about false positives / heuristics
The analyzer uses semantic information to target only `System.Threading.Tasks.Task.ContinueWith` and `Task<T>.ContinueWith`.
