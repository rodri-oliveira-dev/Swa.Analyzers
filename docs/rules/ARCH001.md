# ARCH001: Avoid async void outside event handlers

## Objective
Prevent `async void` in methods, local functions and anonymous functions, except for standard event handlers.

## Motivation
`async void` cannot be awaited and propagates exceptions through the synchronization context instead of through a `Task`. This makes failures harder to observe, test and compose. In application code, `async Task` is the safer default.

## Invalid

```csharp
public async void PublishAsync()
{
    await _client.SendAsync();
}
```

```csharp
Action action = async () =>
{
    await Task.Delay(1);
};
```

## Valid

```csharp
public async Task PublishAsync()
{
    await _client.SendAsync();
}
```

```csharp
button.Click += async (sender, e) =>
{
    await Task.Delay(1);
};
```

```csharp
public async void OnClick(object? sender, EventArgs e)
{
    await Task.Delay(1);
}
```

## How to configure
This rule does not expose custom `.editorconfig` options in the first version.

Severity can be configured normally:

```ini
# .editorconfig
[*.cs]
dotnet_diagnostic.ARCH001.severity = warning
```

## Known limitations
- The rule treats the classic `object sender, EventArgs e` pattern, including derived `EventArgs`, as an allowed event handler shape.
- Custom delegate-based events that do not inherit from `EventArgs` are not exempted by this first version.
- The rule intentionally does not offer a code fix because changing a return type from `void` to `Task` may require changes in callers, interfaces, overrides or delegates.

## When not to use
Do not disable this rule broadly. If the code truly must follow an event-handler signature, keep the event handler narrow and move the real work into an `async Task` method.

## Expected impact
- Fewer hidden async failures
- Better testability
- Cleaner async composition
- Lower risk of fire-and-forget mistakes disguised as regular control flow

## False positives and heuristics
The main heuristic is the event-handler exemption. If the solution relies heavily on custom event delegates, consider extending the rule in a later version to recognize project-specific delegate patterns.
