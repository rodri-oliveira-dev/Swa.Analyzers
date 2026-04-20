# Commit and release standards

## Commit granularity

- One rule per commit
- Do not mix unrelated cleanup with the rule implementation
- If minimal shared infrastructure is required, keep it inside the first rule commit that needs it

## Commit format

Use this format:

`feat(analyzers): add ARCH001 short-rule-description`

Examples:

- `feat(analyzers): add ARCH001 avoid async void outside event handlers`
- `feat(analyzers): add ARCH002 avoid task continuewith`

## What each rule commit must include

- analyzer implementation
- automated tests for the rule
- documentation under `docs/rules`
- README update if the rule table or usage needs adjustment
- `AnalyzerReleases.Unshipped.md` update

## Release hygiene

- Keep rule IDs stable after publication
- Do not silently repurpose an existing rule ID for a different behavior
- If rule behavior changes materially, update tests and documentation in the same change
