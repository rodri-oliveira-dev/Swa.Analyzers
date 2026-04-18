# Company.Analyzers

Internal Roslyn analyzers focused on architecture, reliability and test quality.

## Project layout

- `src/Company.Analyzers`: analyzer package
- `tests/Company.Analyzers.Tests`: analyzer tests
- `docs/rules`: rule documentation

## Rules

| Rule ID | Title                                    | Category    | Default severity |
| ------- | ---------------------------------------- | ----------- | ---------------- |
| ARCH001 | Avoid async void outside event handlers  | Reliability | Warning          |
| ARCH002 | Avoid Task.ContinueWith                  | Reliability | Warning          |
| ARCH003 | Prohibit NotBeNull() in tests            | TestQuality | Info             |
| ARCH004 | Enforce _sut naming in unit tests        | TestQuality | Info             |
| ARCH005 | Restrict usage of Arg.Any()              | TestQuality | Info             |
| ARCH006 | Warn on exclusions in BeEquivalentTo()   | TestQuality | Info             |
| ARCH007 | Detect string concatenation inside loops | Performance | Info             |
| ARCH008 | Prohibit manual path composition         | Reliability | Info             |

## Install
Add the analyzer package to the target solution as an analyzer reference or publish it internally as a NuGet package.

## Configure
```ini
[*.cs]
dotnet_diagnostic.ARCH001.severity = warning
dotnet_diagnostic.ARCH002.severity = warning
dotnet_diagnostic.ARCH003.severity = info
dotnet_diagnostic.ARCH004.severity = info
dotnet_diagnostic.ARCH005.severity = info
dotnet_diagnostic.ARCH006.severity = info
dotnet_diagnostic.ARCH007.severity = info
dotnet_diagnostic.ARCH008.severity = info
```

## Run tests
```bash
dotnet test
```
