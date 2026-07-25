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
- `ControllerInstaller`:
  - Creates `ProgramEditController`, `PlaybackController`, `StepViewApplier`, `GameFlowController`
  - Preferentially uses scene-bound `PlayerInputCatcher` (XR/editor wiring)
  - Falls back to input prefab instantiation only if scene binding is missing
  - Wires runtime events between systems

Files:

- `Assets/TuringSimulator/GameFlow/ModelInstaller.cs`
- `Assets/TuringSimulator/GameFlow/ViewInstaller.cs`
- `Assets/TuringSimulator/GameFlow/ControllerInstaller.cs`

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
  - runs simulation asynchronously
  - loads playback timeline from `SimulationRunResult.Steps`
  - enables playback when finished
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

- `ProgramWorkbench` reads scene block/card/wire topology.
- Builds `ProgramGraphSnapshot`.
- Compiles via `GraphToProgramCompiler`.
- Replaces active program in `IProgramEditController`.

Main files:

- `Assets/TuringSimulator/Controller/ProgramWorkbench.cs`
- `Assets/TuringSimulator/Controller/GraphToProgramCompiler.cs`
- `Assets/TuringSimulator/Controller/ProgramEditController.cs`

## Level Data

Level definitions are Unity assets containing:

- UI presentation (`title`, `description`)
- ITS-compatible `levelId`
- validation tests (`mainTest`, `validationTests`)

Main files:

- `Assets/TuringSimulator/Core/Level/LevelDefinition.cs`
- `Assets/TuringSimulator/Core/Level/LevelDatabase.cs`
- `Assets/Prefabs/Levels/LevelDatabase.asset`

## ITS Integration from Client

### REST (`ITSClient`)

- `/session/new`: allocates a fresh student session id for each new run
- `/ask`: free-form question (from voice transcription pipeline)
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

- Controller mic button -> `MicToggleRequestedEventChannel`
- STT lifecycle -> `ListeningStateChanged`, `PartialTranscription`, `TranscriptionReady`
- Ask lifecycle -> `AskRequested`, `AskResult`, `ThinkingStateChanged`
- Agent reaction tuple -> `AgentActionRequestedEventData` (`text`, `animation`)

Main files:

- `Assets/TuringSimulator/ITS/VoiceAskControllerInput.cs`
- `Assets/TuringSimulator/ITS/VoiceInputHandler.cs`
- `Assets/TuringSimulator/ITS/AgentActionMapper.cs`
- `Assets/TuringSimulator/ITS/AgentActionExecutor.cs`
- `Assets/TuringSimulator/ITS/AgentAnimator.cs`

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
- Keep `levelId` in `LevelDefinition` aligned with server level metadata.
- If adding new ITS events, update both client DTO constants and server contract handling.
- `SkillTracker.StudentId` is session identity for `/ask` payloads. Treat changes as product-sensitive.

## Known Gaps

- Main-menu UI scene flow is still not fully wired; runtime now supports menu detach/start hooks and keyboard menu return (`M`) with fresh session on next start.
- Runtime instantiation is used heavily; scene-only wiring is not the current architecture.
- Unity level content coverage may lag server pedagogical map.
- The current repository contains five validation fixtures across two levels;
  the MVP target is ten named scenarios. `ValidationTest.scenarioId` and
  `ValidationRunner.Results` provide stable names and per-scenario summaries
  for the editor/UI.
