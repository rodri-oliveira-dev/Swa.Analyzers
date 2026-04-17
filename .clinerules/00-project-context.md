# Project context

This repository contains a reusable Roslyn analyzer package for .NET, focused on code quality, architecture conventions, testing quality, and internal engineering standards.

## Current repository structure

- `src/Swa.Analyzers.Core`: analyzer implementation project
- `src/Swa.Analyzers.SampleApp`: optional sample app used only when it adds clear validation value
- `tests/Swa.Analyzers.Tests`: analyzer test project
- `docs/rules`: rule documentation, one file per rule
- `README.md`: package overview and rule index
- `Directory.Packages.props`: central package management
- `Swa.Analyzers.slnx`: solution file

## Rule naming and identity

- Rule IDs must be stable and sequential: `ARCH001`, `ARCH002`, `ARCH003`, etc.
- Every rule must have a unique `DiagnosticDescriptor`
- Every rule must have title, message, description, category, severity, and help link or local documentation reference
- `RuleIdentifiers.cs` is the source of truth for rule ID constants

## Delivery model

- Implement one rule per iteration
- Do not bundle two rules in the same commit
- Each rule delivery must include:
  - analyzer implementation
  - automated tests
  - documentation under `docs/rules/{RULE_ID}.md`
  - README updates when needed
  - `AnalyzerReleases.Unshipped.md` update

## Reuse and maintainability

- Treat this package as an internal NuGet-ready product
- Prefer clear, neutral, reusable naming
- Avoid repository-specific hacks unless explicitly documented and configurable
- Keep public behavior predictable and documented

## Scope discipline

- Do not refactor unrelated files while implementing a rule
- Do not rename projects, folders, namespaces, or files without a strong technical reason directly tied to the current rule
- Do not change `Swa.Analyzers.SampleApp` unless it materially improves manual validation for the current rule
