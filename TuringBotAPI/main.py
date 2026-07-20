"""
main.py

FastAPI server for the Turing Machine ITS.

Endpoints
---------
POST /event   — Unity sends a game action; BKT is updated; optional
                reactive comment is returned.
POST /ask     — Student asks a free-form question; Gemini answers.
POST /hint    — Student requests a hint; orchestrator selects and
                escalates; Gemini expands into natural speech.
GET  /state/{student_id}
              — Debug: inspect the student's current knowledge state.

Run with:
    uvicorn main:app --reload --port 8000

Unity calls:
    http://localhost:8000/event
    http://localhost:8000/ask
    http://localhost:8000/hint
"""

from __future__ import annotations

import json
import logging
import os
import time
from contextlib import asynccontextmanager
from typing import Optional

from dotenv import load_dotenv
from fastapi import FastAPI, HTTPException, WebSocket, WebSocketDisconnect
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, Field, ValidationError

load_dotenv()

from logging_config import setup_logging
from student_model import STUDENT_MODEL
from orchestrator import (
    generate_ask_response,
    generate_hint_response,
    generate_event_comment,
    tutor_provider_name,
)
from domain.concepts import CONCEPT_MAP
from protocol.live_v1 import (
    PROTOCOL_VERSION,
    build_advisory_nudge,
    build_error,
    build_handshake_ack,
    validate_envelope,
    validate_inbound_payload,
)

_LOG = logging.getLogger("api")

# ── Lifespan: load persisted state on startup, save on shutdown ─────────────

@asynccontextmanager
async def lifespan(app: FastAPI):
    setup_logging()
    STUDENT_MODEL.load()
    yield
    STUDENT_MODEL.save()

app = FastAPI(
    title="Turing Machine ITS",
    description="Intelligent Tutoring System API for the TM factory game.",
    version="0.1.0",
    lifespan=lifespan,
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],   # tighten in production
    allow_methods=["*"],
    allow_headers=["*"],
)


# ── Request / Response models ────────────────────────────────────────────────

class EventRequest(BaseModel):
    """
    Sent by Unity whenever the student performs a meaningful game action.

    student_id  : Unique identifier for the student session.
    level_id    : Current level (e.g. "ReplaceAllWithNuts").
    event_type  : One of: block_placed, block_removed, wire_connected,
                  wire_removed, program_run, level_complete, level_failed.
    correct     : Whether the action moved the student toward the solution.
    skill_ids   : BKT skills this action provides evidence for.
                  Unity decides which skills an action exercises based on
                  the domain model.
    details     : Optional free-form dict for richer game state (e.g.
                  {"block": "condition", "port": "true", "target": "reject"}).
    """
    student_id : str
    level_id   : str
    event_type : str
    correct    : bool
    skill_ids  : list[str] = Field(default_factory=list)
    details    : dict      = Field(default_factory=dict)


class EventResponse(BaseModel):
    updated_skills : dict[str, float]   # { skill_id: new_p_know }
    comment        : Optional[str]      # agent comment, or null


class AskRequest(BaseModel):
    student_id : str
    level_id   : str
    question   : str


class AskResponse(BaseModel):
    reply: str


class HintRequest(BaseModel):
    student_id : str
    level_id   : str
    skill_id   : Optional[str] = None  # if None, server selects weakest skill


class HintResponse(BaseModel):
    reply      : str
    skill_id   : Optional[str]
    hint_level : int   # 1=Socratic 2=Conceptual 3=Partial 4=Direct


class KnowledgeStateResponse(BaseModel):
    student_id     : str
    knowledge_state: dict[str, float]
    mastered       : list[str]


class SessionNewResponse(BaseModel):
    student_id: str


# ── Endpoints ────────────────────────────────────────────────────────────────

@app.post("/event", response_model=EventResponse)
async def handle_event(req: EventRequest) -> EventResponse:
    """
    Receive a game event from Unity, update BKT, return optional comment.
    """
    t0 = time.perf_counter()
    _LOG.info(
        "rest_event type=%s skills=%d correct=%s",
        req.event_type,
        len(req.skill_ids),
        req.correct,
        extra={
            "student_id": req.student_id,
            "level_id": req.level_id,
            "event_type": req.event_type,
        },
    )
    updated: dict[str, float] = {}

    for skill_id in req.skill_ids:
        try:
            CONCEPT_MAP.get_skill(skill_id)   # validate
        except KeyError:
            raise HTTPException(
                status_code=422,
                detail=f"Unknown skill_id '{skill_id}'.",
            )
        new_p = STUDENT_MODEL.observe(req.student_id, skill_id, req.correct)
        updated[skill_id] = round(new_p, 4)

    comment = await generate_event_comment(
        student_id = req.student_id,
        level_id   = req.level_id,
        event_type = req.event_type,
        correct    = req.correct,
        skill_ids  = req.skill_ids,
    )

    # Persist after every event so no state is lost on crash
    STUDENT_MODEL.save()

    ms = (time.perf_counter() - t0) * 1000.0
    _LOG.info(
        "rest_event_done latency_ms=%.2f",
        ms,
        extra={"latency_ms": round(ms, 2), "student_id": req.student_id, "level_id": req.level_id},
    )

    return EventResponse(updated_skills=updated, comment=comment)


@app.post("/ask", response_model=AskResponse)
async def handle_ask(req: AskRequest) -> AskResponse:
    """
    Answer a free-form student question in the agent's voice.
    """
    if not req.question.strip():
        raise HTTPException(status_code=422, detail="Question cannot be empty.")

    t0 = time.perf_counter()
    reply = await generate_ask_response(
        student_id = req.student_id,
        level_id   = req.level_id,
        question   = req.question,
    )
    ms = (time.perf_counter() - t0) * 1000.0
    _LOG.info(
        "rest_ask_done latency_ms=%.2f",
        ms,
        extra={"latency_ms": round(ms, 2), "student_id": req.student_id, "level_id": req.level_id},
    )
    return AskResponse(reply=reply)


@app.post("/hint", response_model=HintResponse)
async def handle_hint(req: HintRequest) -> HintResponse:
    """
    Return a graduated hint for the student's current struggle.
    Hint level auto-escalates after repeated requests.
    """
    t0 = time.perf_counter()
    result = await generate_hint_response(
        student_id = req.student_id,
        level_id   = req.level_id,
        skill_id   = req.skill_id,
    )
    ms = (time.perf_counter() - t0) * 1000.0
    _LOG.info(
        "rest_hint_done latency_ms=%.2f",
        ms,
        extra={"latency_ms": round(ms, 2), "student_id": req.student_id, "level_id": req.level_id},
    )
    return HintResponse(**result)


@app.get("/state/{student_id}", response_model=KnowledgeStateResponse)
async def get_state(student_id: str) -> KnowledgeStateResponse:
    """
    Debug endpoint — inspect a student's current BKT knowledge state.
    """
    state    = STUDENT_MODEL.knowledge_state(student_id)
    mastered = [
        sid for sid in state
        if STUDENT_MODEL.is_mastered(student_id, sid)
    ]
    return KnowledgeStateResponse(
        student_id      = student_id,
        knowledge_state = {k: round(v, 4) for k, v in state.items()},
        mastered        = mastered,
    )


@app.get("/health")
async def health() -> dict:
    return {
        "status": "ok",
        "version": app.version,
        "tutor_provider": tutor_provider_name(),
    }


@app.post("/session/new", response_model=SessionNewResponse)
async def new_session() -> SessionNewResponse:
    """
    Allocate a fresh student session identity.
    """
    student_id = STUDENT_MODEL.create_new_student()
    STUDENT_MODEL.save()
    _LOG.info(
        "rest_session_new",
        extra={"student_id": student_id, "source": "session_new"},
    )
    return SessionNewResponse(student_id=student_id)


@app.websocket("/ws/live")
async def ws_live(websocket: WebSocket) -> None:
    await websocket.accept()
    log = logging.getLogger("live_ws")
    log.info("ws_accept")
    active_session_id: Optional[str] = None
    active_student_id: Optional[str] = None
    try:
        while True:
            raw = await websocket.receive_text()
            t0 = time.perf_counter()
            try:
                data = json.loads(raw)
            except json.JSONDecodeError:
                await websocket.send_json(
                    build_error(
                        session_id="",
                        student_id="",
                        level_id="",
                        code="invalid_payload",
                        message="invalid_json",
                    )
                )
                continue
            try:
                env = validate_envelope(data)
            except ValidationError as e:
                await websocket.send_json(
                    build_error(
                        session_id=str(data.get("session_id", "")),
                        student_id=str(data.get("student_id", "")),
                        level_id=str(data.get("level_id", "")),
                        code="invalid_payload",
                        message=str(e),
                        correlation_id=str(data.get("message_id")) if data.get("message_id") else None,
                    )
                )
                continue

            log.info(
                "ws_inbound",
                extra={
                    "kind": env.kind,
                    "session_id": env.session_id,
                    "student_id": env.student_id,
                    "level_id": env.level_id,
                    "source": "live_stream",
                },
            )

            if env.protocol_version != PROTOCOL_VERSION:
                await websocket.send_json(
                    build_error(
                        session_id=env.session_id,
                        student_id=env.student_id,
                        level_id=env.level_id,
                        code="unsupported_protocol_version",
                        message=env.protocol_version,
                        correlation_id=env.message_id,
                    )
                )
                continue

            try:
                validated = validate_inbound_payload(env.kind, env.payload)
            except ValidationError as e:
                await websocket.send_json(
                    build_error(
                        session_id=env.session_id,
                        student_id=env.student_id,
                        level_id=env.level_id,
                        code="invalid_payload_body",
                        message=str(e),
                        correlation_id=env.message_id,
                    )
                )
                continue
            except ValueError as e:
                await websocket.send_json(
                    build_error(
                        session_id=env.session_id,
                        student_id=env.student_id,
                        level_id=env.level_id,
                        code="unknown_inbound_kind",
                        message=str(e),
                        correlation_id=env.message_id,
                    )
                )
                continue

            if env.kind == "live.handshake":
                active_session_id = env.session_id
                active_student_id = env.student_id
                await websocket.send_json(
                    build_handshake_ack(
                        env.message_id,
                        env.session_id,
                        env.student_id,
                        env.level_id,
                    )
                )
            elif (
                active_session_id is None
                or env.session_id != active_session_id
                or env.student_id != active_student_id
            ):
                await websocket.send_json(
                    build_error(
                        session_id=env.session_id,
                        student_id=env.student_id,
                        level_id=env.level_id,
                        code="session_mismatch",
                        message="send a new handshake before sending live telemetry",
                        correlation_id=env.message_id,
                    )
                )
            elif env.kind == "live.session_ping":
                pass
            elif env.kind == "live.sim_step":
                # Advisory-only MVP: periodic neutral nudge while stepping (throttled by modulus).
                step_index = validated.step_index
                if step_index > 0 and step_index % 15 == 0:
                    await websocket.send_json(
                        build_advisory_nudge(
                            session_id=env.session_id,
                            student_id=env.student_id,
                            level_id=env.level_id,
                            text=(
                                "Still tracing the tape — pause at each step and "
                                "confirm which symbol is under the head before branching."
                            ),
                            correlation_id=env.message_id,
                        )
                    )

            ms = (time.perf_counter() - t0) * 1000.0
            log.info(
                "ws_handled",
                extra={
                    "latency_ms": round(ms, 2),
                    "kind": env.kind,
                    "session_id": env.session_id,
                },
            )
    except WebSocketDisconnect:
        log.info("ws_disconnect")