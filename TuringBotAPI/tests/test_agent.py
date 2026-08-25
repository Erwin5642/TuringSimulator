import asyncio

from agent import answer_question, fallback_reply, search_docs
from rag.store import KnowledgeStore
from tests.conftest import KNOWLEDGE_DIR
from tutor_provider import (
    FallbackTutorProvider,
    TutorProviderUnavailable,
    build_tutor_provider,
)


class ScriptedProvider:
    name = "scripted"

    def __init__(self) -> None:
        self.tool_names: list[str] = []

    def embed_document(self, text: str):
        return None

    def embed_query(self, text: str):
        return None

    async def generate_with_tools(self, *, system, user, execute_tool, max_rounds=3):
        payload = execute_tool("search_docs", {"query": "shaka", "category": "gameplay"})
        self.tool_names.append("search_docs")
        assert payload["chunks"]
        assert payload["chunks"][0]["id"] == "gameplay-speak"
        assert "MarquinhosDoGrau" in system or "fábrica" in system.lower()
        return "Segura o Shaka na mão direita para falar comigo, trainee."


def test_provider_falls_back_without_gemini_key(monkeypatch):
    monkeypatch.setenv("GEMINI_API_KEY", "")
    provider = build_tutor_provider()
    assert provider.name == "fallback"


def test_fallback_provider_is_explicitly_unavailable():
    provider = FallbackTutorProvider()
    try:
        asyncio.run(
            provider.generate_with_tools(
                system="s",
                user="u",
                execute_tool=lambda n, a: {},
            )
        )
    except TutorProviderUnavailable:
        return
    raise AssertionError("fallback provider unexpectedly generated text")


def test_search_docs_tool_returns_chunks():
    store = KnowledgeStore.from_directory(KNOWLEDGE_DIR)
    hits = search_docs(store, "teleporte chão", category="gameplay")
    assert hits
    assert hits[0].document.id == "gameplay-teleport"


def test_agent_calls_search_docs_then_answers():
    store = KnowledgeStore.from_directory(KNOWLEDGE_DIR)
    provider = ScriptedProvider()
    reply = asyncio.run(
        answer_question(
            store=store,
            provider=provider,
            level_id="MoveLeftRight",
            question="Como eu falo com você?",
        )
    )
    assert provider.tool_names == ["search_docs"]
    assert "Shaka" in reply


def test_agent_offline_uses_retrieved_docs():
    store = KnowledgeStore.from_directory(KNOWLEDGE_DIR)
    reply = asyncio.run(
        answer_question(
            store=store,
            provider=FallbackTutorProvider(),
            level_id="MoveLeftRight",
            question="Como falar com o tutor usando shaka?",
        )
    )
    assert "interferência" in reply or "Shaka" in reply
    assert "mão direita" in reply.lower() or "Shaka" in reply


def test_fallback_reply_empty():
    assert "mudo" in fallback_reply([])
