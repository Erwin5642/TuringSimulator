# Client Architecture (Unity, Current State)

This document describes the Unity client as implemented right now.

Companion setup docs:

- `docs/client/SCENE_OBJECT_WIRING_MAP.md` (scene objects + inspector wiring)
- `docs/client/EVENT_DRIVEN_DEMO_EVENT_MAP.md` (event channels + trigger chain)

## Runtime Boot and Wiring

Primary boot path:

1. `BasicScene` loads.
2. `TuringBootstrap` (`Awake`) runs phased setup:
   - `BindObjects()`: resolves ITS components from editor-assigned references (or same root object fallback)
   - `InitializeObjects()`: builds `ModelInstaller`
   - `CreateObjects()`: builds `ViewInstaller` using **scene bindings first**, prefab fallback second
   - `PrepareGameObjects()`: builds `ControllerInstaller` using **scene bindings first**, prefab fallback second
   - `BeginGame()`: calls `GameFlowController.Start()`

Key file: `Assets/TuringSimulator/GameFlow/TuringBootstrap.cs`

## Core Installers

- `ModelInstaller`:
  - `LevelContext`, `LevelLoader`
  - `SimulationRunner` (`SimulationRunRequest` -> `SimulationRunResult`)
  - `SimulationBuffer` (engine step capture + trace source)
  - active run state (`CurrentProgram`, `CurrentTape`)
  - `ValidationRunner`
- `ViewInstaller`:
  - Preferentially uses scene-bound references for `machine`, `tape`, `halt`, `levelUI`
  - Falls back to prefab instantiation only if scene bindings are missing
  - `ITapeVisual.Initialize()` uses the existing `TapeCellView` pool under `cellsRoot`, then `MachineViewer` drives `SetTape` / `MoveHead` / `ShowWrite`
- `ControllerInstaller`:
  - Creates `ProgramEditController`, `PlaybackController`, `StepViewApplier`, `GameFlowController`
  - Preferentially uses scene-bound `PlayerInputCatcher` (XR/editor wiring)
  - Falls back to input prefab instantiation only if scene binding is missing
  - Wires runtime events between systems

Files:

- `Assets/TuringSimulator/GameFlow/ModelInstaller.cs`
- `Assets/TuringSimulator/GameFlow/ViewInstaller.cs`
- `Assets/TuringSimulator/GameFlow/ControllerInstaller.cs`

## Tape Visual

`ConveyorTapeVisual` (`Assets/Prefabs/View/Tape.prefab`):

- `cellsRoot` slides left/right with the head and keeps that offset. It is not reset to the tape origin after a move.
- `Initialize()` keeps the prefab `TapeCellView` pool (about 10 cells). A new cell is cloned only when the head leaves that range.
- Blank cells stay inactive. `ShowWrite` activates the head cell for a physical symbol and deactivates it for blank.
- Symbol prefabs spawn as children of each `TapeCellView`. `SetTape` / `Reset` restore the original `cellsRoot` position and the initial pool.

Editor debug (`TapeDebugHotkeys` on the Tape prefab, Play Mode):

- **Left / Right arrows** — slide the tape
- **W** then **0 / 1 / 2 / 3** — write blank / gear / nut / screw (W or Esc cancels)

Files:

- `Assets/TuringSimulator/View/Machine/Tape/ConveyorTapeVisual.cs`
- `Assets/TuringSimulator/View/Machine/Tape/TapeDebugHotkeys.cs`

## Game State and Flow

State enum: `Assets/TuringSimulator/GameFlow/GameState.cs`

Key execution path in `GameFlowController`:

- `Start()`:
  - `Menu -> Loading -> Editing`
  - loads current level
  - enables program editing
- `Run()`:
  - transitions to `Running`
  - emits run lifecycle channels (`RunStarted`, `SimulationStepProduced`, `RunFinished`)
  - enables playback immediately and starts play-requested mode
  - appends each produced step into `StepViewApplier` and wakes playback as soon as any step exists
  - does not wait for the full simulation to finish before the machine starts moving
- `Abort()`:
  - cancels the in-flight simulation coroutine
  - pauses/disables playback
  - clears simulation state, resets the machine/tape, reloads the current level
  - returns to `Editing` so the program can be changed again
- `Halt()`:
  - transitions `Running -> Halted -> Validating`
  - runs validation tests
  - emits `ValidationCompleted` and `LevelOutcome` channels
  - transitions to `Victory` or `Defeat`
- `Next()`:
  - resets simulation/view state
  - loads current or next level based on previous outcome
  - returns to editing

File: `Assets/TuringSimulator/GameFlow/GameFlowController.cs`

## Program Editing and Compilation

Primary editing path:

- `ProgramWorkbench` owns the start/power port and block/card/wire topology.
- **Start port unwired** → active program is cleared to halt (no transitions; engine rejects immediately).
- **Start port wired to a block input** → that block is the directed-graph entry; only blocks reachable from it are compiled.
- Builds `ProgramGraphSnapshot`, fingerprints it (`ProgramGraphFingerprint`), and skips recompile when unchanged.
- Off-start wire changes are filtered via `IProgramBlockConnectivity` (union-find); disconnects rebuild the forest from current edges.
- Compiles via `GraphToProgramCompiler` into `IProgramEditController`. Compile failure keeps the previous program.
- `ProgramChangedEventData.TransitionCount` is the real transition-table size (`IProgram.TransitionCount`).

Halt rules (`SimulationEngine`):

- Entering / being in an Accept (final) state → `HaltStatus.Accept`
- No matching transition for the current state/symbol → implicit `HaltStatus.Reject`
- Unwired Move/Write compiles to a non-final sink (so the run rejects unless wired into Accept)

Main files:

- `Assets/TuringSimulator/Controller/ProgramWorkbench.cs`
- `Assets/TuringSimulator/Controller/GraphToProgramCompiler.cs`
- `Assets/TuringSimulator/Controller/ProgramEditController.cs`
- `Assets/TuringSimulator/Core/ProgramGraph/ProgramGraphFingerprint.cs`
- `Assets/TuringSimulator/Core/ProgramGraph/IProgramBlockConnectivity.cs`
- `Assets/TuringSimulator/Core/Simulation/SimulationEngine.cs`

## Level Data

Level definitions are Unity assets containing:

- UI presentation (`title`, `description`) in pt-BR
- ITS-compatible `levelId` matching `LevelID` and `TuringBotAPI/knowledge/goals/`
- validation tests (`mainTest`, `validationTests`)

Progression in `LevelDatabase.asset` (order = play order; eight levels):

1. `MoveLeftRight` — move only; ends in Reject (no Accept module yet)
2. `PlaceGear` — move + write; ends in Reject (no Accept module yet)
3. `ReplaceAllWithNuts` — finish via Accept module
4. `RejectIfGearExists` — Accept or Reject modules
5. `SwapNutsAndScrews` — finish via Accept module
6. `PatternRepeated` — Accept or Reject modules
7. `BalancedPairs` — Accept or Reject modules
8. `PatternSomewhere` — Accept or Reject modules

Each level has **5** validation scenarios (`mainTest` + 4 `validationTests`).
`AppendScrew` is not in the Unity progression (removed from the game).

Main files:

- `Assets/TuringSimulator/Core/Level/LevelDefinition.cs`
- `Assets/TuringSimulator/Core/Level/LevelDatabase.cs`
- `Assets/Prefabs/Levels/LevelDatabase.asset`
- `Assets/Prefabs/Levels/Level */Level * Definition.asset`
- `Assets/Prefabs/Levels/Level */Level * Test*.asset` / `* Main Test.asset`

## ITS Integration from Client

### REST (`ITSClient`)

- `/session/new`: allocates a fresh student session id for each new run
- `/ask`: free-form question (from voice transcription pipeline). Reply JSON includes optional `audio_url` (MVP: always omitted/null; Unity synthesizes speech with Wit TTS and ignores the URL).
- health check via `/health`

File: `Assets/TuringSimulator/ITS/ITSClient.cs`

### Skill tracking (`SkillTracker`)

- Holds active `student_id`
- Holds current `level_id` for `/ask` payload context
- No BKT/event telemetry logic on this slim main line

Files:

- `Assets/TuringSimulator/ITS/SkillTracker.cs`
- `Assets/TuringSimulator/ITS/ITSModel.cs`

### Event-driven Ask pipeline

Voice and tutoring path is channel-based:

- Controller mic button -> `MicToggleRequestedEventChannel` (`MicListenMode.Toggle`)
- Hold Shaka (right hand) -> `HandGesturePerformed` -> `HandGestureMicListener` -> `MicToggleRequested` (`Start` / `Stop`)
- STT lifecycle -> `ListeningStateChanged`, `PartialTranscription`, `TranscriptionReady`
- `TranscriptionReady` is raised only after `_silenceCommitSeconds` (default 15) of no new STT, or when Shaka / T stops. The text is the latest Meta Voice string (not concatenated).
- When Wit stops capturing before that commit, `VoiceCaptureStopped` plays a short cue (`Button Pop.wav`) and shows `O microfone parou de ouvir.`
- Ask lifecycle -> `AskRequested`, `AskResult`, `ThinkingStateChanged`
- If `/ask` cannot be posted (`ITSClient` missing or server down), `TranscriptionAskFallbackListener` raises a successful `AskResult` whose `Reply` is the STT text, so the tutor repeats exactly what was heard via `AgentTTS` / `tts_witconfig`
- Agent reaction tuple -> `AgentActionRequestedEventData` (`text`, `animation`, optional `audioUrl`)

Main files:

- `Assets/TuringSimulator/Controller/Hands/HandGestureMicListener.cs`
- `Assets/TuringSimulator/ITS/VoiceAskControllerInput.cs`
- `Assets/TuringSimulator/ITS/VoiceInputHandler.cs`
- `Assets/TuringSimulator/ITS/TranscriptionAskFallbackListener.cs`
- `Assets/TuringSimulator/ITS/AgentActionMapper.cs`
- `Assets/TuringSimulator/ITS/AgentActionExecutor.cs`
- `Assets/TuringSimulator/ITS/AgentAnimator.cs`
- `Assets/TuringSimulator/ITS/AgentTTS.cs`
- `Assets/TuringSimulator/ITS/VoiceDebugHotkeys.cs`

STT and TTS must use different Wit apps:

- **STT (Portuguese):** `stt_witconfig` / app `turing_stt` on `AppVoiceExperience` only
- **TTS (English):** `tts_witconfig` / app `turing_tts` on `TTS/TTSWitService` only

Do not assign `stt_witconfig` to `TTSWit`, or `tts_witconfig` to `AppVoiceExperience`. Ignore `Assets/WitAI/witconfig.asset` (`turing`) for production wiring.

Tutor subtitles stay pt-BR. Spoken audio uses the English `turing_tts` voice until Meta adds Portuguese TTS.

### Agent speech (`AgentTTS` implements `IAgentSpeech`)

Tutor lines are synthesized on-device by Wit.ai TTS (Voice SDK). `AgentTTS.Speak(text, audioUrl)`:

- Sends `text` to the scene `TTSSpeaker` (`Speak`, interrupting any current line).
- Ignores `audioUrl` (ITS still may send `audio_url: null`).
- Raises `OnSpeechStarted` immediately so subtitles stay up during download, then `OnSpeechFinished` when Wit playback (including split phrases) is idle.
- On load failure or timeout, raises `OnSpeechError` and still finishes so animation does not hang.

Scene objects: `TTS/TTSWitService` (config = `Assets/WitAI/tts_witconfig.asset`, app `turing_tts` / English) and `TTS/TTSSpeaker` (preset `WIT$REBECCA`). `AgentTTS.Speak` also records `AgentSpeechStarted` on the event trace with the spoken line.

Editor debug (`VoiceDebugHotkeys` on `AgentTTS`, Play Mode):

- **L** — speak a predefined Portuguese sample and show it in the subtitle bubble (audio still uses English `turing_tts`).
- **T** — toggle STT and show partial/final text on an overlay (`VoiceInputHandler` + `AppVoiceExperience` with `stt_witconfig`).

Files:

- `Assets/TuringSimulator/ITS/IAgentSpeech.cs`
- `Assets/TuringSimulator/ITS/AgentTTS.cs`
- `Assets/TuringSimulator/ITS/VoiceDebugHotkeys.cs`
- `Assets/TuringSimulator/ITS/AgentSpeechDuration.cs`

## XR / Editor-Oriented Wiring Notes

- The project is XR Toolkit-oriented; wiring input and scene objects in the editor is now the preferred integration path.
- `PlayerInputCatcher` keyboard bindings remain available as development fallback; XR button/menu interactions should call the same start/menu flow methods through scene event wiring.
- `TuringBootstrap` is now a thinner composition root with editor-first references and optional auto-start.
- `MvpSceneWiringValidator` can be attached to the scene `Systems` root and
  invoked from its Inspector context menu. It reports missing bootstrap,
  workbench, tutor, drawer, and validation-scenario references.
- `ProgramWorkbench` and the tutor components are intentionally expected to be
  assigned in the scene; runtime fallback does not create a visible editing
  layout.
- Event channel wiring for the full demo path is documented in:
  - `docs/client/EVENT_DRIVEN_DEMO_EVENT_MAP.md`

## AI-Agent Safe Invariants (Client)

- Do not bypass `TuringBootstrap` for core system creation unless migrating architecture intentionally.
- Keep `levelId` in `LevelDefinition` aligned with `TuringBotAPI/knowledge/goals/`.
- If adding new ITS events, update both client DTO constants and server contract handling.
- `SkillTracker.StudentId` is session identity for `/ask` payloads. Treat changes as product-sensitive.

## Known Gaps

- Main-menu UI scene flow is still not fully wired; runtime now supports menu detach/start hooks and keyboard menu return (`M`) with fresh session on next start.
- Runtime instantiation is used heavily; scene-only wiring is not the current architecture.
- Unity ships eight levels with five named validation scenarios each (40 total).
- Server goal docs cover the eight Unity `LevelDatabase` ids; `AppendScrew` is not a playable level.
- `ValidationTest.scenarioId` and `ValidationRunner.Results` provide stable names
  and per-scenario summaries for the editor/UI. Validation matches halt status,
  final head index, and tape contents.
