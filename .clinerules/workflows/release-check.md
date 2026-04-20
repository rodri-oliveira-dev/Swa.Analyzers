# Workflow: release check

Use this workflow before packaging or promoting the analyzer package.

## Checklist

1. Build the solution in Release mode.
2. Run the analyzer test suite.
3. Confirm every shipped rule has:
   - stable ID
   - documentation
   - README entry
   - release note entry
4. Confirm `AnalyzerReleases.Unshipped.md` only contains unreleased entries.
5. Move released entries to `AnalyzerReleases.Shipped.md` when appropriate.
6. Verify package metadata, version, and dependencies.
7. Confirm no experimental rule is enabled by default unless explicitly approved.
8. Review recent changes for accidental breaking behavior.
