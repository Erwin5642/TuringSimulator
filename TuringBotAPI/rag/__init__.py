"""Public RAG package surface."""

from rag.documents import KnowledgeDocument, load_knowledge_dir
from rag.store import KnowledgeStore, SearchHit

__all__ = [
    "KnowledgeDocument",
    "KnowledgeStore",
    "SearchHit",
    "load_knowledge_dir",
]
