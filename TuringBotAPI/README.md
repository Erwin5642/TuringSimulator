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
# Defaults: GEMINI_MODEL=gemini-2.5-flash
#           GEMINI_EMBED_MODEL=models/gemini-embedding-001

uvicorn main:app --reload --port 8000
```

The interactive API docs are at **http://localhost:8000/docs**. The web tester is
served by the same process at **http://localhost:8000/web-tester/**.

---

## Web tester (contrato da Unity)

Há uma interface web simples em `web-tester/index.html` para testar no navegador
o mesmo fluxo do cliente Unity:

- `GET /health`
- `POST /session/new` (com fallback local de `student_id`)
- `POST /ask` com `student_id`, `level_id`, `question`

O FastAPI serve essa página em `/web-tester/` (e `/` redireciona para lá). Com
uvicorn local, acesse:

- [http://localhost:8000/web-tester/](http://localhost:8000/web-tester/)
  
A Base URL da API é preenchida com a origem atual. Se você abrir o HTML por um
servidor estático na porta `8080` (`python3 -m http.server 8080`), o tester
aponta para `http://localhost:8000`.

---

## Deploy on Quave ONE

Use the **Custom Dockerfile** preset. This repo is a Unity monorepo; the API lives in `TuringBotAPI/`.

Dashboard settings:

- **Dockerfile path:** `TuringBotAPI/Dockerfile`
- **Context directory:** `TuringBotAPI`
- **Port:** `3000` (must match `PORT` in the image)
- **HTTP probes:** enable, path `/health`
- **Runtime env vars (Deploy, not Build):** `GEMINI_API_KEY`, optional `GEMINI_MODEL` (`gemini-2.5-flash`), `GEMINI_EMBED_MODEL` (`models/gemini-embedding-001`), `AGENT_NAME`

Do not bake the Gemini key into the image. Embedding cache writes to `/tmp/embeddings.sqlite` (ephemeral; rebuilt on restart).

Local image check:

```bash
cd TuringBotAPI
docker build -t turing-bot-api .
docker run --rm -p 3000:3000 -e GEMINI_API_KEY turing-bot-api
```

The image starts both the Unity API and the web tester on `PORT` (default 3000):

- API docs: http://localhost:3000/docs
- Web tester: http://localhost:3000/web-tester/

The server can start without `GEMINI_API_KEY`. In that case `/health` reports
`"tutor_provider": "fallback"` and `/ask` answers with a short player-facing
pt-BR clipboard (not the raw knowledge dump) so the Unity scene remains testable.

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
  "reply": "Tá tranquilo, tá favorável na mão direita para falar comigo, trainee.",
  "tokens_in": 1842,
  "tokens_out": 28
}
```

Unity uses `reply` and synthesizes tutor speech with Wit.ai TTS. `tokens_in` /
`tokens_out` are for the web tester and logs (Gemini usage when available,
otherwise an estimate from question and reply length).

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
├── Dockerfile           Quave ONE / container image (API + web tester)
├── web-tester/          browser tester served at `/web-tester/`
├── .dockerignore
├── requirements.txt
├── .env.example
└── tests/
```

Embeddings cache file `embeddings.sqlite` is created at runtime and gitignored.
