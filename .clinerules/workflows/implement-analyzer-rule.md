# Workflow: implement analyzer rule

Use this workflow when implementing a new analyzer rule in this repository.

## Inputs

- rule ID, for example `ARCH001`
- short title
- precise rule definition
- accepted exceptions, if any
- whether `.editorconfig` configuration is required
- whether a code fix is safe

## Steps

1. Read the active rule request carefully.
2. Inspect the current repository structure and existing rule patterns.
3. Confirm the minimal file set that must change:
   - analyzer source under `src/Swa.Analyzers.Core/Rules`
   - tests under `tests/Swa.Analyzers.Tests/Rules`
   - rule documentation under `docs/rules`
   - `README.md`
   - `AnalyzerReleases.Unshipped.md`
4. Design the narrowest reliable implementation.
5. Prefer semantic analysis when necessary for correctness.
6. Implement the analyzer.
7. Add or update tests for invalid, valid, edge, and false-positive scenarios.
8. Add `.editorconfig` tests if configuration is supported.
9. Add code fix tests only if the fix is safe and deterministic.
10. Write or update `docs/rules/{RULE_ID}.md`.
11. Update `README.md` with the new rule.
12. Update `AnalyzerReleases.Unshipped.md`.
13. Build the solution.
14. Run the relevant test project.
15. Review diagnostic title, message, description, category, and severity.
16. Produce the final delivery summary with:
   - technical summary
   - changed files
   - key decisions
   - limitations
   - exact commit message

## Guardrails

- Do not implement more than one rule per execution
- Do not expand the rule beyond the requested scope unless the broader scope is required for correctness
- Do not create a code fix when the change is unsafe
- Do not leave tests or documentation for later
