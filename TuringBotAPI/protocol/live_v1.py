"""
WebSocket live protocol v1 — envelope + normative kind strings (advisory-only downstream).

Wire format: one JSON object per text frame (UTF-8).
"""

from __future__ import annotations

from typing import Any, Literal, Optional

from pydantic import BaseModel, Field

PROTOCOL_VERSION = "1.0.0"

# --- Envelope -----------------------------------------------------------------

class LiveEnvelope(BaseModel):
    protocol_version: str
    message_id: str
    correlation_id: Optional[str] = None
    sent_at_unix_ms: int
    session_id: str
    student_id: str
    level_id: str
    kind: str
    payload: dict[str, Any] = Field(default_factory=dict)


# --- Client payloads (subset used in MVP) -------------------------------------

class HandshakePayload(BaseModel):
    client: str
    client_version: str
    supports_compression: bool = False


class HandshakeAckPayload(BaseModel):
    server: str
    accepted_protocol_version: str
    capabilities: list[str] = Field(default_factory=list)


class LevelSnapshotPayload(BaseModel):
    title: Optional[str] = None
    description: Optional[str] = None
    tape_symbols: list[str] = Field(default_factory=list)
    head_index: int = 0
    program_summary: Optional[dict[str, Any]] = None


class RunLifecyclePayload(BaseModel):
    phase: Literal[
        "run_start",
        "run_abort",
        "run_finished",
        "playback_pause",
        "playback_resume",
        "step_forward",
        "step_backward",
    ]


class SimStepPayload(BaseModel):
    phase: Literal["computed", "played"]
    step_index: int
    previous_state: int
    next_state: int
    symbol_read: str
    symbol_written: str
    head_index_before: int
    head_index_after: int
    game_flow_state: Optional[str] = None


class SimHaltPayload(BaseModel):
    halt_status: str


class SessionPingPayload(BaseModel):
    """Heartbeat / keep-alive; {} is valid."""

    model_config = {"extra": "ignore"}


INBOUND_KIND_PAYLOAD: dict[str, type[BaseModel]] = {
    "live.handshake": HandshakePayload,
    "live.run_lifecycle": RunLifecyclePayload,
    "live.level_snapshot": LevelSnapshotPayload,
    "live.sim_step": SimStepPayload,
    "live.sim_halt": SimHaltPayload,
    "live.session_ping": SessionPingPayload,
}


def validate_inbound_payload(kind: str, payload: dict[str, Any]) -> BaseModel:
    """
    Validate env.payload for WebSocket messages sent by the Unity client.
    Raises ValidationError (Pydantic) if fields are invalid.
    Raises ValueError if kind is not a known inbound kind.
    """
    model_cls = INBOUND_KIND_PAYLOAD.get(kind)
    if model_cls is None:
        raise ValueError(f"unknown_inbound_kind:{kind}")
    return model_cls.model_validate(payload)


class AdvisoryHintPayload(BaseModel):
    text: str
    skill_id: Optional[str] = None
    hint_level: Optional[int] = None
    urgency: Optional[Literal["low", "medium", "high"]] = None


class AdvisoryWarningPayload(BaseModel):
    text: str
    skill_id: Optional[str] = None
    urgency: Optional[Literal["low", "medium", "high"]] = None


class AdvisoryNudgePayload(BaseModel):
    text: str


class LiveErrorPayload(BaseModel):
    code: str
    message: str
    details: Optional[dict[str, Any]] = None


def validate_envelope(data: dict[str, Any]) -> LiveEnvelope:
    return LiveEnvelope.model_validate(data)


def build_handshake_ack(message_id: str, session_id: str, student_id: str, level_id: str) -> dict[str, Any]:
    import uuid
    import time

    env = LiveEnvelope(
        protocol_version=PROTOCOL_VERSION,
        message_id=str(uuid.uuid4()),
        correlation_id=message_id,
        sent_at_unix_ms=int(time.time() * 1000),
        session_id=session_id,
        student_id=student_id,
        level_id=level_id,
        kind="live.handshake_ack",
        payload=HandshakeAckPayload(
            server="turing-bot-api",
            accepted_protocol_version=PROTOCOL_VERSION,
            capabilities=["bkt_stream", "gemini_advisory"],
        ).model_dump(),
    )
    return env.model_dump()


def build_advisory_nudge(
    *,
    session_id: str,
    student_id: str,
    level_id: str,
    text: str,
    correlation_id: Optional[str] = None,
) -> dict[str, Any]:
    """Lightweight advisory-only message for long runs (throttle client-side volume)."""
    import uuid
    import time

    return LiveEnvelope(
        protocol_version=PROTOCOL_VERSION,
        message_id=str(uuid.uuid4()),
        correlation_id=correlation_id,
        sent_at_unix_ms=int(time.time() * 1000),
        session_id=session_id,
        student_id=student_id,
        level_id=level_id,
        kind="live.advisory_nudge",
        payload=AdvisoryNudgePayload(text=text).model_dump(),
    ).model_dump()


def build_error(
    *,
    session_id: str,
    student_id: str,
    level_id: str,
    code: str,
    message: str,
    correlation_id: Optional[str] = None,
) -> dict[str, Any]:
    import uuid
    import time

    return LiveEnvelope(
        protocol_version=PROTOCOL_VERSION,
        message_id=str(uuid.uuid4()),
        correlation_id=correlation_id,
        sent_at_unix_ms=int(time.time() * 1000),
        session_id=session_id,
        student_id=student_id,
        level_id=level_id,
        kind="live.error",
        payload=LiveErrorPayload(code=code, message=message).model_dump(),
    ).model_dump()
