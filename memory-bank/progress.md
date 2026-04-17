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

Notable design decisions:

- ARCH003 limits execution to test projects by checking for known test-framework attribute types in the compilation (xUnit/NUnit/MSTest). It then reports FluentAssertions `NotBeNull()` when the invocation is inside a known test method **or** inside a *test type* (a type that contains at least one known test method).
- ARCH004 enforces `_sut` only when it can confidently identify the SUT: it infers the SUT type from the test type name suffix (Tests/Test/Specs/Spec) and reports only when there is exactly one instance field whose type name matches that inferred SUT type.

Also track:

- notable design decisions
- recurring false-positive patterns
- improvements needed in shared infrastructure
