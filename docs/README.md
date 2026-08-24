# TuringSimulator Docs (Current State)

This folder documents how the repository works **today** (as-is), with emphasis on fast onboarding for humans and coding agents.

## Structure

- `client/`: Unity client architecture, runtime flow, key data paths
- `server/`: FastAPI ITS server architecture, agentic RAG, API behavior
- `EDITOR_MVP_CHECKLIST.md`: serialized Unity hierarchy and Inspector wiring
- `client/SCENE_OBJECT_WIRING_MAP.md`: per-object scene wiring map for full demo
- `client/EVENT_DRIVEN_DEMO_EVENT_MAP.md`: event-channel trigger map

## System Overview

TuringSimulator is split into two major runtimes:

- **Client (Unity / C#)**: Simulation, level validation, runtime gameplay orchestration, player interaction, and telemetry emission.
- **Server (Python / FastAPI)**: ITS `/ask` tutor via agentic RAG over markdown knowledge docs.

High-level flow:

1. Unity boots `BasicScene` and initializes game systems from `TuringBootstrap`.
2. Player edits/runs a visual Turing program in the Unity scene.
3. Unity sends session + question traffic to ITS REST endpoints (`/session/new`, `/ask`, `/health`) and receives tutoring responses (`reply` plus optional `audio_url`). Tutor speech is synthesized in Unity with Wit.ai TTS.
4. Server searches the knowledge corpus (boosted by `level_id`) and returns a pt-BR tutor reply.

## Canonical Entry Points

- Client bootstrap: `Assets/TuringSimulator/GameFlow/TuringBootstrap.cs`
- Gameplay orchestration: `Assets/TuringSimulator/GameFlow/GameFlowController.cs`
- Client ITS REST: `Assets/TuringSimulator/ITS/ITSClient.cs`
- Server app: `TuringBotAPI/main.py`
- Server RAG agent: `TuringBotAPI/agent.py`
- Server knowledge corpus: `TuringBotAPI/knowledge/`

## Important Current-State Notes

- Gameplay is still bootstrapped through `TuringBootstrap`, now simplified to prefer editor scene bindings and only use prefab/runtime fallback when needed.
- Unity progression has eight levels (`LevelDatabase`), each with five validation
  scenarios. Server goal docs use those same `levelId`s (no `AppendScrew`).
- `student_id` from `/session/new` is a per-run identity on `/ask` payloads; the server does not store BKT or chat history. Returning to menu clears the local active session before the next run.
- The current main-line client is voice Ask/Answer scoped, with event-driven
  channel wiring for gameplay and tutor reactions.
- Wit STT and TTS use **separate** apps: `stt_witconfig` (`turing_stt`, Portuguese)
  on `AppVoiceExperience`, and `tts_witconfig` (`turing_tts`, English) on `TTSWit`.
  Hold Shaka to listen; release to finalize transcription. If `/ask` cannot be
  posted, the tutor repeats the STT text through Wit TTS.
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
