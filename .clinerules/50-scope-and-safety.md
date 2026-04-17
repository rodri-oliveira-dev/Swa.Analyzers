# Scope and safety guardrails

## Scope discipline

- Implement only the requested rule in the current iteration
- Avoid opportunistic refactors outside the active rule scope
- Do not modify generated files, tooling configuration, or sample code unless needed for the current rule

## Reliability first

- If a rule cannot be implemented with acceptable confidence, document the limitation before expanding scope
- Prefer a narrower, reliable rule over a broad and noisy rule
- If false positives are likely, document them clearly and keep severity conservative

## Repository hygiene

- Do not change package names, root namespaces, or folder layout without explicit need
- Do not add dependencies without strong justification
- Reuse existing test infrastructure where possible

## Communication discipline

For each completed rule, provide:

- technical summary of what was implemented
- files created or changed
- important design decisions
- risks or limitations
- exact commit message
