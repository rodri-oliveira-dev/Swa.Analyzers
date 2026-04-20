# Rule documentation standards

Every rule must have a dedicated markdown document under `docs/rules/{RULE_ID}.md`.

## Required sections

Each rule document must include:

1. Objective
2. Motivation
3. Non-compliant code example(s)
4. Compliant code example(s)
5. Configuration
6. Known limitations
7. When not to use
8. Expected impact
9. Notes about false positives, heuristics, or exceptions

## Documentation quality

- Keep wording precise and practical
- Explain both the technical reason and the engineering benefit
- Reflect the real analyzer behavior, not the idealized behavior
- If the rule is heuristic or intentionally partial, say so explicitly
- If there is no code fix, state that clearly when relevant

## README updates

When a new rule is added:

- update the rule table or index in `README.md`
- keep installation and usage guidance current
- keep configuration examples consistent with the actual analyzer behavior

## Consistency

- Use the same rule title in code, README, and rule document
- Keep examples aligned with the current implementation
- Avoid copy-paste placeholders or stale text from previous rules
