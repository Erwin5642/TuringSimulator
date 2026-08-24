"""
FastAPI server for the Turing Machine factory tutor (agentic RAG).

Unity contract (unchanged):
    POST /ask
    POST /session/new
    GET  /health
"""

from __future__ import annotations

import logging
import os
import time
import uuid
from contextlib import asynccontextmanager
from pathlib import Path
from typing import Optional

from dotenv import load_dotenv
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel

load_dotenv()

from agent import SEARCH_DOCS_TOOL, answer_question
from logging_config import setup_logging
from rag.store import KnowledgeStore
from tutor_provider import build_tutor_provider

_LOG = logging.getLogger("api")
_ROOT = Path(__file__).resolve().parent


def _knowledge_dir() -> Path:
    return Path(os.getenv("KNOWLEDGE_DIR", _ROOT / "knowledge"))


def _cache_path() -> Path:
    return Path(os.getenv("RAG_CACHE_PATH", _ROOT / "embeddings.sqlite"))


def _agent_name() -> str:
    return os.getenv("AGENT_NAME", "MarquinhosDoGrau")


@asynccontextmanager
async def lifespan(app: FastAPI):
    setup_logging()
    provider = build_tutor_provider(tools=[SEARCH_DOCS_TOOL])
    store = KnowledgeStore.from_directory(
        _knowledge_dir(),
        embedder=provider,
        cache_path=_cache_path(),
    )
    app.state.provider = provider
    app.state.store = store
    _LOG.info(
        "startup provider=%s docs=%d",
        provider.name,
        len(store.documents),
    )
    yield


app = FastAPI(
    title="Turing Machine ITS",
    description="Agentic RAG tutor for the TM factory game.",
    version="0.2.0",
    lifespan=lifespan,
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)


class AskRequest(BaseModel):
    student_id: str
    level_id: str
    question: str


class AskResponse(BaseModel):
    reply: str
    audio_url: Optional[str] = None


class SessionNewResponse(BaseModel):
    student_id: str


@app.post("/ask", response_model=AskResponse)
async def handle_ask(req: AskRequest) -> AskResponse:
    if not req.question.strip():
        raise HTTPException(status_code=422, detail="Question cannot be empty.")

    t0 = time.perf_counter()
    reply = await answer_question(
        store=app.state.store,
        provider=app.state.provider,
        level_id=req.level_id,
        question=req.question,
        agent_name=_agent_name(),
    )
    ms = (time.perf_counter() - t0) * 1000.0
    _LOG.info(
        "rest_ask_done latency_ms=%.2f",
        ms,
        extra={
            "latency_ms": round(ms, 2),
            "student_id": req.student_id,
            "level_id": req.level_id,
        },
    )
    return AskResponse(reply=reply, audio_url=None)


@app.post("/session/new", response_model=SessionNewResponse)
async def new_session() -> SessionNewResponse:
    student_id = f"student_{uuid.uuid4().hex}"
    _LOG.info("rest_session_new", extra={"student_id": student_id, "source": "session_new"})
    return SessionNewResponse(student_id=student_id)


@app.get("/health")
async def health() -> dict:
    provider_name = "fallback"
    store = getattr(app.state, "store", None)
    provider = getattr(app.state, "provider", None)
    if provider is not None:
        provider_name = provider.name
    return {
        "status": "ok",
        "version": app.version,
        "tutor_provider": provider_name,
        "documents": len(store.documents) if store is not None else 0,
    }
