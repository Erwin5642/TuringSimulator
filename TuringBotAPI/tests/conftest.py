"""Shared pytest fixtures for the RAG tutor."""

from __future__ import annotations

from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[1]
KNOWLEDGE_DIR = ROOT / "knowledge"


@pytest.fixture(autouse=True)
def _offline_env(monkeypatch, tmp_path):
    monkeypatch.setenv("GEMINI_API_KEY", "")
    monkeypatch.setenv("RAG_CACHE_PATH", str(tmp_path / "embeddings.sqlite"))
    monkeypatch.setenv("KNOWLEDGE_DIR", str(KNOWLEDGE_DIR))
