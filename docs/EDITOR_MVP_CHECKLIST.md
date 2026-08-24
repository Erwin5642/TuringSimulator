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
TTS
```

## Required object/components

- `Systems/BootstrapRoot`: `TuringBootstrap`
- `Systems`: `MvpSceneWiringValidator`
- `Systems`: `EventChannelWiringValidator`
- `Tutor/SkillTracker`: `SkillTracker`
- `Tutor/AgentDialogue`: `AgentDialogue`
- `Tutor/AgentTTS`: `AgentTTS` (assign `TTS Speaker`) + `VoiceDebugHotkeys`
- `TTS/TTSWitService`: `TTSWit` (assign `Assets/WitAI/tts_witconfig.asset`, app `turing_tts` / English)
- `TTS/TTSSpeaker`: `TTSSpeaker` (voice preset, e.g. `WIT$REBECCA`)
- `Tutor/Voice`: `HandGestureMicListener` (`_gestureId = Shaka`) + `VoiceInputHandler` + `AppVoiceExperience` (`stt_witconfig` / `turing_stt` / Portuguese) + `ITSClient` + `TranscriptionAskFallbackListener` + `AgentVoiceFeedbackListener` + `VoiceAskControllerInput` + `VoiceHearingStoppedCue`
- `Tutor/AgentActionMapper`: `AgentActionMapper` (Victory/Defeat/ThumbsUp plus AskRequested Thinking and AskResult Talking/Reply)
- `Tutor/AgentActionExecutor`: `AgentActionExecutor`
- `Left Hand`: ThumbsUp detector + publisher
- `Right Hand`: ThumbsUp detector **and** Shaka pose detector + publishers
- `Agent/AgentAvatar`: `Animator` + `AgentAnimator`
- `Gameplay/ProgramWorkbench`: `ProgramWorkbench`
- `Gameplay/PlayerInput`: `PlayerInputCatcher`
- `Gameplay/CardDrawer`: `CardDrawerBehaviour`
- `View/MachineView`: object implementing `IMachineView` (typically `MachineViewer`)
- `View/TapeView`: object implementing `ITapeVisual` (typically `ConveyorTapeVisual`) + `TapeDebugHotkeys`
- `View/HaltIndicator`: object implementing `IHaltStatusIndicator` (typically `HaltStatusColorIndicator`)
- `UI/LevelUI`: `LevelUI`

## Wiring pass

1. In `TuringBootstrap`, assign:
   - `LevelDatabase`
   - full `ViewSceneBindings`
   - full `ControllerSceneBindings`
   - ITS references (`ITSClient`, `SkillTracker`, `AgentTTS`, `AgentDialogue`)
2. In `ControllerSceneBindings`, assign all gameplay channels.
3. In `VoiceInputHandler`, `ITSClient`, `HandGestureMicListener`, and agent components, assign all required event channels.
4. In `AgentDialogue`, set `_useLegacyDirectWiring` to `false`.
5. In `EventChannelWiringValidator`, assign all 18 channels.
6. Confirm `TTSWit` uses `tts_witconfig` and `AppVoiceExperience` uses `stt_witconfig` (never swapped).
7. Confirm `EventTracePanel` `_maxPayloadLength` is 240.

## Play-mode smoke pass

1. Start API:
   - `uvicorn main:app --reload --port 8000`
2. Open `BasicScene`.
3. Run `Validate Scene` on `MvpSceneWiringValidator`.
4. Run `Validate Event Channels` on `EventChannelWiringValidator`.
5. Start/run a level and confirm gameplay progression:
   - `Menu -> Loading -> Editing -> Running -> Halted -> Validating -> Victory/Defeat`
6. Hold Shaka with the **right hand** (hand tracking), speak Portuguese, release; or toggle mic via controller / Editor **T**.
7. Confirm agent responds with both:
   - spoken/subtitled text (Wit TTS via `tts_witconfig`)
   - mapped animation (via `AgentActionRequested` rules)
   - event-trace rows showing STT `partial=` / `text=` and tutor `reply=` / `AgentSpeechStarted` (not truncated to a few words)
8. Editor voice smoke: Game view focused, **L** speaks the sample TTS line without `Unsupported language for synthesize: 'pt'`; **T** shows the STT overlay.
