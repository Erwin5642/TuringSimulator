# TuringSimulator Docs (Current State)

This folder documents how the repository works **today** (as-is), with emphasis on fast onboarding for humans and coding agents.

## Structure

- `client/`: Unity client architecture, runtime flow, key data paths
- `server/`: FastAPI ITS server architecture, BKT model, API behavior
- `EDITOR_MVP_CHECKLIST.md`: serialized Unity hierarchy and Inspector wiring
- `client/SCENE_OBJECT_WIRING_MAP.md`: per-object scene wiring map for full demo
- `client/EVENT_DRIVEN_DEMO_EVENT_MAP.md`: event-channel trigger map

## System Overview

TuringSimulator is split into two major runtimes:

- **Client (Unity / C#)**: Simulation, level validation, runtime gameplay orchestration, player interaction, and telemetry emission.
- **Server (Python / FastAPI)**: ITS endpoints, Bayesian Knowledge Tracing (BKT), hint orchestration, and live advisory protocol handling.

High-level flow:

1. Unity boots `BasicScene` and initializes game systems from `TuringBootstrap`.
2. Player edits/runs a visual Turing program in the Unity scene.
3. Unity sends session + question traffic to ITS REST endpoints (`/session/new`, `/ask`, `/health`) and receives tutoring responses.
4. Server returns ask responses scoped to the active student session and level context.

## Canonical Entry Points

- Client bootstrap: `Assets/TuringSimulator/GameFlow/TuringBootstrap.cs`
- Gameplay orchestration: `Assets/TuringSimulator/GameFlow/GameFlowController.cs`
- Client ITS REST: `Assets/TuringSimulator/ITS/ITSClient.cs`
- Server app: `TuringBotAPI/main.py`
- Server pedagogy/orchestration: `TuringBotAPI/orchestrator.py`
- Server student model persistence: `TuringBotAPI/student_model.py`

## Important Current-State Notes

- Gameplay is still bootstrapped through `TuringBootstrap`, now simplified to prefer editor scene bindings and only use prefab/runtime fallback when needed.
- Unity progression has eight levels (`LevelDatabase`), each with five validation
  scenarios. Server `LEVEL_META` may still include extra IDs (e.g. `AppendScrew`)
  that are not in the game.
- Per-run personalization is server-issued via `student_id` allocation; returning to menu clears local active session before the next run.
- The current main-line client is voice Ask/Answer scoped, with event-driven
  channel wiring for gameplay and tutor reactions.
- MVP scene wiring is editor-first: the visible workbench, drawer, tutor UI,
  and bootstrap references should be assigned in `BasicScene`, with
  `MvpSceneWiringValidator` available as an Inspector checklist.
- Validation content is data-driven. Eight level definitions are registered in
  `LevelDatabase` with five named scenarios each (tape + head + Accept/Reject
  halt), rather than separate Unity scenes per scenario.

## How To Use These Docs

- Start in `client/README.md` when changing Unity behavior.
- Start in `server/README.md` when changing ITS logic or API contracts.
- When changing contracts between both sides, update both documents in the same PR.
