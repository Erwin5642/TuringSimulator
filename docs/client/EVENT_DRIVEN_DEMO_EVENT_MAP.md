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
19. `HandGesturePerformedEventChannel` (optional; hands-only gesture → agent rules)
20. `VoiceCaptureStoppedEventChannel` (optional; Wit capture-stopped cue)

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
  - Side effect in `GameFlowController`: the same `OnStepProduced` handler appends the step to `StepViewApplier` and wakes playback

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
  - Payload: `MicListenMode` (`Toggle`, `Start`, `Stop`; default `Toggle`)
  - Raised by: `VoiceAskControllerInput` (controller toggle) and `HandGestureMicListener` (Shaka hold-to-talk `Start`/`Stop`)
  - Trigger in play mode: press configured mic toggle button, or hold/release Shaka
  - Consumed by: `VoiceInputHandler` (`Start` / `Stop` / `Toggle`)

- `ListeningStateChanged`
  - Raised by: `VoiceInputHandler` when a Shaka/T listen session starts or commits
  - Trigger in play mode: mic session opens/closes (stays on across Wit pauses)
  - Consumed by: `AgentVoiceFeedbackListener` (UI listening indicator)

- `PartialTranscription`
  - Raised by: `VoiceInputHandler` (partial STT chunks)
  - Payload: `partial="..."` with the live Wit string (channel `Raise` records the event-trace row)
  - Trigger in play mode: speak while mic is active
  - Consumed by: `AgentVoiceFeedbackListener` (partial caption text)

- `TranscriptionReady`
  - Raised by: `VoiceInputHandler` after `_silenceCommitSeconds` of no new STT, or when the player stops Shaka / T
  - Payload: `text="..."` with the latest Meta Voice STT string (not joined across utterances)
  - Trigger in play mode: wait 15s after the last word, or release Shaka / second T. A Wit pause alone does not raise this.
  - Consumed by: `ITSClient` (creates ask request when `/ask` is available) and `TranscriptionAskFallbackListener` (echoes STT as `AskResult` when `/ask` cannot be posted)

- `VoiceCaptureStopped`
  - Raised by: `VoiceInputHandler` when Wit `OnStoppedListening` fires while the Shaka/T session is still open
  - Trigger in play mode: speak, then wait until Meta Voice stops capturing (before T / Shaka release)
  - Consumed by: `VoiceHearingStoppedCue` (plays `Button Pop.wav` and shows `O microfone parou de ouvir.`)

- `AskRequested`
  - Raised by: `ITSClient` (before POST `/ask`)
  - Payload: `q="..."`
  - Trigger in play mode: valid transcription + active session
  - Consumed by: `AgentActionMapper` (`Thinking`, empty text)

- `AskResult`
  - Raised by: `ITSClient` (success/failure response)
  - Payload: `success reply="..."`
  - Trigger in play mode: `/ask` returns or fails
  - Consumed by: `AgentActionMapper` (`MatchProperty: Success` / `True` → `Talking`, `TextProperty: Reply`)

- `ThinkingStateChanged`
  - Raised by: `ITSClient` (true while waiting, false when done/fail)
  - Trigger in play mode: ask lifecycle
  - Consumed by: `AgentVoiceFeedbackListener` (loading/thinking UI)

- `AgentActionRequested`
  - Raised by: `AgentActionMapper` (rule output)
  - Trigger in play mode: any mapped source event
  - Consumed by:
    - `AgentActionExecutor` (subtitle + Wit TTS speech)
    - `AgentAnimator` (animation parameters/triggers)

- `HandGesturePerformed` (optional)
  - Raised by: `HandGestureChannelPublisher` (wired from sample `StaticHandGesture` UnityEvents)
  - Trigger in play mode: hold/release a configured hand pose
  - Consumed by:
    - `AgentActionMapper` rules filtered on `GestureId`, `Phase`, or `GestureKey` (ThumbsUp)
    - `HandGestureMicListener` for `_gestureId = Shaka` (mic Start/Stop; not a tutor line)

Event-trace text: keep `PartialTranscription` / `TranscriptionReady` / `AskRequested` / `AskResult` payloads on the channel `ToString()`. `AgentTTS.Speak` also records `AgentSpeechStarted` with the spoken line. `EventTracePanel` `_maxPayloadLength` is 240 so Portuguese sentences stay readable.

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

- `HandGestureMicListener`
  - assign `HandGesturePerformedEventChannel` and `MicToggleRequestedEventChannel`
  - `_gestureId = Shaka`

- `VoiceInputHandler`
  - assign:
    - `MicToggleRequestedEventChannel`
    - `ListeningStateChangedEventChannel`
    - `PartialTranscriptionEventChannel`
    - `TranscriptionReadyEventChannel`
    - `VoiceCaptureStoppedEventChannel`

- `VoiceHearingStoppedCue`
  - assign `VoiceCaptureStoppedEventChannel`, `AudioSource`, `Button Pop.wav`, and `AgentDialogue`

- `ITSClient`
  - assign:
    - `TranscriptionReadyEventChannel`
    - `AskRequestedEventChannel`
    - `AskResultEventChannel`
    - `ThinkingStateChangedEventChannel`

- `TranscriptionAskFallbackListener`
  - assign `TranscriptionReadyEventChannel` and `AskResultEventChannel`
  - assign `ITSClient` when present (so echo only runs when `/ask` cannot be posted)

- `AgentActionMapper`
  - assign `AgentActionRequestedEventChannel`
  - add rule entries with `SourceChannel` + optional filter + text mode + animation

- `AgentActionExecutor`
  - assign `AgentActionRequestedEventChannel`
  - assign `AgentDialogue` and `AgentTTS` references (or rely on singleton fallback)
  - on `AgentTTS`, assign `TTS Speaker` to the scene `TTSSpeaker`

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

## 4b) Example Rule (Hand gesture -> agent action)

Scene wiring:

1. Create asset: **Create → TuringSimulator → Events → Hand Gesture Performed**
2. Add sample `StaticHandGesture` (or `One Hand Static Gesture` prefab) and assign `XRHandTrackingEvents` + hand shape
3. Add `HandGestureChannelPublisher` with `_gestureId = ThumbsUp` and the channel asset
4. Wire `StaticHandGesture.gesturePerformed` → `HandGestureChannelPublisher.PublishPerformed`
5. Wire `StaticHandGesture.gestureEnded` → `HandGestureChannelPublisher.PublishEnded`

In `AgentActionMapper`, add rule:

- `SourceChannel`: `HandGesturePerformedEventChannel`
- `MatchProperty`: `GestureKey`
- `MatchValue`: `ThumbsUp:Performed`
- `TextMode`: `Static`
- `StaticText`: `Parabéns`
- `Animation`: `Celebrate`

Optional release rule:

- `MatchProperty`: `GestureKey`
- `MatchValue`: `ThumbsUp:Ended`
- `TextMode`: `Empty`
- `Animation`: `Idle`

Shaka is mic input, not a tutor mapper rule. Hold-to-talk:

1. Right Hand: second `StaticHandGesture` + `HandGestureChannelPublisher` with pose `Shaka.asset` and `_gestureId = Shaka`
2. `HandGestureMicListener` maps `Performed` → `MicToggleRequested` `Start` and `Ended` → `Stop` (two-hand hold count)
3. `VoiceInputHandler` drives `AppVoiceExperience` (`stt_witconfig` / `turing_stt` / PT). The committed text is the latest Wit string; a listen session can wait for silence or Shaka/T stop before raising `TranscriptionReady`.
4. `TranscriptionReady` (after 15s silence or Shaka/T stop) → `ITSClient` `/ask` → mapper `AskResult` → `AgentTTS` (`tts_witconfig` / `turing_tts` / EN). If `/ask` cannot be posted, `TranscriptionAskFallbackListener` raises `AskResult` with `Reply` = STT text instead.

Do not wire `StaticHandGesture` UnityEvents to `VoiceInputHandler.StartListening` / `StopListening`.

Ask mapper rules (pt-BR subtitles; English TTS voice):

- `AskRequested` → `Thinking`, `TextMode: Empty`
- `AskResult` / `MatchProperty: Success` / `True` → `Talking`, `TextMode: PayloadProperty` / `Reply`

## 5) Smoke Test Sequence

1. Enter play mode and run `EventChannelWiringValidator`.
2. Start session from menu and load level.
3. Edit program and run it.
4. Confirm halt -> validation -> level outcome events.
5. Press mic toggle **or hold Shaka**, speak Portuguese, pause through a short gap, then wait 15s **or** release Shaka / press T.
6. Confirm ask lifecycle events (`TranscriptionReady` `text=` -> `AskRequested` `q=` -> `AskResult` `reply=`) on the event trace, plus `AgentSpeechStarted` with the spoken line.
7. Confirm `AgentActionRequested` fires and drives both:
   - speech/subtitles (`AgentActionExecutor`)
   - animation (`AgentAnimator`)
