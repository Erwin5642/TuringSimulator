# AI Coding Guide (Repo-Specific)

This guide is for agents making changes in this repository.

## Start-Here Checklist

1. Read `docs/client/README.md` and `docs/server/README.md`.
2. Identify whether change is client-only, server-only, or contract-crossing.
3. If contract-crossing, update both sides in one change set.
4. Preserve current runtime behavior unless user explicitly requests architecture migration.

## Contract-Sensitive Areas

- `student_id`, `level_id` payload semantics on `/ask`
- REST JSON naming (`snake_case`) and Newtonsoft settings on Unity side

Primary files:

- Client:
  - `Assets/TuringSimulator/ITS/ITSClient.cs`
  - `Assets/TuringSimulator/ITS/ITSModel.cs`
  - `Assets/TuringSimulator/ITS/Protocol/*`
  - `Assets/TuringSimulator/ITS/LiveTutorSocket.cs`
- Server:
  - `TuringBotAPI/main.py`
  - `TuringBotAPI/agent.py`
  - `TuringBotAPI/rag/store.py`
  - `TuringBotAPI/knowledge/`

## Common Pitfalls

- Updating server endpoints but not Unity callers.
- Changing level IDs in Unity assets without updating `TuringBotAPI/knowledge/goals/` docs.
- Breaking `SkillTracker` session/level fields used on `/ask` payloads.
- Assuming menu/session lifecycle exists in current runtime flow.
- Adding hidden runtime discovery when an Inspector-visible scene binding would
  make the system easier to understand and debug.

## Verification Expectations

- For ITS changes:
  - validate `/health`
  - smoke-check `/ask` and `/session/new`
  - run `pytest` in `TuringBotAPI/tests/`
  - ensure no serialization regressions on Unity side
- For scene/content changes:
  - validate `BasicScene` with `MvpSceneWiringValidator`
  - inspect the hierarchy and serialized references in the Unity Editor
  - run one manual edit/run/validate pass before calling the MVP complete

## Documentation Rule

When behavior changes, update the relevant docs in `docs/client` and/or `docs/server` in the same PR.
