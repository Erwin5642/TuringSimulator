# Unity Editor MVP Checklist (Current)

Use this as a quick checklist. For full detail, use:

- `docs/client/SCENE_OBJECT_WIRING_MAP.md`
- `docs/client/EVENT_DRIVEN_DEMO_EVENT_MAP.md`

## Scene roots (minimum)

```text
Systems
Tutor
Agent
Gameplay
View
UI
Voice
```

## Required object/components

- `Systems/BootstrapRoot`: `TuringBootstrap`
- `Systems`: `MvpSceneWiringValidator`
- `Systems`: `EventChannelWiringValidator`
- `Tutor/ITSClient`: `ITSClient`
- `Tutor/SkillTracker`: `SkillTracker`
- `Tutor/AgentDialogue`: `AgentDialogue`
- `Tutor/AgentTTS`: `AgentTTS`
- `Tutor/VoiceInputHandler`: `VoiceInputHandler`
- `Tutor/VoiceAskControllerInput`: `VoiceAskControllerInput`
- `Tutor/AgentActionMapper`: `AgentActionMapper`
- `Tutor/AgentActionExecutor`: `AgentActionExecutor`
- `Tutor/AgentVoiceFeedbackListener`: `AgentVoiceFeedbackListener`
- `Agent/AgentAvatar`: `Animator` + `AgentAnimator`
- `Gameplay/ProgramWorkbench`: `ProgramWorkbench`
- `Gameplay/PlayerInput`: `PlayerInputCatcher`
- `Gameplay/CardDrawer`: `CardDrawerBehaviour`
- `View/MachineView`: object implementing `IMachineView` (typically `MachineViewer`)
- `View/TapeView`: object implementing `ITapeVisual` (typically `ConveyorTapeVisual`)
- `View/HaltIndicator`: object implementing `IHaltStatusIndicator` (typically `HaltStatusColorIndicator`)
- `UI/LevelUI`: `LevelUI`
- `Voice/AppVoiceExperience`: `AppVoiceExperience`

## Wiring pass

1. In `TuringBootstrap`, assign:
   - `LevelDatabase`
   - full `ViewSceneBindings`
   - full `ControllerSceneBindings`
   - ITS references (`ITSClient`, `SkillTracker`, `AgentTTS`, `AgentDialogue`)
2. In `ControllerSceneBindings`, assign all gameplay channels.
3. In `VoiceInputHandler`, `ITSClient`, and agent components, assign all required event channels.
4. In `AgentDialogue`, set `_useLegacyDirectWiring` to `false`.
5. In `EventChannelWiringValidator`, assign all 18 channels.

## Play-mode smoke pass

1. Start API:
   - `uvicorn main:app --reload --port 8000`
2. Open `BasicScene`.
3. Run `Validate Scene` on `MvpSceneWiringValidator`.
4. Run `Validate Event Channels` on `EventChannelWiringValidator`.
5. Start/run a level and confirm gameplay progression:
   - `Menu -> Loading -> Editing -> Running -> Halted -> Validating -> Victory/Defeat`
6. Toggle mic via controller input and ask a question.
7. Confirm agent responds with both:
   - spoken/subtitled text
   - mapped animation (via `AgentActionRequested` rules)
