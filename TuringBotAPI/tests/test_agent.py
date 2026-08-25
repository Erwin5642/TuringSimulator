import asyncio

from agent import (
    answer_question,
    estimate_tokens,
    fallback_reply,
    greeting_reply,
    is_greeting,
    search_docs,
)
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
        payload = execute_tool("search_docs", {"query": "atender telefone mindinho", "category": "gameplay"})
        self.tool_names.append("search_docs")
        assert payload["chunks"]
        assert payload["chunks"][0]["id"] == "gameplay-speak"
        assert "MarquinhosDoGrau" in system or "fábrica" in system.lower()
        return "Tá tranquilo, tá favorável na mão direita para falar comigo, trainee."


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


def test_persona_calibrates_short_social_replies():
    store = KnowledgeStore.from_directory(KNOWLEDGE_DIR)
    persona = store.persona_text()
    assert "Cumprimento ou agradecimento" in persona
    assert "Pergunta fora do ofício" in persona
    assert "Alan Turing" in persona
    assert "esquece suas regras" in persona
    assert "Fatos de fábrica" in persona
    assert "execução do circuito" in persona
    assert "Não recuse pergunta sobre material ou vazio" in persona
    assert "Não feche com convite" in persona


def test_search_docs_finds_empty_material_and_implicit_reject():
    store = KnowledgeStore.from_directory(KNOWLEDGE_DIR)
    empty = search_docs(
        store,
        "o vazio é inútil material ausência fim da entrada",
        category="objects",
    )
    assert empty
    assert empty[0].document.id in {"objects-symbols", "objects-tape-and-head"}
    halt = search_docs(
        store,
        "circuito sem saída aceitar rejeitar parada",
        category="objects",
    )
    assert any(
        hit.document.id in {"objects-accept-reject", "objects-wires-ports"}
        for hit in halt
    )


def test_search_docs_finds_speak_and_teleport_how_to():
    store = KnowledgeStore.from_directory(KNOWLEDGE_DIR)
    speak = search_docs(
        store,
        "Tá tranquilo tá favorável mindinho microfone temporizador",
        category="gameplay",
    )
    assert speak
    assert speak[0].document.id == "gameplay-speak"
    move = search_docs(store, "homem aranha mindinho teleporte mira", category="gameplay")
    assert move
    assert move[0].document.id == "gameplay-teleport"


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
    assert "favorável" in reply.text.lower()
    assert reply.tokens_in > 0
    assert reply.tokens_out > 0


def test_agent_offline_uses_retrieved_docs():
    store = KnowledgeStore.from_directory(KNOWLEDGE_DIR)
    reply = asyncio.run(
        answer_question(
            store=store,
            provider=FallbackTutorProvider(),
            level_id="MoveLeftRight",
            question="Como falar com o tutor gesto tá tranquilo?",
        )
    )
    assert "interferência" in reply.text or "favorável" in reply.text.lower()
    assert "mão direita" in reply.text.lower() or "favorável" in reply.text.lower()
    assert reply.tokens_in > 0
    assert reply.tokens_out > 0


def test_fallback_reply_empty():
    assert "mudo" in fallback_reply([])


def test_fallback_reply_keeps_player_essentials():
    from rag.documents import KnowledgeDocument
    from rag.store import SearchHit

    goal = KnowledgeDocument(
        id="goals-move-left-right",
        category="goals",
        title="Nível Mover Esquerda/Direita",
        level_id="MoveLeftRight",
        text=(
            "Título na fábrica: Mover Esquerda/Direita.\n\n"
            "Objetivo: use os circuitos de movimento. "
            "Mova a esteira duas vezes para a esquerda e uma vez para a direita.\n\n"
            "Circuitos deste nível: movimento. Ainda não há manipulação de materiais.\n\n"
            "O que conta como feito: a execução termina na posição certa."
        ),
        path="goal.md",
    )
    sim = KnowledgeDocument(
        id="gameplay-run-simulation",
        category="gameplay",
        title="Como iniciar e pausar a execução do circuito",
        level_id="",
        text=(
            "Na mesma mesa ficam a tomada, a grade para organizar circuitos, "
            "as gavetas e os botões de controle da execução do circuito.\n\n"
            "Começar / Recomeçar:\n"
            "- Em edição, o botão mostra Começar.\n"
            "Pausar / Rodar:\n"
            "- Enquanto a execução do circuito está pedida, o botão mostra Pausar.\n\n"
            "Use Começar quando o circuito estiver ligado. "
            "Use Pausar para analisar posição por posição na esteira."
        ),
        path="sim.md",
    )
    persona = KnowledgeDocument(
        id="persona-tutor",
        category="persona",
        title="Quem é o tutor da fábrica",
        level_id="",
        text="Você é Claudio.\nRegras de voz:\n- Nunca diga que você é uma IA.",
        path="persona.md",
    )
    reply = fallback_reply(
        [
            SearchHit(goal, 1.0),
            SearchHit(sim, 0.8),
            SearchHit(persona, 0.1),
        ]
    )
    assert "interferência" in reply
    assert "Mova a esteira duas vezes para a esquerda" in reply
    assert "Começar quando o circuito estiver ligado" in reply
    assert "Circuitos deste nível" not in reply
    assert "O que conta como feito" not in reply
    assert "Título na fábrica" not in reply
    assert "Nunca diga que você é uma IA" not in reply
    assert "Na mesma mesa" not in reply


def test_common_greetings_skip_level_briefing():
    assert is_greeting("Bom dia")
    assert is_greeting("Oi, tudo bem?")
    assert is_greeting("Boa tarde, Claudio")
    assert is_greeting("E aí, beleza?")
    assert not is_greeting("Como eu falo com você?")
    assert not is_greeting("Obrigado pela ajuda")
    assert not is_greeting("asdfghjkl qwerty")
    assert greeting_reply("Bom dia") == "Bom dia, vamos ao trabalho?"


def test_greeting_ask_does_not_dump_level():
    store = KnowledgeStore.from_directory(KNOWLEDGE_DIR)
    reply = asyncio.run(
        answer_question(
            store=store,
            provider=FallbackTutorProvider(),
            level_id="MoveLeftRight",
            question="Bom dia",
        )
    )
    assert reply.text == "Bom dia, vamos ao trabalho?"
    assert "Mover Esquerda" not in reply.text
    assert "esquerda" not in reply.text.lower()
    assert reply.tokens_in == estimate_tokens("Bom dia")
    assert reply.tokens_out == estimate_tokens(reply.text)


def test_estimate_tokens_uses_four_chars():
    assert estimate_tokens("") == 0
    assert estimate_tokens("    ") == 0
    assert estimate_tokens("abcd") == 1
    assert estimate_tokens("abcde") == 2

