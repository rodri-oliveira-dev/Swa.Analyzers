### New Rules

| Rule ID | Category    | Severity | Notes                                                    |
| ------- | ----------- | -------- | -------------------------------------------------------- |
| ARCH001 | Reliability | Warning  | Avoid async void outside standard event handlers.        |
| ARCH002 | Reliability | Warning  | Avoid Task.ContinueWith. Prefer await.                   |
| ARCH003 | TestQuality | Info     | Avoid FluentAssertions NotBeNull() in tests.             |
| ARCH004 | TestQuality | Info     | Enforce _sut naming for the system under test.           |
| ARCH005 | TestQuality | Info     | Restrict NSubstitute Arg.Any() usage.                    |
| ARCH006 | TestQuality | Info     | Warn on FluentAssertions exclusions in BeEquivalentTo.   |
| ARCH007 | Performance | Info     | Detect string concatenation inside loops.                |
| ARCH008 | Reliability | Info     | Prohibit manual path composition in filesystem sinks.    |
| ARCH009 | Reliability | Warning  | Prohibit synchronous blocking of async operations.       |
| ARCH010 | Reliability | Warning  | Enforce CancellationToken propagation.                   |
| ARCH011 | Reliability | Warning  | Prohibit asynchronous or blocking logic in constructors. |
| ARCH012 | Reliability | Info     | Prefer DateTimeOffset over DateTime.                     |
| ARCH013 | TestQuality | Info     | Restrict mocking frameworks to NSubstitute.              |
