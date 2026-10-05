"""Deterministic rubric scoring for tutor replies."""

from __future__ import annotations

import re
import unicodedata
from dataclasses import dataclass, field
from typing import Any

_SENTENCE_RE = re.compile(r"[.!?]+")
REFUSE_HINTS = (
    "nao tenho conhecimento",
    "so da fabrica",
    "radio da fabrica",
)


@dataclass(frozen=True)
class ScoreResult:
    passed: bool
    failures: list[str] = field(default_factory=list)


def normalize(text: str) -> str:
    stripped = unicodedata.normalize("NFKD", text or "")
    without_marks = "".join(
        char for char in stripped if not unicodedata.combining(char)
    )
    return " ".join(without_marks.lower().split())


def term_in(haystack: str, term: str) -> bool:
    needle = normalize(term)
    if not needle:
        return False
    if needle in haystack:
        return True
    if needle + "s" in haystack:
        return True
    if needle.endswith("m") and needle[:-1] + "ns" in haystack:
        return True
    if needle.endswith("ao") and needle[:-2] + "oes" in haystack:
        return True
    return False


def count_sentences(text: str) -> int:
    parts = [part.strip() for part in _SENTENCE_RE.split((text or "").strip())]
    return len([part for part in parts if part])


def score_reply(
    reply: str,
    item: dict[str, Any],
    global_checks: dict[str, Any] | None = None,
) -> ScoreResult:
    haystack = normalize(reply)
    failures: list[str] = []
    if not haystack:
        return ScoreResult(False, ["empty_reply"])

    merged = dict(global_checks or {})
    item_checks = item.get("checks") or {}
    for key, value in item_checks.items():
        if key in {"contain_all", "contain_any", "forbid"}:
            merged[key] = list(merged.get(key) or []) + list(value)
        elif key == "contain_groups":
            merged[key] = list(merged.get(key) or []) + list(value)
        else:
            merged[key] = value

    _score_contain_all(haystack, merged.get("contain_all"), failures)
    _score_contain_any(haystack, merged.get("contain_any"), failures)
    _score_contain_groups(haystack, merged.get("contain_groups"), failures)
    _score_forbid(haystack, merged.get("forbid"), failures)

    if merged.get("expect_refuse"):
        if not any(normalize(hint) in haystack for hint in REFUSE_HINTS):
            failures.append("expect_refuse")

    max_sentences = merged.get("max_sentences")
    if isinstance(max_sentences, int):
        actual = count_sentences(reply)
        if actual > max_sentences:
            failures.append(f"max_sentences:{actual}>{max_sentences}")

    return ScoreResult(not failures, failures)


def _score_contain_all(
    haystack: str, terms: list[str] | None, failures: list[str]
) -> None:
    for term in terms or []:
        if not term_in(haystack, term):
            failures.append(f"contain_all:{term}")


def _score_contain_any(
    haystack: str, terms: list[str] | None, failures: list[str]
) -> None:
    if not terms:
        return
    if not any(term_in(haystack, term) for term in terms):
        failures.append("contain_any:" + "|".join(terms))


def _score_contain_groups(
    haystack: str, groups: list[list[str]] | None, failures: list[str]
) -> None:
    for group in groups or []:
        if not any(term_in(haystack, term) for term in group):
            failures.append("contain_group:" + "|".join(group))


def _score_forbid(
    haystack: str, terms: list[str] | None, failures: list[str]
) -> None:
    for term in terms or []:
        if term_in(haystack, term):
            failures.append(f"forbid:{term}")
