# Swa.Analyzers agent guidance

This repository contains reusable Roslyn analyzers for .NET.

## Core goals

- implement one rule at a time
- keep analyzers reusable and maintainable
- always include tests and documentation with each rule
- keep changes minimal and scoped

## Project layout

- `src/Swa.Analyzers.Core`: analyzer code
- `tests/Swa.Analyzers.Tests`: automated tests
- `docs/rules`: per-rule documentation
- `src/Swa.Analyzers.SampleApp`: optional sample app, only change when it adds direct value

## Delivery expectations per rule

- analyzer implementation
- tests for invalid, valid, edge, and false-positive scenarios
- documentation under `docs/rules/{RULE_ID}.md`
- README update when needed
- `AnalyzerReleases.Unshipped.md` update
- one commit per rule

## Commit format

`feat(analyzers): add ARCH001 short-rule-description`

## Implementation principles

- prefer semantic analysis when needed
- avoid broad or noisy heuristics
- use conservative defaults
- add configuration only when it provides real value
- add code fixes only when safe and deterministic
