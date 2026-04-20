# System patterns

## Repository pattern

- analyzer code under `src/Swa.Analyzers.Core`
- tests under `tests/Swa.Analyzers.Tests`
- rule docs under `docs/rules`

## Rule pattern

Each rule should provide:

- stable rule ID
- `DiagnosticDescriptor`
- analyzer implementation
- tests
- rule markdown documentation
- release metadata entry

## Engineering pattern

- one rule per iteration
- one commit per rule
- prefer narrow, reliable checks over broad heuristics
- keep diagnostics actionable and predictable
