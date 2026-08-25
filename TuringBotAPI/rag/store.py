"""In-memory knowledge index with an optional SQLite embedding cache."""

from __future__ import annotations

import hashlib
import json
import logging
import re
import sqlite3
from dataclasses import dataclass
from pathlib import Path
from typing import Optional

from rag.documents import KnowledgeDocument, load_knowledge_dir
from rag.embeddings import Embedder, NullEmbedder, cosine_similarity

_LOG = logging.getLogger("rag.store")

_TOKEN_RE = re.compile(r"[a-z0-9áàâãéêíóôõúç_+-]+", re.IGNORECASE)
LEVEL_BOOST = 0.12
DEFAULT_TOP_K = 4


@dataclass(frozen=True)
class SearchHit:
    document: KnowledgeDocument
    score: float

    def to_tool_dict(self) -> dict:
        return {
            "id": self.document.id,
            "title": self.document.title,
            "category": self.document.category,
            "level_id": self.document.level_id,
            "text": self.document.text,
            "score": round(self.score, 4),
        }


class KnowledgeStore:
    def __init__(
        self,
        documents: list[KnowledgeDocument],
        embedder: Optional[Embedder] = None,
        cache_path: Optional[Path] = None,
    ) -> None:
        if not documents:
            raise ValueError("KnowledgeStore requires at least one document.")
        self._documents = list(documents)
        self._embedder: Embedder = embedder or NullEmbedder()
        self._cache_path = cache_path
        self._vectors: dict[str, list[float]] = {}
        self._build_index()

    @classmethod
    def from_directory(
        cls,
        root: Path,
        embedder: Optional[Embedder] = None,
        cache_path: Optional[Path] = None,
    ) -> "KnowledgeStore":
        return cls(load_knowledge_dir(root), embedder=embedder, cache_path=cache_path)

    @property
    def documents(self) -> list[KnowledgeDocument]:
        return list(self._documents)

    def persona_text(self) -> str:
        for doc in self._documents:
            if doc.category == "persona":
                return doc.text
        return ""

    def search(
        self,
        query: str,
        *,
        category: Optional[str] = None,
        level_id: Optional[str] = None,
        top_k: int = DEFAULT_TOP_K,
    ) -> list[SearchHit]:
        if not query or not query.strip():
            return []
        category_filter = (category or "").strip() or None
        level = (level_id or "").strip() or None
        k = max(1, top_k)

        pool = [
            doc
            for doc in self._documents
            if category_filter is None or doc.category == category_filter
        ]
        if not pool:
            return []

        query_vec = self._embedder.embed_query(query)
        use_vectors = (
            query_vec is not None
            and all(doc.id in self._vectors for doc in pool)
        )

        scored: list[SearchHit] = []
        if use_vectors and query_vec is not None:
            for doc in pool:
                score = cosine_similarity(query_vec, self._vectors[doc.id])
                if level and doc.level_id == level:
                    score += LEVEL_BOOST
                scored.append(SearchHit(doc, score))
        else:
            query_tokens = _tokenize(query)
            if level:
                query_tokens = query_tokens | _tokenize(level)
            for doc in pool:
                score = _keyword_score(query_tokens, doc)
                if level and doc.level_id == level:
                    score += LEVEL_BOOST
                scored.append(SearchHit(doc, score))

        scored.sort(key=lambda hit: hit.score, reverse=True)
        hits = [hit for hit in scored if hit.score > 0.0][:k]
        if not hits:
            return scored[:k]
        return hits

    def _build_index(self) -> None:
        cache = _EmbeddingCache(self._cache_path) if self._cache_path else None
        for doc in self._documents:
            digest = _document_hash(doc)
            cached = cache.get(doc.id, digest) if cache else None
            if cached is not None:
                self._vectors[doc.id] = cached
                continue
            vector = self._embedder.embed_document(_embed_text(doc))
            if vector is None:
                continue
            self._vectors[doc.id] = vector
            if cache:
                cache.put(doc.id, digest, vector)
        _LOG.info(
            "rag_index docs=%d vectors=%d",
            len(self._documents),
            len(self._vectors),
        )


def _embed_text(doc: KnowledgeDocument) -> str:
    level = doc.level_id or "geral"
    return f"{doc.title}\nnível:{level}\ncategoria:{doc.category}\n{doc.text}"


def _document_hash(doc: KnowledgeDocument) -> str:
    payload = f"{doc.id}\n{doc.category}\n{doc.title}\n{doc.level_id}\n{doc.text}"
    return hashlib.sha256(payload.encode("utf-8")).hexdigest()


def _tokenize(text: str) -> set[str]:
    return {token.lower() for token in _TOKEN_RE.findall(text)}


def _keyword_score(query_tokens: set[str], doc: KnowledgeDocument) -> float:
    if not query_tokens:
        return 0.0
    haystack = _tokenize(f"{doc.id} {doc.title} {doc.level_id} {doc.category} {doc.text}")
    if not haystack:
        return 0.0
    overlap = query_tokens & haystack
    return len(overlap) / len(query_tokens)


class _EmbeddingCache:
    def __init__(self, path: Path) -> None:
        path.parent.mkdir(parents=True, exist_ok=True)
        self._conn = sqlite3.connect(str(path))
        self._conn.execute(
            """
            CREATE TABLE IF NOT EXISTS embeddings (
                doc_id TEXT PRIMARY KEY,
                content_hash TEXT NOT NULL,
                vector_json TEXT NOT NULL
            )
            """
        )
        self._conn.commit()

    def get(self, doc_id: str, content_hash: str) -> Optional[list[float]]:
        row = self._conn.execute(
            "SELECT vector_json FROM embeddings WHERE doc_id = ? AND content_hash = ?",
            (doc_id, content_hash),
        ).fetchone()
        if row is None:
            return None
        data = json.loads(row[0])
        return [float(x) for x in data]

    def put(self, doc_id: str, content_hash: str, vector: list[float]) -> None:
        self._conn.execute(
            """
            INSERT INTO embeddings (doc_id, content_hash, vector_json)
            VALUES (?, ?, ?)
            ON CONFLICT(doc_id) DO UPDATE SET
                content_hash = excluded.content_hash,
                vector_json = excluded.vector_json
            """,
            (doc_id, content_hash, json.dumps(vector)),
        )
        self._conn.commit()
