# Server Architecture (TuringBotAPI, Current State)

This document describes the ITS server behavior as implemented today.

## Entry Point and Lifecycle

Main app file: `TuringBotAPI/main.py`

- FastAPI app with lifespan hooks:
  - startup: `setup_logging()`, `STUDENT_MODEL.load()`
  - shutdown: `STUDENT_MODEL.save()`
- CORS currently open (`allow_origins=["*"]`) for local development.

## REST API Surface

- `POST /event`
  - validates incoming skill IDs
  - updates BKT via `STUDENT_MODEL.observe(student_id, skill_id, correct)`
  - optionally generates a short reactive comment
  - persists state immediately
- `POST /ask`
  - generates a free-form tutoring reply
- `POST /hint`
  - chooses weak skill (if not provided)
  - escalates hint level per student/skill
  - returns structured hint response
- `GET /state/{student_id}`
  - debug introspection of knowledge state
- `GET /health`
  - basic health/version check plus active tutor provider (`gemini` or `fallback`)
- `POST /session/new`
  - allocates a fresh server-side `student_id`
  - persists immediately so the session id survives restart

## Live WebSocket Protocol

Endpoint: `/ws/live`

Server validates envelope and payload shapes using protocol models in:

- `TuringBotAPI/protocol/live_v1.py`

Current behavior highlights:

- handshake acknowledgment
- protocol-version validation
- payload-kind validation
- periodic advisory nudge on `live.sim_step` every N steps (currently modulus-based)

This path is advisory-focused and intentionally lightweight.

## Student Model (Personalization Core)

File: `TuringBotAPI/student_model.py`

Storage model:

- in-memory dictionary keyed by `student_id` then `skill_id`
- value is `SkillState` (BKT probability + hint progression metadata)

Key behaviors:

- lazy initialization per unseen `(student_id, skill_id)`
- BKT update on each observation
- weakest-skill ranking for hint targeting
- hint-level escalation and reset logic

Persistence:

- file path from `STUDENT_STATE_PATH` env, default `student_state.json`
- load on startup, save on shutdown
- save also occurs after `/event`

## Tutoring Orchestration

File: `TuringBotAPI/orchestrator.py`

Builds model prompt context from:

- level metadata (`LEVEL_META`)
- concept map and mappings (`domain/concepts.py`)
- per-student knowledge state (`STUDENT_MODEL`)
- hint forest (`domain/hints.py`) for graduated hints

Outputs:

- ask response
- hint response (with selected `skill_id` and `hint_level`)
- event comment (selectively generated)

Provider boundary:

- `TuringBotAPI/tutor_provider.py` defines the small async provider contract.
- Gemini is constructed lazily and only when `GEMINI_API_KEY` is available.
- If Gemini is unavailable or a generation call fails, the API returns a
  deterministic factory-themed fallback rather than failing startup or
  dropping the tutor interaction.
- The fallback is intended for development/demo continuity; it is not a
  replacement for evaluating Gemini response quality.

## Logging and Observability

Files:

- `TuringBotAPI/logging_config.py`
- `TuringBotAPI/main.py`
- `TuringBotAPI/student_model.py`

Features:

- console logs for runtime diagnostics
- optional structured JSON-line logs via `AGENT_LOG_PATH`
- event-level metadata (student_id, level_id, skill_id, latency, event_type, kind)

## AI-Agent Safe Invariants (Server)

- `student_id` is the identity key for personalization.
- Any contract change in request/response payloads must be mirrored in Unity DTOs and serializers.
- Keep protocol version and kind constants synchronized with client protocol definitions.
- Maintain graceful handling for unknown/legacy skill IDs when loading persisted data.
- A live WebSocket must receive a valid handshake before telemetry; subsequent
  frames must use the same `session_id` and `student_id` as that handshake.

## Known Gaps

- Session history is retained by design; there is no deletion endpoint in current API.
- `/session/new` intentionally creates a fresh BKT identity. A future login/resume
  flow should be added if players must return to a named account.
- The live channel remains advisory-only; `/ask`, `/hint`, and reactive `/event`
  comments use the REST path.
- Provider tests currently verify fallback selection and session isolation; Gemini
  quality and quota behavior still require an environment with a real API key.
