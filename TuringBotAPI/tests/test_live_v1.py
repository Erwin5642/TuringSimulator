"""Tests for live_v1 inbound payload validation."""

import pytest
from pydantic import ValidationError

from protocol.live_v1 import (
    INBOUND_KIND_PAYLOAD,
    PROTOCOL_VERSION,
    build_advisory_nudge,
    validate_inbound_payload,
)


def test_inbound_kind_map_covers_handshake():
    assert "live.handshake" in INBOUND_KIND_PAYLOAD


def test_validate_handshake_ok():
    m = validate_inbound_payload(
        "live.handshake",
        {"client": "u", "client_version": "1", "supports_compression": False},
    )
    assert m.client == "u"


def test_validate_sim_step_rejects_bad_phase():
    with pytest.raises(ValidationError):
        validate_inbound_payload(
            "live.sim_step",
            {
                "phase": "bad",
                "step_index": 0,
                "previous_state": 0,
                "next_state": 1,
                "symbol_read": "0",
                "symbol_written": "_",
                "head_index_before": 0,
                "head_index_after": 1,
            },
        )


def test_unknown_kind_raises():
    with pytest.raises(ValueError, match="unknown_inbound_kind"):
        validate_inbound_payload("live.unknown", {})


def test_session_ping_empty_payload():
    m = validate_inbound_payload("live.session_ping", {})
    assert m is not None


def test_build_advisory_nudge_shape():
    d = build_advisory_nudge(
        session_id="sess",
        student_id="stu",
        level_id="ReplaceAllWithNuts",
        text="Pause each step.",
        correlation_id="corr-1",
    )
    assert d["protocol_version"] == PROTOCOL_VERSION
    assert d["kind"] == "live.advisory_nudge"
    assert d["session_id"] == "sess"
    assert d["payload"]["text"] == "Pause each step."
    assert d["correlation_id"] == "corr-1"
