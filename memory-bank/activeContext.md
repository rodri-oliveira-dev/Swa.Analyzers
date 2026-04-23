# Active context

Current focus:

- build a stable analyzer package foundation
- implement rules incrementally
- keep one rule per change set

Active rule:

- ARCH012 - Prefer DateTimeOffset over DateTime (implemented)

Open design questions:

- None for this iteration.

Known risks:

- Test detection is heuristic (based on known test-framework attributes). If a repo uses a different test framework or custom attributes, the analyzer may stay silent.
- ARCH004 uses a conservative SUT identification heuristic (based on test type name suffix and a single matching field type). If the test naming pattern differs, or if multiple candidates exist, the analyzer may stay silent.
- ARCH005 restricts Arg.Any() based on a specific NSubstitute call-chain shape (DidNotReceive/DidNotReceiveWithAnyArgs). If teams use different negative-assertion helpers/wrappers, the analyzer will report or stay silent depending on symbol shapes.
- ARCH006 detects `BeEquivalentTo(...)` + `Excluding*` only when the exclusion call is inside the options delegate passed to BeEquivalentTo. If options are built elsewhere and applied indirectly, the analyzer may stay silent.

- ARCH007 is heuristic and intentionally conservative: it targets self-referential string concatenation assignments inside loop bodies. It can still report in loops that execute only a few times (the analyzer doesn't estimate runtime iteration counts).

- ARCH008 is intentionally conservative and sink-based: it reports only when a manual composition (string `+` or interpolated string) is passed directly into known filesystem sinks. If a path is composed earlier and stored in a variable, the analyzer stays silent.

- ARCH009 is intentionally scoped to `Task`, `Task<T>`, `ValueTask`, and `ValueTask<T>`. Custom awaitables with `.GetAwaiter().GetResult()` are not flagged because they may have different blocking semantics (e.g., always-completed value types).

- ARCH010 detects overloads using a conservative heuristic (exactly one additional parameter of type `CancellationToken` with matching prefix types). Extension methods with such overloads are not currently detected. Tokens available only through complex closure captures may also be missed by `LookupSymbols`.

- ARCH011 targets only `System.Threading.Tasks.Task`, `Task<T>`, `ValueTask`, and `ValueTask<T>`. Custom awaitable types are not flagged. Unawaited async calls are reported only as expression statements (fire-and-forget), not when assigned or passed as arguments.

Pending follow-up items:

- Consider expanding supported test frameworks/attributes if needed by consumers.
- Consider adding support for extension-method overloads with CancellationToken in ARCH010.

When working on a rule, update this file with:

- active rule ID
- open design questions
- known risks
- pending follow-up items
