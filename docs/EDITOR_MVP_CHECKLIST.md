# Unity Editor MVP Checklist

This checklist keeps the MVP visible in the Unity hierarchy and Inspector.
Runtime fallback is useful for recovery, but the intended demo setup is
serialized scene wiring that can be inspected by a teammate.

## BasicScene hierarchy

Create or confirm these named roots:

```text
Systems
  TuringBootstrap
  MvpSceneWiringValidator
Tutor
  ITSClient
  SkillTracker
  LiveTutorSocket
  AgentDialogue
  AgentTTS (optional for text-only MVP)
UI
  LevelUI
  TutorCanvas
ProgramWorkbench
  Program blocks
  Wires and sockets
CardDrawer
```

## Inspector wiring

1. Select `SimulationRoot` or the chosen `Systems` root.
2. On `TuringBootstrap`, assign:
   - `LevelDatabase`;
   - `ViewSceneBindings`;
   - `ControllerSceneBindings`;
   - the ITS components under `Tutor`.
3. Assign the `ProgramWorkbench` in `ControllerSceneBindings`.
4. On `ProgramWorkbench`, assign the entry block, all program blocks, and all
   scene card behaviours.
5. On `CardDrawer`, assign:
   - a symbol-card prefab with `SymbolCardBehaviour`;
   - a direction-card prefab with `DirectionCardBehaviour`;
   - `XRGrabInteractable`, Rigidbody, and colliders on both spawned prefabs.
6. On `AgentDialogue`, assign the subtitle bubble/text, ask panel, input/send
   controls, hint button, loading indicator, and optional microphone controls.
7. On `LiveTutorSocket`, set the local WebSocket URL and leave
   `connectOnStart` enabled for desktop/editor testing.
8. Add a TMP text object to `LevelUI` for validation summaries if per-scenario
   results should be visible in the scene.
9. Add `MvpSceneWiringValidator` to the `Systems` root, drag the same
   references into its sections, then use its context-menu action:
   `Validate Scene`.
10. Add `SceneReloadButton` to a world-space XR UI `Button`. It registers its
    click handler automatically; make sure `BasicScene` is in Build Settings.
11. Wire world-space XR UI buttons to `TuringBootstrap` methods for flow control:
    - `StartOrRunFromInteraction`
    - `PausePlaybackFromInteraction`
    - `PlayPlaybackFromInteraction`
    - `StepForwardFromInteraction`
    - `StepBackwardFromInteraction`
    - `NextLevelFromInteraction`
    - `ReturnToMainMenu`

## Play-mode smoke pass

- Start the API from `TuringBotAPI`:
  `uvicorn main:app --reload --port 8000`
- Open `BasicScene`.
- Confirm the Console reports a new session and no missing MVP wiring.
- Grab a drawer slot and verify a configured card appears.
- Place the card into a block slot and connect a wire.
- Press Start/Run and watch `Menu -> Loading -> Editing -> Running`.
- Confirm validation displays named scenario results.
- Click Hint and Ask; confirm the subtitle bubble receives a reply.
- Click the reload button and confirm the scene resets without creating a new
  `student_id`.
- Return to menu, start again, and confirm a new `student_id` is logged.
- Confirm the second player starts with an empty BKT state.

## Hardware-only checks

These cannot be reliably validated from repository code:

- hand/controller tracking and grab pose;
- physical card placement ergonomics;
- TTS output on the target headset;
- microphone permissions and voice transcription;
- world-space UI readability and comfort.
