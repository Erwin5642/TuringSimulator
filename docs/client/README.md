# Client Architecture (Unity, Current State)

This document describes the Unity client as implemented right now.

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
  - `SimulationRunner` + `SimulationBuffer`
  - `ValidationRunner`
- `ViewInstaller`:
  - Preferentially uses scene-bound references for `machine`, `tape`, `halt`, `levelUI`
  - Falls back to prefab instantiation only if scene bindings are missing
- `ControllerInstaller`:
  - Creates `ProgramEditController`, `PlaybackController`, `StepViewApplier`, `GameFlowController`
  - Exposes command methods (`RequestStartOrRun`, playback/next/menu requests) for XR UI wiring
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
  - sends live lifecycle events
  - runs simulation asynchronously
  - enables playback when finished
- `Halt()`:
  - transitions `Running -> Halted -> Validating`
  - runs validation tests
  - emits success/fail evidence to `SkillTracker`
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
- `/event`: skill evidence updates
- `/ask`: free-form question
- `/hint`: graduated hints
- health check via `/health`

File: `Assets/TuringSimulator/ITS/ITSClient.cs`

### Live socket (`LiveTutorSocket`)

- Connects to `/ws/live`
- Sends:
  - handshake
  - run lifecycle
  - level snapshot
  - sim step / sim halt
- Receives advisory messages via inbound handler
- `SkillTracker.BeginSession()` binds the socket to the newly allocated
  student ID and rotates the WebSocket session ID before connecting.
- `SkillTracker.ClearSession()` closes the live channel and clears the active
  player binding, preventing the next player from inheriting telemetry.

File: `Assets/TuringSimulator/ITS/LiveTutorSocket.cs`

### Skill tracking (`SkillTracker`)

- Maps gameplay outcomes into ITS skill IDs
- Emits events with `StudentId` + `levelId`
- Maintains current level context
- Holds active session identity and blocks telemetry if no session is active

## XR / Editor-Oriented Wiring Notes

- The project is XR Toolkit-oriented; wiring input and scene objects in the editor is now the preferred integration path.
- Keyboard fallback input was removed; use XR simulator or headset interactions for all run/playback/menu controls.
- XR buttons can call `TuringBootstrap` methods (`StartOrRunFromInteraction`, `PausePlaybackFromInteraction`, `PlayPlaybackFromInteraction`, `StepForwardFromInteraction`, `StepBackwardFromInteraction`, `NextLevelFromInteraction`, `ReturnToMainMenu`).
- `TuringBootstrap` is now a thinner composition root with editor-first references and optional auto-start.
- `MvpSceneWiringValidator` can be attached to the scene `Systems` root and
  invoked from its Inspector context menu. It reports missing bootstrap,
  workbench, tutor, drawer, and validation-scenario references.
- `ProgramWorkbench` and the tutor components are intentionally expected to be
  assigned in the scene; runtime fallback does not create a visible editing
  layout.

File: `Assets/TuringSimulator/ITS/SkillTracker.cs`

## AI-Agent Safe Invariants (Client)

- Do not bypass `TuringBootstrap` for core system creation unless migrating architecture intentionally.
- Keep `levelId` in `LevelDefinition` aligned with server level metadata.
- If adding new ITS events, update both client DTO constants and server contract handling.
- `SkillTracker.StudentId` is session identity for REST/live payloads. Treat changes as product-sensitive.
- Make sure to use portuguese for any text that may be displayed on UI and agent dialogue.

## Known Gaps

- Main-menu UI scene flow is still not fully wired; runtime supports menu detach/start hooks through XR-wired button events.
- Runtime instantiation is used heavily; scene-only wiring is not the current architecture.
- Unity level content coverage may lag server pedagogical map.
- The current repository contains five validation fixtures across two levels;
  the MVP target is ten named scenarios. `ValidationTest.scenarioId` and
  `ValidationRunner.Results` provide stable names and per-scenario summaries
  for the editor/UI.
