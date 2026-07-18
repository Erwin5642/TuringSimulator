# Refactor Week 1 Day 5 Smoke + Checkpoint

Use this at the end of Week 1 so the game remains demoable before Day 6 work.

## Manual smoke checklist (Editor + VR)

1. Open `BasicScene`.
2. Enter Play Mode and confirm no critical wiring warnings.
3. Start from menu and confirm transition flow reaches Editing.
4. Edit one program transition and rerun.
5. Step playback through halt and confirm validation summary appears.
6. Trigger Hint and Ask once each; confirm tutor response is visible.
7. Trigger Next/Retry and confirm expected level flow.
8. Exit Play Mode, reload scene, and verify no stuck session state.

## Event trace checks

- Confirm program changes emit `ProgramChangedEventData`.
- Confirm playback steps emit `PlaybackStepEventData`.
- Confirm halt emits `HaltReachedEventData`.
- Confirm level load emits `LevelLoadedEventData`.
- Confirm run button emits `RunRequestedEventData`.

## If unstable by end of day

- Revert only Day 3/4 channel-wiring changes.
- Keep Day 2 named handler extraction and trace logging.
- Ship with direct runtime behavior intact and event channels disabled.

## Checkpoint commands

After the smoke pass succeeds:

```bash
git add <refactor files>
git commit -m "Checkpoint week1 day5: stable event wiring baseline"
git push
```
