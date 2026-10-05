"""Minimum gap between remote tutor requests."""

from __future__ import annotations

import logging
import time
from typing import Protocol

_LOG = logging.getLogger("request_pacer")


class IRequestPacer(Protocol):
    def wait(self) -> None:
        """Block until another remote request is allowed."""


class RequestPacer:
    def __init__(self, interval_s: float) -> None:
        if interval_s < 0:
            raise ValueError("request interval must be >= 0.")
        self._interval_s = interval_s
        self._last_at: float | None = None

    def wait(self) -> None:
        if self._interval_s <= 0:
            return
        now = time.monotonic()
        if self._last_at is not None:
            remaining = self._interval_s - (now - self._last_at)
            if remaining > 0:
                _LOG.info("request_pace sleep_s=%.2f", remaining)
                time.sleep(remaining)
        self._last_at = time.monotonic()
