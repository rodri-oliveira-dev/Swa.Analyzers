# ARCH011: Prohibit asynchronous or blocking logic in constructors

## Objective
Prevent constructors from containing blocking operations (.Result, .Wait(), .GetAwaiter().GetResult()) or unawaited asynchronous calls (Task/ValueTask returned and discarded). Constructors should remain fast and non-blocking.

## Motivation
Performing blocking or asynchronous work inside a constructor leads to several engineering problems:

- **Deadlocks**: Blocking on asynchronous operations inside a constructor carries the same deadlock risk as anywhere else, and constructors are especially hard to refactor to async because they cannot be async themselves.
- **Thread-pool starvation**: Synchronous waits in constructors block the caller during object creation, reducing scalability.
- **Hidden async work**: Discarding a Task/ValueTask without awaiting, assigning to a field, or chaining hides fire-and-forget work that may fail silently.
- **Composability**: Constructors that perform I/O or blocking work make types harder to use in tests and dependency injection containers.

Prefer an async factory method (e.g., `static async Task<MyClass> CreateAsync(...)`) when asynchronous initialization is needed.

## Non-compliant

```csharp
using System.Threading.Tasks;

public sealed class Service
{
    private readonly int _value;

    public Service(Task<int> fetcher)
    {
        // Blocks the calling thread
        _value = fetcher.Result;
    }
}

public sealed class Loader
{
    public Loader(Task dataTask)
    {
        // Blocks the calling thread
        dataTask.Wait();
    }
}

public sealed class Processor
{
    public Processor(Task<int> fetcher)
    {
        // Same blocking risk as .Result
        var value = fetcher.GetAwaiter().GetResult();
    }
}

public sealed class BackgroundStarter
{
    public BackgroundStarter()
    {
        // Fire-and-forget: exceptions are lost, ordering is unclear
        StartAsync();
    }

    private Task StartAsync() => Task.CompletedTask;
}
```

## Compliant

```csharp
using System.Threading.Tasks;

public sealed class Service
{
    private readonly int _value;

    private Service(int value)
    {
        _value = value;
    }

    public static async Task<Service> CreateAsync(Task<int> fetcher)
    {
        var value = await fetcher;
        return new Service(value);
    }
}

public sealed class Loader
{
    private readonly Task _dataTask;

    public Loader(Task dataTask)
    {
        // Assignment only: no blocking or fire-and-forget
        _dataTask = dataTask;
    }
}
```

## Configuration
This rule does not expose custom `.editorconfig` options in the first version.

Severity can be configured normally:

```ini
[*.cs]
dotnet_diagnostic.ARCH011.severity = warning
```

## Known limitations
- The analyzer targets only `System.Threading.Tasks.Task`, `Task<T>`, `ValueTask`, and `ValueTask<T>`. It does not flag blocking on custom awaitable types.
- `.Wait()` overloads with cancellation tokens or timeouts are still reported because they retain the same blocking semantics.
- Unawaited async calls are reported only when the Task/ValueTask is used as an expression statement (fire-and-forget). Assigning to a local variable or passing as an argument is not reported.
- No code fix is provided because converting constructor logic to an async factory requires changing the public API and callers.

## When not to use
In rare scenarios you may intentionally perform synchronous work in a constructor:

- Trivial in-memory initialization that does not block on external I/O.
- Legacy code paths where changing the constructor surface is not feasible.

In those cases, suppress the diagnostic with a clear comment explaining why the pattern is unavoidable.

## Expected impact
- Fewer deadlocks during object construction.
- Better thread-pool utilization and scalability.
- Clearer lifecycle: async initialization is explicit via factory methods.

## Notes about false positives / heuristics
The analyzer uses semantic information to ensure it reports only on members defined by the genuine BCL task types inside constructors. It stays silent for:

- Custom types that happen to define a `.Result` property.
- Custom types that happen to define a `.Wait()` method.
- Custom awaitables with `.GetAwaiter().GetResult()` that are not `Task`/`ValueTask`.
- Assignments and argument-passing of Task/ValueTask values.
- Blocking calls outside constructors (those are covered by ARCH009).
