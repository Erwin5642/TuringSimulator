# Server Architecture (TuringBotAPI, Current State)

This document describes the ITS server behavior as implemented today.

## Entry Point and Lifecycle

Main app file: `TuringBotAPI/main.py`

- FastAPI app with lifespan hooks:
  - startup: `setup_logging()`, build tutor provider, load markdown corpus into `KnowledgeStore`
  - no student-state file; sessions are UUID identities only
- CORS currently open (`allow_origins=["*"]`) for local development.
- Static web tester at `/web-tester/` (`TuringBotAPI/web-tester/`); `/` redirects there.

## REST API Surface

Unity's main demo line uses only these three endpoints.

- `POST /ask`
  - free-form question with `student_id`, `level_id`, `question`, and optional `tape` / `program` snapshots
  - agentic RAG: Gemini may call `search_docs` up to three times, plus `check_tape` and `check_program` at most once each (inspect calls do not count toward the three searches)
  - generate loop allows up to 5 function-call rounds so inspect does not starve retrieval
  - response is `{reply, tokens_in, tokens_out}`
  - Unity uses `reply` and synthesizes speech with Wit TTS; extra token fields are for the web tester and logs
  - `tokens_in` / `tokens_out` come from Gemini usage metadata when present; otherwise they are estimated from question and reply length (~4 characters per token)
  - `tape` is a compact window of the **visible** esteira `{cells, head_offset}` (blank padding around occupied cells, always including the braço). Before Começar the conveyor is empty, so the window may be only `vazio`. An all-blank esteira still spans index 0 through the current head so `head_offset` is the real braço position, not a collapsed 0. `program` is the authored block graph in factory names (`blocos`, `fios`), not the compiled transition table
  - snapshots are request-scoped; the server does not store student work
- `POST /session/new`
  - allocates a fresh `student_id` (`student_{uuid}`)
  - no BKT or per-student memory is stored
- `GET /health`
  - `{status, version, tutor_provider, documents}`
  - `tutor_provider` is `gemini` or `fallback`

Removed from this MVP: `POST /event`, `POST /hint`, `GET /state/{id}`, `GET /ws/live`.

## Agentic RAG

Files:

- `TuringBotAPI/knowledge/**/*.md` — reviewed corpus (persona, gameplay, objects, goals, concepts, errors)
- `TuringBotAPI/rag/documents.py` — frontmatter loader
- `TuringBotAPI/rag/store.py` — in-memory index + SQLite embedding cache
- `TuringBotAPI/agent.py` — `search_docs` / `check_tape` / `check_program` tools + answer loop
- `TuringBotAPI/bench.py` — compact tape window + program payload sanitizer
- `TuringBotAPI/tutor_provider.py` — Gemini or offline fallback

Index:

- All markdown files load at startup.
- Each file is one chunk with YAML frontmatter (`id`, `category`, `title`, `level_id`).
- Vectors stay in RAM. SQLite cache (`RAG_CACHE_PATH`, default `embeddings.sqlite`) stores embeddings by `doc_id` + content hash.
- `search_docs` does cosine search when embeddings exist; otherwise token overlap.
- Hits whose `level_id` matches the `/ask` level receive a score boost.

Agent:

- Persona document is always injected into the system prompt. It covers voice, routing (search vs refuse), identity, player vocab (esteira/execução do circuito, not fita/corrida/simulação; circuito = program of blocos + fios, bloco = instruction), and answer shape (short, no unsolicited briefing, no full circuit). Factory facts live in `knowledge/gameplay`, `objects`, `goals`, `concepts`, and `errors`. Failing-circuit questions should search category `errors`.
- Common greetings (`oi`, `bom dia`, `boa tarde`, …) still get a short in-character reply and skip retrieval.
- The current `level_id` is labeled as internal context; the model should not recap the objective unless the trainee asked about the task.
- Gemini function-calling, max 3 `search_docs` rounds plus one-shot `check_tape` / `check_program`, then a final pt-BR reply. `search_docs` is for how-to-play, factory objects, task questions, and program-design mistakes (`category` `errors`). Inspect tools are only for this trainee's visible esteira/circuito (`check_tape` may be only `vazio` before Começar) and are omitted from the user prompt unless the model calls them. The tutor must not name the hidden lot that failed validation.
- If Gemini is missing or fails, the server still searches and returns a short player-facing pt-BR reply: the radio-interference prefix plus trimmed sentences from the top chunks. Persona text, bullet lists, and agent-only notes (`Blocos deste nível`, `O que conta como feito`, voice rules) are omitted. Offline fallback does not run inspect tools.

Provider boundary:

- Gemini is constructed only when `GEMINI_API_KEY` is available.
- Chat model defaults to `gemini-2.5-flash`; embedding model defaults to `models/gemini-embedding-001`.
- Fallback is for development/demo continuity, not a substitute for Gemini quality.

## Logging and Observability

Files:

- `TuringBotAPI/logging_config.py`
- `TuringBotAPI/main.py`

Features:

- console logs for runtime diagnostics
- optional structured JSON-line logs via `AGENT_LOG_PATH`
- ask-level metadata (student_id, level_id, latency, tokens_in, tokens_out)

## AI-Agent Safe Invariants (Server)

- Unity `/ask` JSON stays `snake_case` with `student_id`, `level_id`, `question`, and optional `tape` / `program`.
- `/ask` always includes `reply`; Unity synthesizes tutor speech with Wit TTS and ignores extra fields.
- `/ask` also returns `tokens_in` and `tokens_out` for the web tester.
- Player-facing replies and fallbacks stay pt-BR.
- Knowledge edits happen in `TuringBotAPI/knowledge/`, not in Python skill tables.
- Keep `level_id` values aligned with Unity `LevelDefinition.levelId`.

## Deploy (Quave ONE)

Image: `TuringBotAPI/Dockerfile` (context `TuringBotAPI`). Custom Dockerfile preset.

- App port `3000`, HTTP probe `/health`
- Same process serves the Unity API and the web tester (`/web-tester/`)
- `GEMINI_API_KEY` is a runtime (Deploy) env var, not a build ARG
- Embedding cache is ephemeral at `/tmp/embeddings.sqlite`

## Known Gaps

- No per-student memory, hint escalation, or BKT.
- No live WebSocket advisory channel.
- Teleport with hands is the Spider-Man gesture with the pinky tucked (thumb + index); XR controller still uses the configured locomotion command. Confirm pads/controls against the shipped scene.
- Spoken clips are not generated on the server.
