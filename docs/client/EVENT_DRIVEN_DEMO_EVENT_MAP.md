# Event-Driven Demo Trigger Map (Unity Client)

This document lists the event channels that must be wired for a full playable demo with the slim voice Ask/Answer agent and agent actions (`text + animation`).

Scope: current Unity client runtime (`GameFlow`, `ITS`, `GameFlow/Events`).

Inspector: each channel asset, plus `EventChannelActionListener` bindings and `AgentActionMapper` rules, shows that payload as read-only docs (`MatchProperty` names and enum/bool `MatchValue`s) via `EventPayloadSchema`.

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
21. `TapeMovedEventChannel` (optional; tape slide Started/Finished for move audio/VFX)
22. `TapeReadEventChannel` (optional; read beat Started/Finished; filter `IsMatch`)
23. `TapeWriteEventChannel` (optional; write/delete beat Started/Finished; filter `Effect`)

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
  - Consumed by: `ControllerInstaller` (detect halt path; refresh execution wire colors after step/backward)
  - Side effect: `StepViewApplier.OnStepApplying` (before the tape animation) calls `ProgramWorkbench.HighlightTransition`, switching that wire from `connectedColor` to `previewColor`

- `TapeMoved`
  - Raised by: `ConveyorTapeVisual` at the start and end of a real slide (`Stay` does not raise)
  - Trigger in play mode: playback or debug arrow keys move the tape
  - Consumed by: `EventChannelActionListener` on `Tape/MoveAudio` (`Phase` = `Started` → `AudioSource.Play()` and `PlayOneShot(Click01)`, `Finished` → `Stop()`)
  - Do not use `PlaybackStep` or `SimulationStepProduced` for this cue — those are not lockstep with the slide

- `TapeRead`
  - Raised by: `ConveyorTapeVisual` at the start and end of a read beat (`ShowRead`)
  - Trigger in play mode: playback read, or debug write after the machine has already shown a read
  - Payload: `Phase` (`Started` / `Finished`), `ReadSymbol`, `WriteSymbol`, `IsMatch` (`True` when read equals upcoming write)
  - Consumed by: `EventChannelActionListener` (e.g. `IsMatch` = `True` → match clip `Play()`)
  - `TapeStepFeedback` still waits the read hold so playback stays lockstep; the channel is fire-and-forget

- `TapeWrite`
  - Raised by: `ConveyorTapeVisual` at the start and end of a write or delete (`ShowWrite` no-ops do not raise)
  - Trigger in play mode: playback write, or debug **W** then a symbol
  - Payload: `Phase` (`Started` / `Finished`), `Effect` (`Write` / `Delete`), `Symbol`
  - Consumed by: `EventChannelActionListener` (e.g. `Effect` = `Write` → particles `Play()`)
  - `TapeStepFeedback` still waits the write hold; the channel is fire-and-forget

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
  - Consumed by: `AgentActionMapper` rules (for agent reactions) and `EventChannelActionListener` on `VictoryConfetti` (`Outcome` = `Victory` → `ParticleSystem.Play()`)

### Voice Ask + agent action flow

- `MicToggleRequested`
  - Payload: `MicListenMode` (`Toggle`, `Start`, `Stop`; default `Toggle`)
  - Raised by: `VoiceAskControllerInput` (controller toggle) and `HandGestureMicListener` (Shaka hold-to-talk `Start`/`Stop`)
  - Trigger in play mode: press configured mic toggle button, or hold/release Shaka
  - Consumed by: `VoiceInputHandler` (`Start` / `Stop` / `Toggle`)

- `ListeningStateChanged`
  - Raised by: `VoiceInputHandler` when a Shaka/T listen session starts or commits
  - Trigger in play mode: mic session opens/closes (stays on across Wit pauses)
  - Consumed by: `AgentVoiceFeedbackListener` (mic indicator) and `PalmVoiceCaptionView` (show player STT on the hand)

- `PartialTranscription`
  - Raised by: `VoiceInputHandler` (partial STT chunks)
  - Payload: `partial="..."` with the live Wit string (channel `Raise` records the event-trace row)
  - Trigger in play mode: speak while mic is active
  - Consumed by: `PalmVoiceCaptionView` (player STT on the hand). The agent bubble does not show this text.

- `TranscriptionReady`
  - Raised by: `VoiceInputHandler` after `_silenceCommitSeconds` of no new STT, or when the player stops Shaka / T
  - Payload: `text="..."` with the latest Meta Voice STT string (not joined across utterances)
  - Trigger in play mode: wait 15s after the last word, or release Shaka / second T. A Wit pause alone does not raise this.
  - Consumed by: `ITSClient` (always POSTs `/ask`) and `TranscriptionAskFallbackListener` (radio fallback `AskResult` only when no ITS client is present)

- `VoiceCaptureStopped`
  - Raised by: `VoiceInputHandler` when Wit `OnStoppedListening` fires while the Shaka/T session is still open
  - Trigger in play mode: speak, then wait until Meta Voice stops capturing (before T / Shaka release)
  - Consumed by: `VoiceHearingStoppedCue` (plays `Button Pop.wav`; does not use the agent bubble) and `PalmVoiceCaptionView` (UI-only `Cambio` on the right palm)

- `AskRequested`
  - Raised by: `ITSClient` (before POST `/ask`)
  - Payload: `q="..."`
  - Trigger in play mode: valid transcription + active session
  - Consumed by: `AgentActionMapper` (`Thinking`, empty text)

- `AskResult`
  - Raised by: `ITSClient` (server `/ask` reply, or radio fallback when the API is unreachable)
  - Payload: `success reply="..."`
  - Trigger in play mode: `/ask` returns or cannot reach the host
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

- `TapeView` / `MoveAudio` (`EventChannelActionListener`)
  - assign `TapeMovedEventChannel` (`Assets/TuringSimulator/Events/TapeMovedChannel.asset`)
  - Binding 1: `MatchProperty` `Phase`, `MatchValue` `Started` → `AudioSource.Play()`
  - Binding 2: same filter → `AudioSource.PlayOneShot(Click01.wav)`
  - Binding 3: `MatchProperty` `Phase`, `MatchValue` `Finished` → `AudioSource.Stop()`
  - assign the move clip and tick **Loop** on the dedicated `MoveAudio` `AudioSource` (not the Tape read source)
  - on `ConveyorTapeVisual`, also assign `TapeReadChannel` and `TapeWriteChannel` for read/write FX bindings (`IsMatch` / `Effect`)

- `VictoryConfetti` (`EventChannelActionListener`)
  - assign `LevelOutcomeEventChannel`
  - `MatchProperty` `Outcome`, `MatchValue` `Victory` → nested `ParticleSystem.Play()`
  - keep **Play On Awake** off on the particle system

- `ITSClient`
  - assign:
    - `TranscriptionReadyEventChannel`
    - `AskRequestedEventChannel`
    - `AskResultEventChannel`
    - `ThinkingStateChangedEventChannel`

- `TranscriptionAskFallbackListener`
  - assign `TranscriptionReadyEventChannel` and `AskResultEventChannel`
  - assign `ITSClient` when present (so the listener stays idle; `ITSClient` owns `/ask` and the radio fallback)

- `AgentActionMapper`
  - assign `AgentActionRequestedEventChannel`
  - add rule entries with `SourceChannel` + optional filter + text mode + animation

- `AgentActionExecutor`
  - assign `AgentActionRequestedEventChannel`
  - assign `AgentDialogue` and `AgentTTS` references (or rely on singleton fallback)
  - on `AgentTTS`, assign `TTS Speaker` to the scene `TTSSpeaker`

- `AgentAnimator`
  - assign `AgentActionRequestedEventChannel` and `AgentDialogue`
  - configure animator parameter names (`Idle`, `Thinking`, `Talking`, `Celebrate` trigger)
  - talking stays on until the agent subtitle dismisses, then Idle

- `AgentVoiceFeedbackListener`
  - assign:
    - `ListeningStateChangedEventChannel`
    - `ThinkingStateChangedEventChannel`
  - assign `AgentDialogue` (or use singleton fallback)

- `PalmVoiceCaptionView`
  - assign `HandGesturePerformedEventChannel`, `ListeningStateChangedEventChannel`, `PartialTranscriptionEventChannel`, `VoiceCaptureStoppedEventChannel`

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

## 4c) Example Binding (Victory → Confetti Play)

On `View/VictoryConfetti` (`EventChannelActionListener`), add binding:

- `SourceChannel`: `LevelOutcomeEventChannel`
- `MatchProperty`: `Outcome`
- `MatchValue`: `Victory`
- `OnMatched`: nested `ParticleSystem` → `Play()`

Same pattern on `Tape/MoveAudio` for rumble: `TapeMoved` + `Phase`/`Started` → `Play()`, `Phase`/`Finished` → `Stop()`. Filter matching is shared with `AgentActionMapper` via `EventPayloadFilter`.

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
4. `TranscriptionReady` (after 15s silence or Shaka/T stop) → `ITSClient` `/ask` → mapper `AskResult` → `AgentTTS` (`tts_witconfig` / `turing_tts` / EN). If the ITS API is unreachable, `ITSClient` raises `AskResult` with `Reply` = `O rádio não ta muito bom.` as if it came from the server.

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
