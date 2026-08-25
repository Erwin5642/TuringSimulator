"""Embedding helpers: Gemini vectors or None (caller falls back to keywords)."""

from __future__ import annotations

from typing import Optional, Protocol


class Embedder(Protocol):
    def embed_document(self, text: str) -> Optional[list[float]]:
        ...

    def embed_query(self, text: str) -> Optional[list[float]]:
        ...


class NullEmbedder:
    """Used when no embedding API is configured."""

    def embed_document(self, text: str) -> Optional[list[float]]:
        return None

    def embed_query(self, text: str) -> Optional[list[float]]:
        return None


def cosine_similarity(left: list[float], right: list[float]) -> float:
    if not left or not right or len(left) != len(right):
        return 0.0
    dot = 0.0
    norm_l = 0.0
    norm_r = 0.0
    for a, b in zip(left, right):
        dot += a * b
        norm_l += a * a
        norm_r += b * b
    if norm_l <= 0.0 or norm_r <= 0.0:
        return 0.0
    return dot / ((norm_l ** 0.5) * (norm_r ** 0.5))
