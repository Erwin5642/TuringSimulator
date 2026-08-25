"""Agentic RAG loop: Gemini may call search_docs up to three times."""

from __future__ import annotations

import logging
from typing import Any, Optional

from rag.store import KnowledgeStore, SearchHit
from tutor_provider import TutorProvider, TutorProviderUnavailable

_LOG = logging.getLogger("agent")

MAX_TOOL_ROUNDS = 3
PERSONA_NAME_DEFAULT = "MarquinhosDoGrau"

SEARCH_DOCS_TOOL: dict[str, Any] = {
    "function_declarations": [
        {
            "name": "search_docs",
            "description": (
                "Busca trechos da base da fábrica: persona, como jogar, "
                "objetos (blocos, cartões, botões), objetivos de nível e conceitos. "
                "Use antes de responder perguntas sobre o jogo."
            ),
            "parameters": {
                "type": "object",
                "properties": {
                    "query": {
                        "type": "string",
                        "description": "Consulta em português sobre o jogo, o nível ou um objeto.",
                    },
                    "category": {
                        "type": "string",
                        "description": (
                            "Filtro opcional: persona, gameplay, objects, goals, concepts."
                        ),
                    },
                },
                "required": ["query"],
            },
        }
    ]
}

_OFFLINE_PREFIX = (
    "O rádio da fábrica está com interferência, trainee. "
    "Segue o que eu ainda consegui puxar da prancheta:\n\n"
)
_OFFLINE_EMPTY = (
    "O rádio da fábrica está mudo, trainee. "
    "Olhe o objetivo do nível, siga o fio da energia e tente de novo."
)


def format_hits(hits: list[SearchHit]) -> dict[str, Any]:
    return {"chunks": [hit.to_tool_dict() for hit in hits]}


def search_docs(
    store: KnowledgeStore,
    query: str,
    *,
    category: Optional[str] = None,
    level_id: Optional[str] = None,
    top_k: int = 4,
) -> list[SearchHit]:
    cat = (category or "").strip() or None
    if cat == "":
        cat = None
    return store.search(query, category=cat, level_id=level_id, top_k=top_k)


def fallback_reply(hits: list[SearchHit]) -> str:
    if not hits:
        return _OFFLINE_EMPTY
    parts = [hit.document.text for hit in hits[:2]]
    return _OFFLINE_PREFIX + "\n\n".join(parts)


def build_system_prompt(store: KnowledgeStore, level_id: str, agent_name: str) -> str:
    persona = store.persona_text() or (
        f"Você é {agent_name}, operário da fábrica de Máquinas de Turing."
    )
    level = level_id.strip() or "desconhecido"
    return (
        f"{persona}\n\n"
        f"Nível atual do trainee: {level}\n\n"
        "Você tem a ferramenta search_docs. Sempre busque documentos antes de "
        "responder dúvidas sobre como jogar, blocos, botões, gestos ou o objetivo "
        "do nível. Pode chamar search_docs no máximo três vezes. "
        "Responda em português brasileiro, em frases curtas. "
        "Não entregue o circuito completo de uma vez. "
        "Não invente controles ou blocos que não apareçam nos documentos."
    )


async def answer_question(
    *,
    store: KnowledgeStore,
    provider: TutorProvider,
    level_id: str,
    question: str,
    agent_name: str = PERSONA_NAME_DEFAULT,
) -> str:
    q = question.strip()
    if not q:
        raise ValueError("Question cannot be empty.")

    calls = {"count": 0}

    def execute_tool(name: str, args: dict[str, Any]) -> dict[str, Any]:
        if name != "search_docs":
            return {"error": f"unknown_tool:{name}"}
        if calls["count"] >= MAX_TOOL_ROUNDS:
            return {"error": "search_limit_reached", "chunks": []}
        calls["count"] += 1
        query = str(args.get("query") or q)
        category = args.get("category")
        category_s = str(category) if category else None
        hits = search_docs(
            store,
            query,
            category=category_s,
            level_id=level_id,
        )
        _LOG.info(
            "search_docs n=%d query=%s category=%s hits=%s",
            calls["count"],
            query[:80],
            category_s,
            [hit.document.id for hit in hits],
        )
        return format_hits(hits)

    try:
        return await provider.generate_with_tools(
            system=build_system_prompt(store, level_id, agent_name),
            user=f"Pergunta do trainee: {q}",
            execute_tool=execute_tool,
            max_rounds=MAX_TOOL_ROUNDS,
        )
    except TutorProviderUnavailable:
        hits = search_docs(store, q, level_id=level_id)
        return fallback_reply(hits)
    except Exception:
        _LOG.exception("agent_failed")
        hits = search_docs(store, q, level_id=level_id)
        return fallback_reply(hits)
