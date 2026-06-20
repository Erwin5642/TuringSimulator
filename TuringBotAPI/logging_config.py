"""
Structured logging for research / diagnostics (server-side only).

Set AGENT_LOG_PATH to append JSON-lines records. Console keeps human-readable output.
"""

from __future__ import annotations

import json
import logging
import os
import sys
from datetime import datetime, timezone

_AGENT_LOG_PATH = os.getenv("AGENT_LOG_PATH", "").strip()


class _JsonLineFormatter(logging.Formatter):
    def format(self, record: logging.LogRecord) -> str:
        line = {
            "ts": datetime.now(timezone.utc).isoformat(),
            "level": record.levelname,
            "logger": record.name,
            "message": record.getMessage(),
        }
        # Optional structured keys passed via logger.info(..., extra={"student_id": ...})
        for key in (
            "student_id",
            "level_id",
            "session_id",
            "source",
            "skill_id",
            "latency_ms",
            "event_type",
            "kind",
        ):
            if hasattr(record, key):
                line[key] = getattr(record, key)
        return json.dumps(line, ensure_ascii=False)


def setup_logging() -> None:
    root = logging.getLogger()
    root.setLevel(logging.INFO)

    if not any(type(h) is logging.StreamHandler for h in root.handlers):
        sh = logging.StreamHandler(sys.stdout)
        sh.setFormatter(logging.Formatter("%(levelname)s %(name)s %(message)s"))
        root.addHandler(sh)

    if _AGENT_LOG_PATH:
        fh = logging.FileHandler(_AGENT_LOG_PATH, encoding="utf-8")
        fh.setFormatter(_JsonLineFormatter())
        fh.setLevel(logging.INFO)
        root.addHandler(fh)
