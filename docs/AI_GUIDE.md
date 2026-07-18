# AI Coding Guide (Repo-Specific)

This guide is for agents making changes in this repository.

## Start-Here Checklist

1. Read `docs/client/README.md` and `docs/server/README.md`.
2. Identify whether change is client-only, server-only, or contract-crossing.
3. If contract-crossing, update both sides in one change set.
4. Preserve current runtime behavior unless user explicitly requests architecture migration.

## Contract-Sensitive Areas

- `student_id`, `level_id`, `skill_ids` payload semantics
- REST JSON naming (`snake_case`) and Newtonsoft settings on Unity side
- WebSocket live protocol version/kinds and payload DTOs

Primary files:

- Client:
  - `Assets/TuringSimulator/ITS/ITSClient.cs`
  - `Assets/TuringSimulator/ITS/ITSModel.cs`
  - `Assets/TuringSimulator/ITS/Protocol/*`
  - `Assets/TuringSimulator/ITS/LiveTutorSocket.cs`
- Server:
  - `TuringBotAPI/main.py`
  - `TuringBotAPI/protocol/live_v1.py`
  - `TuringBotAPI/student_model.py`
  - `TuringBotAPI/orchestrator.py`

## Common Pitfalls

- Updating server endpoints but not Unity callers.
- Changing level IDs in Unity assets without updating server level metadata/hints/concepts.
- Breaking `SkillTracker` mappings without adjusting server concept map assumptions.
- Assuming menu/session lifecycle exists in current runtime flow.
- Adding hidden runtime discovery when an Inspector-visible scene binding would
  make the system easier to understand and debug.

## Verification Expectations

- For ITS changes:
  - validate `/health`
  - smoke-check `/event`, `/ask`, `/hint`
  - ensure no serialization regressions on Unity side
- For protocol changes:
  - verify both client and server protocol constants and payload DTOs
  - run/update protocol tests in `TuringBotAPI/tests/`
- For scene/content changes:
  - validate `BasicScene` with `MvpSceneWiringValidator`
  - inspect the hierarchy and serialized references in the Unity Editor
  - run one manual edit/run/validate pass before calling the MVP complete

## Documentation Rule

When behavior changes, update the relevant docs in `docs/client` and/or `docs/server` in the same PR.
