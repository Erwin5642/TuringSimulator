# Scene Object Wiring Map (Full Demo)

This document lists the scene objects that should be present in `BasicScene` and explains how each one is wired to the rest of the game.

Scope: current slim voice Ask/Answer client with event-driven gameplay and agent actions.

## 1) Scene Objects To Place

Recommended hierarchy (names are suggestions, component set is the source of truth):

```text
Systems
  BootstrapRoot
  MvpSceneWiringValidator
  EventChannelWiringValidator
  EventTraceLogInstaller (optional)
Tutor
  ITSClient
  SkillTracker
  AgentDialogue
  AgentTTS
  VoiceInputHandler
  VoiceAskControllerInput
  AgentActionMapper
  AgentActionExecutor
  AgentVoiceFeedbackListener
Agent
  AgentAvatar (Animator + AgentAnimator)
Gameplay
  ProgramWorkbench
  PlayerInput
  CardDrawer
  Program blocks / sockets / wires
View
  MachineView
  TapeView
  HaltIndicator
UI
  LevelUI
  ReloadButton (optional)
Voice
  AppVoiceExperience
```

## 2) Core Composition Objects

### `BootstrapRoot` (`TuringBootstrap`)

Purpose: composition root; creates model/view/controller runtime and starts flow.

Assign in Inspector:

- `levelDatabase`
- `viewSceneBindings.machine` -> object implementing `IMachineView` (normally `MachineViewer`)
- `viewSceneBindings.tape` -> object implementing `ITapeVisual` (normally `ConveyorTapeVisual`)
- `viewSceneBindings.halt` -> object implementing `IHaltStatusIndicator` (normally `HaltStatusColorIndicator`)
- `viewSceneBindings.levelUI` -> `LevelUI`
- `controllerSceneBindings.input` -> `PlayerInputCatcher`
- `controllerSceneBindings.programWorkbench` -> `ProgramWorkbench`
- `controllerSceneBindings` gameplay event channels:
  - `runRequestedChannel`
  - `runStartedChannel`
  - `runFinishedChannel`
  - `levelLoadedChannel`
  - `programChangedChannel`
  - `playbackStepChannel`
  - `simulationStepProducedChannel`
  - `haltReachedChannel`
  - `validationCompletedChannel`
  - `levelOutcomeChannel`
- ITS references:
  - `itsClient`
  - `skillTracker`
  - `agentTTS`
  - `agentDialogue`

Runtime links created by bootstrap:

- Installs `ModelInstaller`, `ViewInstaller`, `ControllerInstaller`
- Starts `GameFlowController` (`Start()`)
- Requests/rehydrates `student_id` through `ITSClient` + `SkillTracker`

### `MvpSceneWiringValidator` (`MvpSceneWiringValidator`)

Purpose: high-level scene checklist validation.

Assign all major scene objects:

- `bootstrap`
- `levelDatabase`
- `programWorkbench`
- `cardDrawer`
- `itsClient`
- `skillTracker`
- `agentDialogue`
- `agentActionMapper`
- `agentActionExecutor`
- `agentVoiceFeedbackListener`
- `agentAnimator`
- `voiceInputHandler`
- `voiceAskControllerInput`
- `eventChannelWiringValidator`

Use context menu: `Validate Scene`.

### `EventChannelWiringValidator` (`EventChannelWiringValidator`)

Purpose: validates that required event channels are assigned.

Assign all 18 channels:

- `RunRequested`, `RunStarted`, `RunFinished`
- `LevelLoaded`, `ProgramChanged`, `PlaybackStep`, `SimulationStepProduced`, `HaltReached`
- `ValidationCompleted`, `LevelOutcome`
- `MicToggleRequested`, `ListeningStateChanged`, `PartialTranscription`, `TranscriptionReady`
- `AskRequested`, `AskResult`, `ThinkingStateChanged`
- `AgentActionRequested`

Use context menu: `Validate Event Channels`.

## 3) Gameplay Editing Objects

### `ProgramWorkbench` (`ProgramWorkbench`)

Purpose: compiles scene block topology into the active TM program.

Assign in Inspector:

- `entryBlockId`
- `blocks` (`ProgramBlockBehaviour[]`)
- `symbolCards` (`SymbolCardBehaviour[]`)
- `directionCards` (`DirectionCardBehaviour[]`)

Wiring:

- Receives `IProgramEditController` from `ControllerInstaller.Initialize(...)`
- Calls `_edit.ReplaceProgramBuilder(...)` after `GraphToProgramCompiler`
- Drives `ProgramChanged` flow through `ControllerInstaller`

### `PlayerInput` (`PlayerInputCatcher`)

Purpose: keyboard/gameplay control source.

Emits requests consumed by `ControllerInstaller`:

- `OnStartRequest`
- `OnPlayRequest`
- `OnPauseRequest`
- `OnForwardRequest`
- `OnBackwardRequest`
- `OnNextRequest`
- `OnMenuRequest`

### `CardDrawer` (`CardDrawerBehaviour`)

Purpose: source of spawnable cards for editing.

Assign in Inspector:

- `symbolCardPrefab` (must include `SymbolCardBehaviour` + XR grab setup)
- `directionCardPrefab` (must include `DirectionCardBehaviour` + XR grab setup)

Wiring:

- Child slots spawn cards from these prefabs
- Spawned cards should be registered in `ProgramWorkbench` for edit/run lock behavior

## 4) Simulation View Objects

### `MachineView` (`MachineViewer`)

Purpose: applies simulation steps to tape + halt visuals.

Wiring:

- Assigned as `viewSceneBindings.machine`
- Initialized by `ViewInstaller.Initialize(Tape, Halt)`

### `TapeView` (`ConveyorTapeVisual`)

Purpose: visual tape state and head movement.

Assign in Inspector:

- `cellsRoot` containing `TapeCellView` children

Wiring:

- Assigned as `viewSceneBindings.tape`
- Receives state updates from `MachineViewer`

### `HaltIndicator` (`HaltStatusColorIndicator`)

Purpose: visual halt status indicator.

Assign in Inspector:

- `targetRenderer`

Wiring:

- Assigned as `viewSceneBindings.halt`
- Updated by `MachineViewer` on halt

### `LevelUI` (`LevelUI`)

Purpose: level metadata + validation summary display.

Assign in Inspector:

- `levelTitle` (`TextMeshPro`)
- `levelDescription` (`TextMeshPro`)
- `validationSummary` (`TextMeshPro`) optional but recommended

Wiring:

- Assigned as `viewSceneBindings.levelUI`
- Updated by level-loaded and validation flow

## 5) Tutor and Voice Objects

### `ITSClient` (`ITSClient`)

Purpose: REST client for `/session/new`, `/ask`, `/health`.

Assign in Inspector:

- `_baseUrl`
- `_transcriptionReadyChannel`
- `_askRequestedChannel`
- `_askResultChannel`
- `_thinkingStateChannel`

Wiring:

- Subscribes to `TranscriptionReady`
- Publishes `AskRequested`, `AskResult`, `ThinkingStateChanged`
- Provides session allocation to bootstrap/controller flow

### `SkillTracker` (`SkillTracker`)

Purpose: stores active `student_id` and current `level_id`.

Wiring:

- Session started/cleared by bootstrap/controller
- Level context updated by level-loaded handler
- Read by `ITSClient` when building ask payloads

### `VoiceInputHandler` (`VoiceInputHandler`)

Purpose: STT orchestration through Meta Voice / Wit.

Assign in Inspector:

- `AppVoiceExperience` reference (`_voiceExperience`) or leave null for runtime auto-find
- `_micToggleRequestedChannel`
- `_listeningStateChannel`
- `_partialTranscriptionChannel`
- `_transcriptionReadyChannel`

Wiring:

- Subscribes to `MicToggleRequested`
- Publishes listening/partial/final transcription channels

### `VoiceAskControllerInput` (`VoiceAskControllerInput`)

Purpose: VR/controller mic toggle source.

Assign in Inspector:

- `_micToggleAction` (optional; defaults to right secondary button if missing)
- `_micToggleRequestedChannel`

Wiring:

- Publishes `MicToggleRequested`

### `AppVoiceExperience`

Purpose: Meta Voice SDK runtime object required by `VoiceInputHandler`.

Assign in Inspector:

- Wit runtime configuration/token asset as required by Meta Voice setup

Wiring:

- Referenced by `VoiceInputHandler`

## 6) Agent Action Objects

### `AgentDialogue` (`AgentDialogue`)

Purpose: subtitle bubble and listening/thinking visual feedback.

Assign in Inspector:

- `_bubbleRoot` (optional; auto-generated if absent)
- `_bubbleText` (optional; auto-generated if absent)
- `_micActiveIndicator`
- `_partialLabel`
- `_loadingIndicator`
- `_useLegacyDirectWiring` should be `false` for pure event-driven mode

Wiring:

- Updated by `AgentVoiceFeedbackListener` (event-driven UI state)
- Used by `AgentActionExecutor` to display subtitles

### `AgentTTS` (`AgentTTS`)

Purpose: speaks agent text through Android TTS.

Wiring:

- Used by `AgentActionExecutor`
- `AgentAnimator` listens to `OnSpeechFinished`

### `AgentActionMapper` (`AgentActionMapper`)

Purpose: maps arbitrary source event channel payloads to `(text, animation)` actions via rules.

Assign in Inspector:

- `_agentActionChannel`
- `_rules[]` entries:
  - `SourceChannel`
  - optional `MatchProperty` + `MatchValue`
  - `TextMode` + text source
  - `Animation`

Wiring:

- Subscribes to each rule's source channel
- Publishes `AgentActionRequested`

### `AgentActionExecutor` (`AgentActionExecutor`)

Purpose: executes action text.

Assign in Inspector:

- `_agentActionChannel`
- `_agentDialogue`
- `_agentTts`

Wiring:

- Subscribes to `AgentActionRequested`
- Shows subtitle and calls TTS

### `AgentAvatar` (`Animator` + `AgentAnimator`)

Purpose: executes action animation.

Assign in Inspector:

- Animator component with expected params
- `_agentActionChannel`
- bool names: `_idleBool`, `_thinkingBool`, `_talkingBool`
- trigger name: `_celebrateTrigger` (example `Commemoration`)

Wiring:

- Subscribes to `AgentActionRequested`
- Applies animation state machine parameters

### `AgentVoiceFeedbackListener` (`AgentVoiceFeedbackListener`)

Purpose: bridges voice/thinking channels into `AgentDialogue` UI.

Assign in Inspector:

- `_listeningStateChannel`
- `_partialTranscriptionChannel`
- `_thinkingStateChannel`
- `_agentDialogue`

Wiring:

- Subscribes to those channels
- Calls `AgentDialogue.SetListeningState`, `SetPartialTranscription`, `SetThinkingState`

## 7) Optional Utility Objects

### `EventTraceLogInstaller` (`EventTraceLogInstaller`)

Purpose: configures in-memory event trace ring buffer.

Assign in Inspector:

- `_enabled`
- `_capacity`
- `_clearOnAwake`

### `ReloadButton` (`Button` + `SceneReloadButton`)

Purpose: reloads active scene while preserving session reload behavior.

Wiring:

- `SceneReloadButton` auto-registers `Button.onClick` in `OnEnable`
- Calls `TuringBootstrap.PrepareForSceneReload()` then reloads scene

## 8) Final Wiring Check

Before demo:

1. Run `MvpSceneWiringValidator -> Validate Scene`.
2. Run `EventChannelWiringValidator -> Validate Event Channels`.
3. In `AgentDialogue`, confirm `_useLegacyDirectWiring` is `false`.
4. Confirm voice ask path:
   - controller toggle -> listening indicator
   - speech -> transcription -> ask -> reply
   - `AgentActionRequested` drives both subtitle/TTS and animation.
