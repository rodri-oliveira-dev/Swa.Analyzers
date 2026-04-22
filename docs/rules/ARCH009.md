# ARCH009: Prohibit synchronous blocking of asynchronous operations

## Objective
Prevent synchronous blocking of asynchronous operations by detecting the use of `.Result`, `.Wait()`, and `.GetAwaiter().GetResult()` on `Task`, `Task<T>`, `ValueTask`, and `ValueTask<T>`.

## Motivation
Blocking asynchronously-started work on the calling thread is a well-known source of deadlocks and scalability degradation.

- **Deadlocks**: When a blocking call runs on a thread that carries a `SynchronizationContext` (e.g., UI threads or legacy ASP.NET request threads), the awaited continuation may try to post back to the same context, which is now blocked.
- **Thread-pool starvation**: Synchronous waits consume threads that could otherwise process new work, reducing overall throughput.
- **Exception wrapping**: `.Result` wraps exceptions in `AggregateException`, hiding the original exception type and complicating error handling.

Prefer `await` so the caller yields the thread and resumes naturally when the operation completes.

## Non-compliant

```csharp
using System.Threading.Tasks;

public sealed class Sample
{
    public int FetchSync(Task<int> fetcher)
    {
        // Risks deadlock and hides original exception type
        return fetcher.Result;
    }

    public void ExecuteSync(Task task)
    {
        // Blocks the calling thread
        task.Wait();
    }

    public int FetchViaAwaiter(Task<int> fetcher)
    {
        // Same blocking risk as .Result
        return fetcher.GetAwaiter().GetResult();
    }
}
```

## Compliant

```csharp
using System.Threading.Tasks;

public sealed class Sample
{
    public async Task<int> FetchAsync(Task<int> fetcher)
    {
        return await fetcher;
    }

    public async Task ExecuteAsync(Task task)
    {
        await task;
    }
}
```

## Configuration
This rule does not expose custom `.editorconfig` options in the first version.

Severity can be configured normally:

```ini
[*.cs]
dotnet_diagnostic.ARCH009.severity = warning
```

## Known limitations
- The analyzer targets only `System.Threading.Tasks.Task`, `Task<T>`, `ValueTask`, and `ValueTask<T>`. It does not flag blocking on custom awaitable types.
- `.Wait()` overloads with cancellation tokens or timeouts are still reported because they retain the same blocking semantics.
- No code fix is provided because converting blocking code to `await` often requires changing the containing method signature (return type, `async` modifier) and can affect callers.

## When not to use
In very rare scenarios you may intentionally block:

- Console application `Main` methods that cannot be `async` in older target frameworks.
- Legacy third-party API boundaries where you cannot change the signature to `async`.

In those cases, suppress the diagnostic with a clear comment explaining why the block is unavoidable.

## Expected impact
- Fewer deadlocks in UI and legacy web applications.
- Better thread-pool utilization and scalability.
- Cleaner exception handling (no `AggregateException` wrapping).

## Notes about false positives / heuristics
The analyzer uses semantic information to ensure it reports only on members defined by the genuine BCL task types. It stays silent for:

- Custom types that happen to define a `.Result` property.
- Custom types that happen to define a `.Wait()` method.
- Custom awaitables with `.GetAwaiter().GetResult()` that are not `Task`/`ValueTask`.
