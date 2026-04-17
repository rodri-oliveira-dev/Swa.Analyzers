# Active context

Current focus:

- build a stable analyzer package foundation
- implement rules incrementally
- keep one rule per change set

Active rule:

- ARCH003 - Prohibit NotBeNull() in tests (implemented)

Open design questions:

- None for this iteration.

Known risks:

- Test detection is heuristic (based on known test-framework attributes). If a repo uses a different test framework or custom attributes, the analyzer may stay silent.

Pending follow-up items:

- Consider expanding supported test frameworks/attributes if needed by consumers.

When working on a rule, update this file with:

- active rule ID
- open design questions
- known risks
- pending follow-up items
