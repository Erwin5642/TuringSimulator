# Refactor Day 1 Baseline Checklist

This checklist is the regression baseline for the event-driven refactor.
Run it before and after each phase so the demo path stays stable.

## Scope (must survive every phase)

- `BasicScene` loads and boots through `TuringBootstrap`.
- Session starts correctly (student id allocated or restored as expected).
- Level data loads into simulation, tape, and UI title/description.
- Program can be edited in the workbench.
- Run executes and playback steps are visible.
- Halt triggers validation summary in `LevelUI`.
- Tutor receives run/level telemetry and can return Hint/Ask responses.
- Next/Retry flow remains functional.

## Smoke Script

1. Open `BasicScene`.
2. Enter Play Mode.
3. Confirm no critical wiring warnings from `MvpSceneWiringValidator`.
4. Trigger Start/Run from input.
5. Edit at least one transition and rerun.
6. Step playback until halt.
7. Confirm validation summary appears with per-scenario pass/fail lines.
8. Request one Hint and one Ask response from tutor UI.
9. Trigger Next or Retry and confirm level flow still works.
10. Exit Play Mode cleanly (no stuck simulation/session state).

## Event Checkpoints to Observe

- `Menu -> Loading -> Editing` transition occurs on start.
- Program change updates both simulation and validation program references.
- Run start and run finish are both emitted/logged.
- Playback step events occur in order and include halt step.
- Validation starts only after halt, then produces pass/fail outcome.

## Day 1 Deliverables (no behavior changes)

- Add event channel base classes and typed payload structs.
- Add lightweight event trace ring buffer/logger.
- Keep current runtime flow unchanged (existing direct calls still own behavior).
- Keep this checklist updated if the demo script changes.
