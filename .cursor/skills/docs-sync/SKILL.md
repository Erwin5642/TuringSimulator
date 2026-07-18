# Docs Sync Skill

Use this skill when a change affects architecture, data contracts, runtime lifecycle, or gameplay/server behavior.

## Goal

Keep repository documentation aligned with the actual code state so future agents can implement safely.

## Required Updates

- Client-side behavior changes:
  - update `docs/client/README.md`
- Server-side behavior changes:
  - update `docs/server/README.md`
- Cross-cutting or workflow changes:
  - update `docs/README.md` and `docs/AI_GUIDE.md` as needed

## Procedure

1. Identify changed behavior and impacted boundaries.
2. Locate the canonical code files for the behavior.
3. Update docs with:
   - current behavior
   - key entry points
   - invariants
   - known limitations (if relevant)
4. Avoid aspirational wording unless clearly marked as planned.
5. Keep docs concise and implementation-anchored.

## Quality Bar

- Paths and symbol names are accurate.
- Client/server contract details are synchronized.
- No stale architecture statements remain after the change.
