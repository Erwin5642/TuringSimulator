# Event-Driven Demo Trigger Map (Unity Client)

This document lists the event channels that must be wired for a full playable demo with the slim voice Ask/Answer agent and agent actions (`text + animation`).

Scope: current Unity client runtime (`GameFlow`, `ITS`, `GameFlow/Events`).

## 1) Required Channel Assets

Create one ScriptableObject asset for each channel class:

1. `RunRequestedEventChannel`
2. `RunStartedEventChannel`
3. `RunFinishedEventChannel`
4. `LevelLoadedEventChannel`
5. `ProgramChangedEventChannel`
6. `PlaybackStepEventChannel`
7. `SimulationStepProducedEventChannel`
8. `HaltReachedEventChannel`
9. `ValidationCompletedEventChannel`
10. `LevelOutcomeEventChannel`
11. `MicToggleRequestedEventChannel`
12. `ListeningStateChangedEventChannel`
13. `PartialTranscriptionEventChannel`
14. `TranscriptionReadyEventChannel`
15. `AskRequestedEventChannel`
16. `AskResultEventChannel`
17. `ThinkingStateChangedEventChannel`
18. `AgentActionRequestedEventChannel`

## 2) Event Chain for Full Demo

### Gameplay flow

- `RunRequested`
  - Raised by: `ControllerInstaller` (input start command)
  - Trigger in play mode: start from menu, or run from editing
  - Consumed by: `ControllerInstaller` (starts session/starts run)

- `LevelLoaded`
  - Raised by: `ControllerInstaller` (when `LevelLoader` changes level)
  - Trigger in play mode: start game, or `Next` after victory/defeat
  - Consumed by: `ControllerInstaller` (tape/model/view/session context setup)

- `ProgramChanged`
  - Raised by: `ControllerInstaller` (from `ProgramEditController` updates)
  - Trigger in play mode: edit/rebuild program in workbench
  - Consumed by: `ControllerInstaller` (sets simulation + validation program)

- `RunStarted`
  - Raised by: `GameFlowController` (when entering `Running`)
  - Trigger in play mode: run current program
  - Consumed by: optional analytics/listeners

- `RunFinished`
  - Raised by: `GameFlowController` (after simulation loop ends)
  - Trigger in play mode: simulation reaches halt/finish
  - Consumed by: optional analytics/listeners

- `PlaybackStep`
  - Raised by: `ControllerInstaller` (each playback step result)
  - Trigger in play mode: run/play/step controls
  - Consumed by: `ControllerInstaller` (detect halt path)

- `SimulationStepProduced`
  - Raised by: `GameFlowController` (subscribed to `SimulationRunner.OnStepProduced`)
  - Trigger in play mode: each simulation engine step during run
  - Consumed by: optional analytics/timeline listeners

- `HaltReached`
  - Raised by: `ControllerInstaller` (when step kind is halt)
  - Trigger in play mode: machine halts
  - Consumed by: `ControllerInstaller` (calls `GameFlowController.Halt()`)

- `ValidationCompleted`
  - Raised by: `GameFlowController` (after validation finishes)
  - Trigger in play mode: halt then validation
  - Consumed by: optional listeners

- `LevelOutcome`
  - Raised by: `GameFlowController` (on `Victory()`/`Defeat()`)
  - Trigger in play mode: validation pass/fail
  - Consumed by: `AgentActionMapper` rules (for agent reactions)

### Voice Ask + agent action flow

- `MicToggleRequested`
  - Raised by: `VoiceAskControllerInput` (VR button action)
  - Trigger in play mode: press configured mic toggle button
  - Consumed by: `VoiceInputHandler` (start/stop listening)

- `ListeningStateChanged`
  - Raised by: `VoiceInputHandler` (`OnStartListening`/`OnStoppedListening`)
  - Trigger in play mode: mic opens/closes
  - Consumed by: `AgentVoiceFeedbackListener` (UI listening indicator)

- `PartialTranscription`
  - Raised by: `VoiceInputHandler` (partial STT chunks)
  - Trigger in play mode: speak while mic is active
  - Consumed by: `AgentVoiceFeedbackListener` (partial caption text)

- `TranscriptionReady`
  - Raised by: `VoiceInputHandler` (final STT text)
  - Trigger in play mode: finish utterance
  - Consumed by: `ITSClient` (creates ask request)

- `AskRequested`
  - Raised by: `ITSClient` (before POST `/ask`)
  - Trigger in play mode: valid transcription + active session
  - Consumed by: `AgentActionMapper` (commonly map to thinking animation)

- `AskResult`
  - Raised by: `ITSClient` (success/failure response)
  - Trigger in play mode: `/ask` returns or fails
  - Consumed by: `AgentActionMapper` (map to talking + text)

- `ThinkingStateChanged`
  - Raised by: `ITSClient` (true while waiting, false when done/fail)
  - Trigger in play mode: ask lifecycle
  - Consumed by: `AgentVoiceFeedbackListener` (loading/thinking UI)

- `AgentActionRequested`
  - Raised by: `AgentActionMapper` (rule output)
  - Trigger in play mode: any mapped source event
  - Consumed by:
    - `AgentActionExecutor` (subtitle + TTS)
    - `AgentAnimator` (animation parameters/triggers)

## 3) Inspector Wiring Matrix (Minimum)

- `ControllerSceneBindings` (inside `TuringBootstrap`)
  - assign gameplay channels:
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

- `VoiceAskControllerInput`
  - assign `MicToggleRequestedEventChannel`

- `VoiceInputHandler`
  - assign:
    - `MicToggleRequestedEventChannel`
    - `ListeningStateChangedEventChannel`
    - `PartialTranscriptionEventChannel`
    - `TranscriptionReadyEventChannel`

- `ITSClient`
  - assign:
    - `TranscriptionReadyEventChannel`
    - `AskRequestedEventChannel`
    - `AskResultEventChannel`
    - `ThinkingStateChangedEventChannel`

- `AgentActionMapper`
  - assign `AgentActionRequestedEventChannel`
  - add rule entries with `SourceChannel` + optional filter + text mode + animation

- `AgentActionExecutor`
  - assign `AgentActionRequestedEventChannel`
  - assign `AgentDialogue` and `AgentTTS` references (or rely on singleton fallback)

- `AgentAnimator`
  - assign `AgentActionRequestedEventChannel`
  - configure animator parameter names (`Idle`, `Thinking`, `Talking`, `Celebrate` trigger)

- `AgentVoiceFeedbackListener`
  - assign:
    - `ListeningStateChangedEventChannel`
    - `PartialTranscriptionEventChannel`
    - `ThinkingStateChangedEventChannel`
  - assign `AgentDialogue` (or use singleton fallback)

- `AgentDialogue`
  - set `_useLegacyDirectWiring = false` for pure event-driven mode

- `EventChannelWiringValidator`
  - assign all channels listed in section 1
  - run `Validate Event Channels` from inspector context menu

## 4) Example Rule (Victory -> "Parabéns" + Commemoration)

In `AgentActionMapper`, add rule:

- `SourceChannel`: `LevelOutcomeEventChannel`
- `MatchProperty`: `Outcome`
- `MatchValue`: `Victory`
- `TextMode`: `Static`
- `StaticText`: `Parabéns`
- `Animation`: `Celebrate`

In `AgentAnimator`:

- set `_celebrateTrigger` to your animator trigger name (example: `Commemoration`)

## 5) Smoke Test Sequence

1. Enter play mode and run `EventChannelWiringValidator`.
2. Start session from menu and load level.
3. Edit program and run it.
4. Confirm halt -> validation -> level outcome events.
5. Press mic toggle button and speak.
6. Confirm ask lifecycle events (`TranscriptionReady` -> `AskRequested` -> `AskResult`).
7. Confirm `AgentActionRequested` fires and drives both:
   - speech/subtitles (`AgentActionExecutor`)
   - animation (`AgentAnimator`)
