### New Rules

| Rule ID | Category    | Severity | Notes                                             |
| ------- | ----------- | -------- | ------------------------------------------------- |
| ARCH001 | Reliability | Warning  | Avoid async void outside standard event handlers. |
| ARCH002 | Reliability | Warning  | Avoid Task.ContinueWith. Prefer await.            |
| ARCH003 | TestQuality | Info     | Avoid FluentAssertions NotBeNull() in tests.      |
