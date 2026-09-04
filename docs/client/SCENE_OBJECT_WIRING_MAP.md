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
  SkillTracker
  AgentDialogue
  AgentTTS
  AgentActionMapper
  AgentActionExecutor
  Voice
    HandGestureMicListener
    VoiceInputHandler
    AppVoiceExperience
    ITSClient
    TranscriptionAskFallbackListener
    AgentVoiceFeedbackListener
    VoiceAskControllerInput
    VoiceHearingStoppedCue
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
  VictoryConfetti
  HaltIndicator
UI
  LevelUI
  ReloadButton (optional)
TTS
  TTSWitService
  TTSSpeaker
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
- Requests/rehydrates `student_id` through the Inspector-bound `ITSClient` + `SkillTracker` (not `SkillTracker.Instance`, which may still be unset during `Awake`)

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

Optional:

- `HandGesturePerformed`, `VoiceCaptureStopped`, `TapeMoved`, `TapeRead`, `TapeWrite`

Use context menu: `Validate Event Channels`.

## 3) Gameplay Editing Objects

### `ProgramWorkbench` (`ProgramWorkbench`)

Purpose: compiles scene block topology into the active TM program.

Assign in Inspector:

- `startOutputPort` (workbench "electricity start" output socket)
- `entryBlockId` (legacy only when `startOutputPort` is **unassigned**; ignored when the port exists but is unwired → halt)
- `blocks` (`ProgramBlockBehaviour[]`)
- `symbolCards` (`SymbolCardBehaviour[]`)
- `directionCards` (`DirectionCardBehaviour[]`)

Wiring:

- Receives `IProgramEditController` from `ControllerInstaller.Initialize(...)`
- Unwired start port → `_edit.Clear()` (halt program)
- Wired start → entry from `startOutputPort.ConnectedPeer.Owner.BlockId`; compile reachable directed subgraph
- Skips recompile when `ProgramGraphFingerprint` is unchanged; off-start wires gated by `IProgramBlockConnectivity`
- Calls `_edit.ReplaceProgramBuilder(...)` after `GraphToProgramCompiler` (keeps previous program on compile failure)
- Stores the compiled state→block-id map so a run can light the live wire
- During playback, `IProgramExecutionHighlight` switches the live energy wire to that socket's `previewColor`; idle wires stay on `connectedColor`
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
- `OnAbortRequest`
- `OnStartOrAbortRequest` (XR `PlayAbortButton`: start while editing, abort/reset while running)
- `OnPlayOrPauseRequest` (XR `PauseResumeButton`)

World-space button labels live on the TMP child of each control (`MachineControlButtonLabel`):

- `PlayAbortButton`: **Começar** in edit/idle, **Recomeçar** while `Running` (abort + full reset back to edit)
- `PauseResumeButton`: **Pausar** while playback is requested, **Rodar** while paused/idle

Assign `_kind` and `_label` on those TMP objects. Runtime text follows game/playback state; it does not stay frozen at the scene default.

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

Purpose: visual tape state and head movement. The conveyor mesh stays still; `cellsRoot` slides left/right and **keeps that offset** (it is not snapped back to the tape origin after a move). Cells keep their local positions. The prefab pool (~10 `TapeCellView` children) grows by one cloned cell only when the head leaves that range.

Assign in Inspector:

- `cellsRoot` containing the initial `TapeCellView` pool
- each `TapeCellView.symbolPrefabs`
- `stepFeedback` → `TapeStepFeedback` on the same Tape (optional until you want read/write cues)
- `tapeMovedChannel` → `Assets/TuringSimulator/Events/TapeMovedChannel.asset`
- `tapeReadChannel` → `Assets/TuringSimulator/Events/TapeReadChannel.asset`
- `tapeWriteChannel` → `Assets/TuringSimulator/Events/TapeWriteChannel.asset`

Child `MoveAudio` (`EventChannelActionListener` + dedicated `AudioSource`) Inspector slots:

Assigning `SourceChannel` shows a read-only payload member list (copy names into `MatchProperty` / `MatchValue`).

- Binding **Start move**: `TapeMovedChannel`, `MatchProperty` `Phase`, `MatchValue` `Started`, `OnMatched` → that `AudioSource.Play()`
- Binding **Move click**: same channel and filter → `AudioSource.PlayOneShot(Click01.wav)` (short mechanical click; the loop clip stays `Factories1`)
- Binding **Stop move**: same channel, `MatchValue` `Finished`, `OnMatched` → `AudioSource.Stop()`
- On the `AudioSource`: assign the move clip, tick **Loop**, leave **Play On Awake** off. Do not `Stop()` the Tape root source used by `TapeStepFeedback`.

Read/write `EventChannelActionListener` examples (same Tape or a child FX object):

- `TapeReadChannel`, `MatchProperty` `IsMatch`, `MatchValue` `True` → match clip `Play()`
- `TapeReadChannel`, `IsMatch` = `False` → mismatch clip `Play()`
- `TapeWriteChannel`, `MatchProperty` `Effect`, `MatchValue` `Write` → write particles `Play()`
- `TapeWriteChannel`, `Effect` = `Delete` → delete particles `Play()`

Do not bind `Play()` for a clip or particle system that `TapeStepFeedback` already plays, or the cue fires twice. Use the channel for *new* FX, or clear the matching `TapeStepFeedback` slot.

`TapeStepFeedback` Inspector slots:

- `Read Match Clip` — positive sound when the read symbol equals the symbol that will be written
- `Read Mismatch Clip` — negative sound when they differ
- `Write Particles` — particle system used when a physical symbol is written or replaced
- `Delete Particles` — particle system used when a physical symbol is cleared to blank
- `Read Hold Seconds` / `Write Effect Seconds` — how long playback waits on those beats (default 0.4)

Behavior:

- Symbol prefabs spawn as children of the owning `TapeCellView` (not of `cellsRoot`)
- Blank cells stay inactive; `ShowWrite` activates the head cell for a physical symbol and deactivates it for blank
- `SetTape` / `Reset` restore `cellsRoot` to its original local position, discard grown cells, and relayout the pool around the initial head
- `ShowRead` / `ShowWrite` call `TapeStepFeedback` at the head cell (holds the playback beat) and raise `TapeRead` / `TapeWrite` Started then Finished for generic listeners
- `MoveHead` raises `TapeMoved` Started then Finished; `MoveAudio` plays/stops the dedicated move source

Wiring:

- Assigned as `viewSceneBindings.tape`
- Receives state updates from `MachineViewer`
- `TapeDebugHotkeys` on the same object: **Left/Right** move the tape, **W** then **0/1/2/3** writes blank/gear/nut/screw

### `VictoryConfetti` (`EventChannelActionListener`)

Purpose: play confetti particles when the player wins.

Prefab: `Assets/Prefabs/View/VictoryConfetti.prefab` (nested Blaster Confetti + listener).

Assign in Inspector:

- `SourceChannel` → `Assets/TuringSimulator/Events/LevelOutcomeChannel.asset`
- `MatchProperty` `Outcome`, `MatchValue` `Victory`
- `OnMatched` → nested `ParticleSystem.Play()` (Play On Awake off)

Place under `View` in `BasicScene`. Spatial spawn is the object's transform.

Wiring:

- `GameFlowController` still raises `LevelOutcome`; this object only consumes Victory

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

- `_apiUrl` (default `https://turing.erwinlabs.dev`)
- `_transcriptionReadyChannel`
- `_askRequestedChannel`
- `_askResultChannel`
- `_thinkingStateChannel`

Wiring:

- Subscribes to `TranscriptionReady`
- Publishes `AskRequested`, then POSTs `/ask`, then `AskResult` / `ThinkingStateChanged`
- If the ITS API is unreachable, still raises a successful `AskResult` whose `Reply` is `O rádio não ta muito bom.`
- Provides session allocation to bootstrap/controller flow

### `TranscriptionAskFallbackListener` (`TranscriptionAskFallbackListener`)

Purpose: when no ITS client is present to post `/ask`, speak the radio fallback as the tutor reply.

Assign in Inspector:

- `_transcriptionReadyChannel`
- `_askResultChannel`
- `_askClient` (optional; uses `ITSClient.Instance` if unset)

Wiring:

- Subscribes to `TranscriptionReady`
- If no ITS client is present and the transcription is non-empty, publishes `AskResult` `Success=true` / `Reply=O rádio não ta muito bom.`
- `AgentActionMapper` AskResult rule then drives `AgentActionExecutor` → `AgentTTS`
- When `ITSClient` is present, it owns `/ask` and the unreachable fallback; this listener does nothing

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
- `_voiceCaptureStoppedChannel`
- `_silenceCommitSeconds` (default 15; `0` commits only on Shaka/T stop)

Wiring:

- Subscribes to `MicToggleRequested`
- `MicListenMode.Start` begins a listen session (no-op if already listening); `Stop` ends it and commits buffered STT; `Toggle` keeps the controller button behavior
- Stops `AgentTTS` when listening starts
- Publishes listening/partial channels immediately; `TranscriptionReady` only after silence or an explicit stop (not on Wit endpointing)
- Uses the latest Meta Voice transcription as-is (no concatenation across utterances)
- Raises `VoiceCaptureStopped` when Wit stops capturing while the session is still waiting for Shaka/T

### `VoiceHearingStoppedCue` (`VoiceHearingStoppedCue`)

Purpose: audio cue when Meta Voice stops hearing. Player-facing stop text is `Cambio` on the right palm (`PalmVoiceCaptionView`), not the agent bubble.

Assign in Inspector:

- `_voiceCaptureStoppedChannel` → `VoiceCaptureStoppedChannel`
- `_audioSource` on the same `Voice` object
- `_clip` → `Button Pop.wav`
- `_agentDialogue` (mic indicator off only; no subtitle)

### `HandGestureMicListener` (`HandGestureMicListener`)

Purpose: hold-to-talk mic from a named hand gesture without calling `VoiceInputHandler` directly.

Assign in Inspector:

- `_handGestureChannel` → `HandGesturePerformedChannel`
- `_micToggleRequestedChannel` → `MicToggleRequestedChannel`
- `_gestureId` → `Shaka`

Wiring:

- `HandGesturePerformed` `Performed` → `MicToggleRequested` `Start`
- `HandGesturePerformed` `Ended` → `MicToggleRequested` `Stop`
- Ignores other gesture ids (ThumbsUp still goes only to `AgentActionMapper`)
- Hold count still ignores duplicate Start/Stop if the same pose retriggers

### `VoiceAskControllerInput` (`VoiceAskControllerInput`)

Purpose: VR/controller mic toggle source.

Assign in Inspector:

- `_micToggleAction` (optional; defaults to right secondary button if missing)
- `_micToggleRequestedChannel`

Wiring:

- Publishes `MicToggleRequested`

### `AppVoiceExperience`

Purpose: Meta Voice SDK runtime object required by `VoiceInputHandler` (Portuguese STT only).

Assign in Inspector:

- Wit Runtime Configuration → **Configuration** → `Assets/WitAI/stt_witconfig.asset` (app `turing_stt`, Portuguese)

Do not assign `tts_witconfig` here.

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

- Updated by `AgentVoiceFeedbackListener` (listening/thinking UI)
- Used by `AgentActionExecutor` to display tutor replies only (player STT stays on the palm caption)

### `AgentTTS` (`AgentTTS`, implements `IAgentSpeech`)

Purpose: synthesizes tutor speech through the scene `TTSSpeaker` (Wit.ai).

Assign in Inspector:

- `TTS Speaker` — the `TTS/TTSSpeaker` object (`WIT$REBECCA` or another preset)
- `Load Timeout Seconds` — hang budget if Wit never starts playback

Wiring:

- Used by `AgentActionExecutor` (`Speak(text)`)
- `AgentAnimator` listens to `OnSpeechFinished`
- `VoiceDebugHotkeys` on the same object: **L** sample TTS, **T** STT overlay

### `TTS` (`TTSWit` + `TTSSpeaker`)

Purpose: Voice SDK TTS service + speaker. Created by **Assets → Create → Voice SDK → TTS → Add Default TTS Setup**.

Assign in Inspector:

- `TTSWit` → Request Settings → **Configuration** → `Assets/WitAI/tts_witconfig.asset` (app `turing_tts`, English TTS)
- `TTSSpeaker` → Voice Preset (e.g. `WIT$REBECCA`)
- Optional: `TTSSpeechSplitter` (`Max Text Length` 250) on `TTSSpeaker`

Do not assign `stt_witconfig`, `Assets/WitAI/witconfig.asset` (`turing`), or the sample `TTS Voices - WitConfiguration` to `TTSWit`.

### `AgentActionMapper` (`AgentActionMapper`)

Purpose: maps arbitrary source event channel payloads to `(text, animation)` actions via rules.

Assign in Inspector:

- `_agentActionChannel`
- `_rules[]` entries:
  - `SourceChannel` (Inspector then lists payload members read-only)
  - optional `MatchProperty` + `MatchValue`
  - `TextMode` + text source
  - `Animation`

Wiring:

- Subscribes to each rule's source channel
- Publishes `AgentActionRequested`

### Hand gestures → agent (`StaticHandGesture` + `HandGestureChannelPublisher`)

Purpose: raise a shared gesture channel from XR Hands sample detectors so `AgentActionMapper` can filter by gesture id/phase.

Assign / create:

- Channel asset: `HandGesturePerformedEventChannel` (**Create → TuringSimulator → Events → Hand Gesture Performed**)
- Sample detector: `StaticHandGesture` (or prefab `Assets/Samples/XR Hands/1.8.0/Gestures/Examples/Prefabs/One Hand Static Gesture.prefab`)
  - `Hand Tracking Events` → left/right `XRHandTrackingEvents` on XR Origin
  - ThumbsUp (both hands): `Hand Shape Or Pose` → `Thumbs Up.asset`
  - Shaka (mic, **Right Hand only**): `Hand Shape Or Pose` → `Shaka.asset` (pose, not only the shape)
- `HandGestureChannelPublisher` (`Assets/TuringSimulator/Controller/Hands/HandGestureChannelPublisher.cs`)
  - `_gestureId` → stable string (`ThumbsUp` or `Shaka`)
  - `_handGestureChannel` → the channel asset
  - UnityEvents from detector:
    - `gesturePerformed` → `PublishPerformed()`
    - `gestureEnded` → `PublishEnded()`

`AgentActionMapper` rule example (ThumbsUp only — do not add a Shaka speech rule):

- `SourceChannel` = `HandGesturePerformedChannel`
- `MatchProperty` = `GestureKey`
- `MatchValue` = `ThumbsUp:Performed`
- `StaticText` / `Animation` as desired

Payload fields available for matching: `GestureId`, `Phase` (`Performed`/`Ended`), `GestureKey` (`GestureId:Phase`).

Shaka hold-to-talk uses the same `HandGesturePerformed` channel from **Right Hand** only, then `HandGestureMicListener` → `MicToggleRequested` `Start`/`Stop`. Do not wire detector UnityEvents to `VoiceInputHandler`.

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
- `_agentDialogue`
- bool names: `_idleBool`, `_thinkingBool`, `_talkingBool`
- trigger name: `_celebrateTrigger` (example `Commemoration`)

Wiring:

- Subscribes to `AgentActionRequested`
- Applies animation state machine parameters
- Stays in Talking while the agent subtitle is visible; goes Idle when the bubble dismisses

### `AgentVoiceFeedbackListener` (`AgentVoiceFeedbackListener`)

Purpose: bridges listening/thinking channels into `AgentDialogue` UI. Does not display player STT.

Assign in Inspector:

- `_listeningStateChannel`
- `_thinkingStateChannel`
- `_agentDialogue`

Wiring:

- Subscribes to those channels
- Calls `AgentDialogue.SetListeningState` and `SetThinkingState`

### `PalmVoiceCaptionView` (`PalmVoiceCaptionView`)

Purpose: player STT on the right-hand palm text. Not shown on the agent bubble.

Assign in Inspector:

- `_label`
- `_handGestureChannel`
- `_listeningStateChannel`
- `_partialTranscriptionChannel`
- `_voiceCaptureStoppedChannel`

Wiring:

- Visible while Shaka is held or a T/mic listen session is open
- Live STT on `PartialTranscription`; UI-only `Cambio` on capture-stopped

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
