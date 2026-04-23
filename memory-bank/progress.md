# Progress

Track rule progress here.

Suggested format:

- ARCH001: done / in progress / planned
- ARCH002: done / in progress / planned
- ARCH003: done / in progress / planned

Current status:

- ARCH001: done
- ARCH002: done
- ARCH003: done
- ARCH004: done
- ARCH005: done
- ARCH006: done
- ARCH007: done
- ARCH008: done
- ARCH009: done
- ARCH010: done
- ARCH011: done
- ARCH012: done

Notable design decisions:

- ARCH003 limits execution to test projects by checking for known test-framework attribute types in the compilation (xUnit/NUnit/MSTest). It then reports FluentAssertions `NotBeNull()` when the invocation is inside a known test method **or** inside a *test type* (a type that contains at least one known test method).
- ARCH004 enforces `_sut` only when it can confidently identify the SUT: it infers the SUT type from the test type name suffix (Tests/Test/Specs/Spec) and reports only when there is exactly one instance field whose type name matches that inferred SUT type.
- ARCH005 restricts NSubstitute `Arg.Any<T>()` usage by allowing it only as a direct argument inside `DidNotReceive()` / `DidNotReceiveWithAnyArgs()` call chains.
- ARCH006 warns on FluentAssertions equivalency exclusions by reporting any `Excluding*` invocation (from `FluentAssertions.Equivalency`) found inside the options delegate passed to `BeEquivalentTo(...)`. The rule is scoped to test contexts using the same heuristic as ARCH003.
- ARCH011 prohibits blocking operations and unawaited asynchronous calls inside instance and static constructors. It reuses the same semantic task-type detection from ARCH009 and adds fire-and-forget detection for discarded Task/ValueTask invocations.

Also track:

- notable design decisions
- recurring false-positive patterns
- improvements needed in shared infrastructure
