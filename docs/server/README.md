# Server Architecture (TuringBotAPI, Current State)

This document describes the ITS server behavior as implemented today.

## Entry Point and Lifecycle

Main app file: `TuringBotAPI/main.py`

- FastAPI app with lifespan hooks:
  - startup: `setup_logging()`, build tutor provider, load markdown corpus into `KnowledgeStore`
  - no student-state file; sessions are UUID identities only
- CORS currently open (`allow_origins=["*"]`) for local development.

## REST API Surface

Unity's main demo line uses only these three endpoints.

- `POST /ask`
  - free-form question with `student_id`, `level_id`, `question`
  - agentic RAG: Gemini may call `search_docs` up to three times, then answers
  - response is `{reply}`; Unity synthesizes speech with Wit TTS
- `POST /session/new`
  - allocates a fresh `student_id` (`student_{uuid}`)
  - no BKT or per-student memory is stored
- `GET /health`
  - `{status, version, tutor_provider, documents}`
  - `tutor_provider` is `gemini` or `fallback`

Removed from this MVP: `POST /event`, `POST /hint`, `GET /state/{id}`, `GET /ws/live`.

## Agentic RAG

Files:

- `TuringBotAPI/knowledge/**/*.md` — reviewed corpus (persona, gameplay, objects, goals, concepts)
- `TuringBotAPI/rag/documents.py` — frontmatter loader
- `TuringBotAPI/rag/store.py` — in-memory index + SQLite embedding cache
- `TuringBotAPI/agent.py` — `search_docs` tool + answer loop
- `TuringBotAPI/tutor_provider.py` — Gemini or offline fallback

Index:

- All markdown files load at startup.
- Each file is one chunk with YAML frontmatter (`id`, `category`, `title`, `level_id`).
- Vectors stay in RAM. SQLite cache (`RAG_CACHE_PATH`, default `embeddings.sqlite`) stores embeddings by `doc_id` + content hash.
- `search_docs` does cosine search when embeddings exist; otherwise token overlap.
- Hits whose `level_id` matches the `/ask` level receive a score boost.

Agent:

- Persona document is always injected into the system prompt.
- Gemini function-calling, max 3 `search_docs` rounds, then a final pt-BR reply.
- If Gemini is missing or fails, the server still searches and returns a deterministic pt-BR reply built from the top chunks.

Provider boundary:

- Gemini is constructed only when `GEMINI_API_KEY` is available.
- Embedding model defaults to `models/text-embedding-004`.
- Fallback is for development/demo continuity, not a substitute for Gemini quality.

## Logging and Observability

Files:

- `TuringBotAPI/logging_config.py`
- `TuringBotAPI/main.py`

Features:

- console logs for runtime diagnostics
- optional structured JSON-line logs via `AGENT_LOG_PATH`
- ask-level metadata (student_id, level_id, latency)

## AI-Agent Safe Invariants (Server)

- Unity `/ask` JSON stays `snake_case` with `student_id`, `level_id`, `question`.
- `/ask` returns `reply` only; Unity synthesizes tutor speech with Wit TTS.
- Player-facing replies and fallbacks stay pt-BR.
- Knowledge edits happen in `TuringBotAPI/knowledge/`, not in Python skill tables.
- Keep `level_id` values aligned with Unity `LevelDefinition.levelId`.

## Deploy (Quave ONE)

Image: `TuringBotAPI/Dockerfile` (context `TuringBotAPI`). Custom Dockerfile preset.

- App port `3000`, HTTP probe `/health`
- `GEMINI_API_KEY` is a runtime (Deploy) env var, not a build ARG
- Embedding cache is ephemeral at `/tmp/embeddings.sqlite`

## Known Gaps

- No per-student memory, hint escalation, or BKT.
- No live WebSocket advisory channel.
- Teleport copy is XR-locomotion generic; confirm against the shipped scene pads/controls.
- Spoken clips are not generated on the server.
