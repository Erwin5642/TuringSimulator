---
name: unity-sensory-feedback
description: >-
  Add Unity audio clips and particle VFX as Inspector-wired EventChannelActionListener
  bindings (UnityEvent Play/Stop). Use when adding sounds, loops, particles, or
  other sensory cues to the Turing machine tape, gameplay events, or similar
  view-phase feedback; or when asked how to create a new VFX or audio feedback
  system without growing TapeStepFeedback or writing a new C# listener type.
---

# Unity Sensory Feedback Channels

Pattern: **publisher raises a named phase → `EventChannelSO<T>` asset → `EventChannelActionListener` filters the payload and fires a UnityEvent** (`ParticleSystem.Play`, `AudioSource.Play` / `Stop`, any other parameterless method).

Do not append another clip/particle field onto `TapeStepFeedback`. Do not add a new C# listener type per cue. Do not hang tape-lockstep cues on `PlaybackStep` or `SimulationStepProduced`.

## When to use which channel

| Cue | Channel | Why |
|---|---|---|
| Tape starts/stops sliding | `TapeMovedEventChannel` | Raised in lockstep with the lerp. Payload has `Started` / `Finished` so loops can stop. |
| Read beat | `TapeReadEventChannel` | Raised in lockstep with `ShowRead`. Filter `Phase` and `IsMatch` (`True` / `False`). |
| Write / delete beat | `TapeWriteEventChannel` | Raised in lockstep with `ShowWrite` when the cell actually changes. Filter `Phase` and `Effect` (`Write` / `Delete`). No-ops do not raise. |
| Read / write **hold** (playback wait) | `ITapeStepFeedback` on the tape visual | Channels are fire-and-forget. `TapeStepFeedback` still waits the beat so playback does not skip ahead. |
| Halt, victory, mic stopped, etc. | Existing gameplay channel (`HaltReached`, `LevelOutcome`, `VoiceCaptureStopped`, …) | Already on the event bus; add a listener binding. |

`EventChannelSO` is fire-and-forget. **Playback timing stays on the visual** (`moveDuration`, read/write holds). Bindings must not `WaitForSeconds`.

## Recipe for a new cue

1. **Name the phase** (one thing happened). If it has a start and an end (loop, trail, rumble), put both on one payload enum, like `TapeMovePhase.Started` / `Finished`.
2. **Reuse a channel** if that phase already exists. Otherwise add:
   - `readonly struct …EventData` in `Assets/TuringSimulator/GameFlow/Events/GameplayEventPayloads.cs`
   - `…EventChannel : EventChannelSO<T>` with `CreateAssetMenu`
   - asset under `Assets/TuringSimulator/Events/`
3. **Publisher** (the MonoBehaviour that owns the motion) assigns the channel in the Inspector and `Raise`s at the beat. Include **world position** when a later binding needs it. No `GetComponent` of listeners.
4. **FX object** (prefab or scene child) with the clip / `ParticleSystem` / `AudioSource`, plus `EventChannelActionListener`:
   - `SourceChannel` — drag the channel asset
   - optional `MatchProperty` / `MatchValue` (case-insensitive public field or property; empty = every raise). The assigned channel Inspector lists members and enum/bool values read-only.
   - `OnMatched` — persistent UnityEvent: drag the component, pick the method (`Play`, `Stop`, …)
5. Optional channel on `Assets/TuringSimulator/GameFlow/Events/EventChannelWiringValidator.cs` (soft, not a required demo-path error).
6. Document in `docs/client/EVENT_DRIVEN_DEMO_EVENT_MAP.md` and the Tape/outcome wiring in `docs/client/SCENE_OBJECT_WIRING_MAP.md`.

Do **not** create a new MonoBehaviour whose only job is to call `Play()` on a channel.

## Looping audio (required when the clip can loop)

Worked example: `Tape/MoveAudio` on `Assets/Prefabs/View/Tape.prefab`.

- Dedicated child `AudioSource` so `Stop` does not cut read/write one-shots.
- Clip + **Loop** live on the source. **Play On Awake** off.
- Binding 1: `TapeMovedChannel`, `Phase` = `Started` → `AudioSource.Play()` (loop clip on the source)
- Binding 2: `TapeMovedChannel`, `Phase` = `Started` → `AudioSource.PlayOneShot(Click01)` (optional extra one-shot)
- Binding 3: `TapeMovedChannel`, `Phase` = `Finished` → `AudioSource.Stop()`
- Publisher must raise **Finished** in a `finally` (and on `Reset`) so the loop cannot stick.

## Keep a dedicated C# listener only when it is not a single method call

- `TapeStepFeedback` — waits the playback beat.
- `AgentActionMapper` — tutor text/animation (shares `EventPayloadFilter` only).
- `VoiceHearingStoppedCue` — also clears the AgentDialogue mic indicator. Stop text is palm-only `Cambio`.
- `LevelOutcomeColorIndicator` — color lerp, not `Play()`.

## Do not

- Grow `TapeStepFeedback` with more serialized FX.
- Subscribe tape VFX to `SimulationStepProduced` (too fast) or `PlaybackStep` (fires after the animation).
- Write `VictoryParticles.cs` / `TapeMoveAudio.cs`-style one-off listeners.
- Route a new sensory cue through `AgentActionMapper` unless it is tutor text/animation.

## Reference implementation

- Publisher: `Assets/TuringSimulator/View/Machine/Tape/ConveyorTapeVisual.cs` (`MoveHead`, `ShowRead`, `ShowWrite`)
- Channels: `TapeMovedEventChannel` / `TapeReadEventChannel` / `TapeWriteEventChannel` (assets under `Assets/TuringSimulator/Events/`)
- Listener: `Assets/TuringSimulator/GameFlow/Events/EventChannelActionListener.cs` (filter: `EventPayloadFilter`)
- Victory FX: `Assets/Prefabs/View/VictoryConfetti.prefab` (`LevelOutcome` / `Victory` → `ParticleSystem.Play()`)
- Older one-job SFX that still needs extra logic: `Assets/TuringSimulator/ITS/VoiceHearingStoppedCue.cs`
