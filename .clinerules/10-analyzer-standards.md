# Analyzer implementation standards

Apply these standards to every new analyzer rule.

## Design principles

- Prefer semantic analysis when correctness depends on symbol information
- Use syntax-only checks only when they are sufficient and significantly cheaper
- Favor precise diagnostics over broad heuristics
- Avoid fragile checks based only on identifier text when semantic information is available
- Keep rules reusable across repositories whenever possible

## Performance expectations

- Register only the narrowest analysis callbacks needed
- Avoid scanning the whole syntax tree when targeted node or operation analysis is enough
- Minimize allocations in hot analyzer paths
- Use `CancellationToken` correctly in analysis code
- Cache well-known symbols only when safe and useful
- Do not compute expensive state unless the current node can realistically match the rule

## Diagnostic quality

- Diagnostic messages must be short, clear, and actionable
- Titles should describe the rule intent, not the implementation detail
- Descriptions should explain why the rule exists and the risk it addresses
- Categories must be consistent across the package
- Default severities should be conservative unless the rule is highly reliable

## Configuration

- Support `.editorconfig` only when configuration adds real value and can be tested reliably
- Keep configuration names stable and documented
- Provide sensible defaults when configuration is omitted

## Code fixes

- Only implement a code fix when the change is deterministic, low risk, and broadly safe
- If the fix can break signatures, overload resolution, interfaces, inheritance, or behavior, do not generate a code fix by default
- Document why a code fix does not exist when users might expect one

## Packaging and release discipline

- Add new rule metadata to `AnalyzerReleases.Unshipped.md`
- Keep descriptors, IDs, help links, and documentation aligned
- Use resources for messages if the project grows enough to justify message centralization
