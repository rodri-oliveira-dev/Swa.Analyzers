# ARCH010: Enforce CancellationToken propagation

## Objective
Detect invocations of methods that can accept a `CancellationToken` when a token is already available in the current scope but is not being passed.

## Motivation
Cooperative cancellation is a cornerstone of responsive and scalable async code. When a method receives a `CancellationToken` and then calls another method that supports cancellation, failing to propagate the token:

- Prevents the called work from being cancelled when the caller is cancelled.
- Forces the caller to wait for the full completion of uncancellable sub-operations.
- Makes APIs less predictable, because consumers expect cancellation to flow through the call stack.

## Non-compliant

```csharp
using System.Threading;
using System.Threading.Tasks;

public sealed class Service
{
    public Task DoWorkAsync(int id)
    {
        return Task.CompletedTask;
    }

    public Task DoWorkAsync(int id, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public sealed class Consumer
{
    // ARCH010: Pass the available CancellationToken to 'DoWorkAsync'.
    public async Task ExecuteAsync(Service service, CancellationToken cancellationToken)
    {
        await service.DoWorkAsync(1);
    }
}
```

```csharp
using System.Threading;
using System.Threading.Tasks;

public sealed class Service
{
    // Optional parameter — still reportable when omitted.
    public Task DoWorkAsync(int id, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

public sealed class Consumer
{
    // ARCH010: Pass the available CancellationToken to 'DoWorkAsync'.
    public async Task ExecuteAsync(Service service, CancellationToken cancellationToken)
    {
        await service.DoWorkAsync(1);
    }
}
```

## Compliant

```csharp
using System.Threading;
using System.Threading.Tasks;

public sealed class Service
{
    public Task DoWorkAsync(int id)
    {
        return Task.CompletedTask;
    }

    public Task DoWorkAsync(int id, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public sealed class Consumer
{
    public async Task ExecuteAsync(Service service, CancellationToken cancellationToken)
    {
        await service.DoWorkAsync(1, cancellationToken);
    }
}
```

```csharp
using System.Threading;
using System.Threading.Tasks;

public sealed class Consumer
{
    // No token available — analyzer stays silent.
    public async Task ExecuteAsync(Service service)
    {
        await service.DoWorkAsync(1);
    }
}
```

## Configuration
This rule does not expose custom `.editorconfig` options in the first version, but the infrastructure is designed to support future configuration (for example, excluding specific method patterns or types).

Severity can be configured normally:

```ini
[*.cs]
dotnet_diagnostic.ARCH010.severity = warning
```

## Known limitations
- The analyzer detects overloads using a conservative heuristic: the overload must have exactly one more parameter than the invoked method, the prefix parameters must match in type, and the additional parameter must be `CancellationToken`. Overloads with different parameter arrangements are not flagged.
- Extension methods with overloads that accept `CancellationToken` are not currently detected. This is a known gap that may be addressed in a future version.
- The analyzer uses `SemanticModel.LookupSymbols` to detect available tokens. Tokens that are reachable only through complex scoping (for example, captured in closures from distant scopes in ways that bypass normal symbol lookup) may not be recognized.
- The analyzer does not evaluate whether a `CancellationToken` variable is actually usable (for example, if it has already been cancelled or disposed). It only checks availability.

## When not to use
In rare cases you may intentionally omit a token:

- When the sub-operation must complete fully even if the parent operation is cancelled (for example, flushing state to disk during cancellation).
- When the called API is known to ignore the token and passing it adds noise.

In those cases, suppress the diagnostic with a clear comment explaining the intentional omission.

## Expected impact
- Better cancellation responsiveness throughout the application.
- Reduced latency when users or systems request cancellation.
- More predictable async API behavior.

## Notes about false positives / heuristics
The analyzer stays silent when:

- No `CancellationToken` is available in the current lexical scope (parameters, locals, fields, or properties).
- The invocation already passes a `CancellationToken` argument.
- The invoked method has no overload or optional parameter that accepts `CancellationToken`.
- The only overload with `CancellationToken` has a fundamentally different parameter signature.
