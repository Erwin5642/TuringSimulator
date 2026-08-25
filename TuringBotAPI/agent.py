"""Agentic RAG loop: Gemini may call search_docs up to three times."""

from __future__ import annotations

import logging
import re
from typing import Any, Optional

from rag.documents import KnowledgeDocument
from rag.store import KnowledgeStore, SearchHit
from tutor_provider import GenerationResult, TutorProvider, TutorProviderUnavailable

_LOG = logging.getLogger("agent")

MAX_TOOL_ROUNDS = 3
PERSONA_NAME_DEFAULT = "Claudio"

SEARCH_DOCS_TOOL: dict[str, Any] = {
    "function_declarations": [
        {
            "name": "search_docs",
            "description": (
                "Busca trechos da base da fábrica (gameplay, objects, goals, concepts). "
                "Chame para dúvida de fábrica: como jogar, objeto, material, execução "
                "ou objetivo do nível. "
                "Não chame para cumprimento, agradecimento, identidade "
                "(nome, se é IA, para quem trabalha) nem assunto fora da fábrica."
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
    "Segue o que eu ainda consegui puxar da prancheta: "
)
_OFFLINE_EMPTY = (
    "O rádio da fábrica está mudo, trainee. "
    "Olhe o objetivo do nível, siga o fio da energia e tente de novo."
)

_GREETING_RE = re.compile(
    r"^\s*(oi|ol[aá]|e a[ií]|fala|salve|hey|bom dia|boa tarde|boa noite)"
    r"(?:\s*[,!.]*)?"
    r"(?:\s+(?:claudio|trainee|tudo bem|beleza|tudo certo))?"
    r"(?:\s*[?.!]*)?\s*$",
    re.IGNORECASE,
)

_FALLBACK_SENTENCE_RE = re.compile(r"(?<=[.!?])\s+")
_FALLBACK_NUMBERED_RE = re.compile(r"^\d+\.\s")
_FALLBACK_SKIP_CATEGORIES = frozenset({"persona"})
_FALLBACK_SKIP_PREFIXES = (
    "circuitos deste nível",
    "o que conta como feito",
    "título na fábrica",
    "você é ",
    "seu propósito",
    "regras de voz",
)
_FALLBACK_HITS = 2
_FALLBACK_SENTENCES_PER_HIT = 2
_FALLBACK_INSTRUCTION_PREFIXES = (
    "use ",
    "para ",
    "faça ",
    "aperte ",
    "pegue ",
    "mova ",
    "ligue ",
    "configure ",
    "não use ",
)


def is_greeting(question: str) -> bool:
    return bool(_GREETING_RE.match((question or "").strip()))


def greeting_reply(question: str) -> str:
    lower = (question or "").strip().lower()
    if "bom dia" in lower:
        return "Bom dia, vamos ao trabalho?"
    if "boa tarde" in lower:
        return "Boa tarde, trainee!"
    if "boa noite" in lower:
        return "Boa noite, trainee!"
    return "Olá, trainee. O que manda agora?"


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


def _is_agent_only_line(line: str) -> bool:
    lower = line.lower()
    if any(lower.startswith(prefix) for prefix in _FALLBACK_SKIP_PREFIXES):
        return True
    if line.startswith(("- ", "* ", "• ")):
        return True
    if _FALLBACK_NUMBERED_RE.match(line):
        return True
    if line.endswith(":") and not lower.startswith("objetivo:"):
        return True
    return False


def _player_sentences(doc: KnowledgeDocument, limit: int) -> str:
    if doc.category in _FALLBACK_SKIP_CATEGORIES:
        return ""
    kept: list[str] = []
    for raw in doc.text.splitlines():
        line = raw.strip()
        if not line or _is_agent_only_line(line):
            continue
        if line.lower().startswith("objetivo:"):
            body = line.split(":", 1)[1].strip()
            if body:
                kept.append(body)
            continue
        kept.append(line)
    if not kept:
        return ""
    blob = " ".join(kept)
    parts = [part.strip() for part in _FALLBACK_SENTENCE_RE.split(blob) if part.strip()]
    preferred = [
        part
        for part in parts
        if part.lower().startswith(_FALLBACK_INSTRUCTION_PREFIXES)
    ]
    chosen = preferred or parts
    return " ".join(chosen[:limit]).strip()


def fallback_reply(hits: list[SearchHit]) -> str:
    if not hits:
        return _OFFLINE_EMPTY
    parts: list[str] = []
    for hit in hits[:_FALLBACK_HITS]:
        snippet = _player_sentences(hit.document, _FALLBACK_SENTENCES_PER_HIT)
        if snippet:
            parts.append(snippet)
    if not parts:
        return _OFFLINE_EMPTY
    return _OFFLINE_PREFIX + " ".join(parts)


def build_system_prompt(store: KnowledgeStore, level_id: str, agent_name: str) -> str:
    persona = store.persona_text() or (
        f"Você é {agent_name}, operário da fábrica de Máquinas de Turing."
    )
    level = level_id.strip() or "desconhecido"
    return (
        f"{persona}\n\n"
        f"Contexto interno do posto (não recite se o trainee não perguntou da tarefa): "
        f"nível {level}.\n\n"
        "A persona define voz, o que responder e o que recusar. "
        "Os fatos estão nos documentos: para dúvida de fábrica, chame search_docs "
        "(no máximo três vezes) e use o trecho. "
        "Cumprimento, identidade e assunto fora da fábrica: não busque. "
        "Não invente o que os documentos não trouxerem."
    )


def estimate_tokens(text: str) -> int:
    stripped = (text or "").strip()
    if not stripped:
        return 0
    return max(1, (len(stripped) + 3) // 4)


def _as_generation(value: Any, question: str = "") -> GenerationResult:
    if isinstance(value, GenerationResult):
        result = value
    else:
        result = GenerationResult(text=str(value or ""))
    if result.tokens_in or result.tokens_out:
        return result
    return GenerationResult(
        text=result.text,
        tokens_in=estimate_tokens(question),
        tokens_out=estimate_tokens(result.text),
    )


async def answer_question(
    *,
    store: KnowledgeStore,
    provider: TutorProvider,
    level_id: str,
    question: str,
    agent_name: str = PERSONA_NAME_DEFAULT,
) -> GenerationResult:
    q = question.strip()
    if not q:
        raise ValueError("Question cannot be empty.")

    if is_greeting(q):
        reply = greeting_reply(q)
        return _as_generation(reply, q)

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
        raw = await provider.generate_with_tools(
            system=build_system_prompt(store, level_id, agent_name),
            user=f"Pergunta do trainee: {q}",
            execute_tool=execute_tool,
            max_rounds=MAX_TOOL_ROUNDS,
        )
        return _as_generation(raw, q)
    except TutorProviderUnavailable:
        hits = search_docs(store, q, level_id=level_id)
        return _as_generation(fallback_reply(hits), q)
    except Exception:
        _LOG.exception("agent_failed")
        hits = search_docs(store, q, level_id=level_id)
        return _as_generation(fallback_reply(hits), q)
