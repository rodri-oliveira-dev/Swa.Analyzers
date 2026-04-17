# Analyzer testing standards

Every rule must be accompanied by focused automated tests.

## Mandatory coverage

For each analyzer rule, add tests for:

- invalid scenario with expected diagnostic
- valid scenario with no diagnostic
- relevant edge cases
- obvious false positives that must not report diagnostics
- `.editorconfig` behavior when the rule supports configuration
- code fix behavior only when a safe code fix exists

## Test quality expectations

- Test names must clearly describe the scenario and expected behavior
- Keep test inputs minimal while still realistic
- Prefer one clear reason per test
- Verify diagnostic ID, location, and message when appropriate
- Avoid brittle tests that depend on unrelated formatting or incidental implementation details

## Rule-specific nuance

- If the rule includes exceptions, add explicit tests for each allowed exception path
- If the rule uses semantic analysis, test aliasing, fully-qualified names, and common variations when relevant
- If the rule interacts with framework APIs, cover the most common supported shapes used in real code

## Regression mindset

- Add a regression test for every bug fixed in a rule
- When a heuristic is intentionally conservative, add tests proving the analyzer stays quiet in ambiguous cases

## Execution expectation

Before concluding a rule:

- ensure the solution builds
- run the related analyzer tests
- verify tests fail without the rule and pass with the implementation
