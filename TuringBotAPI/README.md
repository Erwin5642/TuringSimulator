# Turing Machine ITS — FastAPI Server (Agentic RAG)

## Setup

Use Python **3.11+** and a local virtual environment.

```bash
# From the repository root
cd TuringBotAPI

python3 -m venv .venv
source .venv/bin/activate        # Windows: .venv\Scripts\activate

pip install -U pip
pip install -r requirements-dev.txt

pytest tests/ -q

cp .env.example .env
# Edit .env: set GEMINI_API_KEY (https://aistudio.google.com/app/apikey)

uvicorn main:app --reload --port 8000
```

The interactive API docs are at **http://localhost:8000/docs**

---

## Deploy on Quave ONE

Use the **Custom Dockerfile** preset. This repo is a Unity monorepo; the API lives in `TuringBotAPI/`.

Dashboard settings:

- **Dockerfile path:** `TuringBotAPI/Dockerfile`
- **Context directory:** `TuringBotAPI`
- **Port:** `3000` (must match `PORT` in the image)
- **HTTP probes:** enable, path `/health`
- **Runtime env vars (Deploy, not Build):** `GEMINI_API_KEY`, optional `GEMINI_MODEL`, `AGENT_NAME`

Do not bake the Gemini key into the image. Embedding cache writes to `/tmp/embeddings.sqlite` (ephemeral; rebuilt on restart).

Local image check:

```bash
cd TuringBotAPI
docker build -t turing-bot-api .
docker run --rm -p 3000:3000 -e GEMINI_API_KEY turing-bot-api
```

The server can start without `GEMINI_API_KEY`. In that case `/health` reports
`"tutor_provider": "fallback"` and `/ask` answers from keyword search over the
markdown corpus so the Unity scene remains testable.

---

## Endpoints

Unity's main demo line uses `/session/new`, `/ask`, and `/health`.

### `POST /session/new`

Allocates a fresh `student_id`. No knowledge state is stored for that id.

### `POST /ask`

Student asks the tutor a free-form question (voice STT in Unity).

```json
{
  "student_id": "student_42",
  "level_id": "MoveLeftRight",
  "question": "Como eu falo com você?"
}
```

**Response:**
```json
{
  "reply": "Segura o Shaka na mão direita para falar comigo, trainee."
}
```

Unity synthesizes tutor speech with Wit.ai TTS.

The tutor may call `search_docs` up to three times against `knowledge/**/*.md`
before answering.

### `GET /health`

```json
{
  "status": "ok",
  "version": "0.2.0",
  "tutor_provider": "gemini",
  "documents": 28
}
```

---

## Knowledge corpus

Markdown files in `knowledge/` (one topic per file, YAML frontmatter):

```
knowledge/
├── persona/     tutor name, job, voice rules
├── gameplay/    how to play, teleport, speak, grab, run, thumbs-up
├── objects/     workbench, drawers, blocks, cards, tape, buttons
├── goals/       eight playable levels + validation
└── concepts/    factory ↔ TM metaphor, loops, common mistakes
```

Frontmatter fields: `id`, `category`, `title`, `level_id` (empty or a Unity level id).

---

## File structure

```
TuringBotAPI/
├── main.py              FastAPI app (`/ask`, `/session/new`, `/health`)
├── agent.py             search_docs tool + answer loop
├── tutor_provider.py    Gemini / fallback provider
├── rag/                 markdown loader, SQLite cache, in-memory search
├── knowledge/           RAG corpus (pt-BR)
├── Dockerfile           Quave ONE / container image
├── .dockerignore
├── requirements.txt
├── .env.example
└── tests/
```

Embeddings cache file `embeddings.sqlite` is created at runtime and gitignored.
