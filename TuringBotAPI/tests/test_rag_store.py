from pathlib import Path

import pytest

from rag.documents import load_knowledge_dir, parse_frontmatter_markdown
from rag.store import KnowledgeStore

ROOT = Path(__file__).resolve().parents[1]
KNOWLEDGE_DIR = ROOT / "knowledge"


def test_corpus_loads_with_unique_ids():
    docs = load_knowledge_dir(KNOWLEDGE_DIR)
    ids = [doc.id for doc in docs]
    assert len(ids) == len(set(ids))
    assert len(docs) >= 20
    categories = {doc.category for doc in docs}
    assert categories == {"persona", "gameplay", "objects", "goals", "concepts"}


def test_parse_rejects_unknown_category():
    raw = """---
id: x
category: other
title: t
level_id:
---
body
"""
    with pytest.raises(ValueError, match="Unknown category"):
        parse_frontmatter_markdown(raw, "x.md")


def test_keyword_search_finds_speak_and_condition():
    store = KnowledgeStore.from_directory(KNOWLEDGE_DIR)
    speak = store.search("shaka falar microfone")
    assert speak
    assert speak[0].document.id == "gameplay-speak"

    condition = store.search("bloco de condição inspetor")
    assert any(hit.document.id == "objects-condition-block" for hit in condition)


def test_level_boost_prefers_current_goal():
    store = KnowledgeStore.from_directory(KNOWLEDGE_DIR)
    hits = store.search(
        "objetivo do nível",
        category="goals",
        level_id="PlaceGear",
        top_k=3,
    )
    assert hits
    assert hits[0].document.id == "goals-place-gear"
    assert hits[0].document.level_id == "PlaceGear"


def test_category_filter_hides_other_topics():
    store = KnowledgeStore.from_directory(KNOWLEDGE_DIR)
    hits = store.search("shaka", category="objects")
    assert all(hit.document.category == "objects" for hit in hits)
    assert all(hit.document.id != "gameplay-speak" for hit in hits)


def test_sqlite_cache_skips_reembed(tmp_path):
    class CountingEmbedder:
        def __init__(self) -> None:
            self.calls = 0

        def embed_document(self, text: str) -> list[float]:
            self.calls += 1
            return [float(len(text) % 7 + 1), 1.0, 0.25]

        def embed_query(self, text: str) -> list[float]:
            return [1.0, 1.0, 0.25]

    cache = tmp_path / "cache.sqlite"
    docs = load_knowledge_dir(KNOWLEDGE_DIR)
    first = CountingEmbedder()
    KnowledgeStore(docs, embedder=first, cache_path=cache)
    assert first.calls == len(docs)

    second = CountingEmbedder()
    KnowledgeStore(docs, embedder=second, cache_path=cache)
    assert second.calls == 0


def test_vector_search_ranks_matching_doc(tmp_path):
    class AxisEmbedder:
        def embed_document(self, text: str) -> list[float]:
            if "mindinho" in text.lower():
                return [1.0, 0.0]
            return [0.0, 1.0]

        def embed_query(self, text: str) -> list[float]:
            return [1.0, 0.0]

    store = KnowledgeStore.from_directory(
        KNOWLEDGE_DIR,
        embedder=AxisEmbedder(),
        cache_path=tmp_path / "vec.sqlite",
    )
    hits = store.search("como falar com o tutor")
    assert hits[0].document.id == "gameplay-speak"
