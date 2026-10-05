"""Tutor benchmark bank + deterministic scorer."""

from evals.bank import load_bank
from evals.score import count_sentences, score_reply, term_in


def test_bank_loads_with_unique_scored_items():
    bank = load_bank()
    items = bank["itens"]
    ids = [item["id"] for item in items]
    assert len(ids) == len(set(ids))
    assert len(items) >= 50
    assert bank["idioma"] == "pt-BR"
    assert "forbid" in bank["global_checks"]
    categories = {item["categoria"] for item in items}
    assert {"objetos", "conceitos", "erros", "niveis", "meta_solucao"} <= categories


def test_every_gold_reply_passes_its_checks():
    bank = load_bank()
    global_checks = bank.get("global_checks")
    failed: list[str] = []
    for item in bank["itens"]:
        gold = (item.get("referencia") or "").strip()
        assert gold, f"{item['id']} missing referencia"
        scored = score_reply(gold, item, global_checks)
        if not scored.passed:
            failed.append(f"{item['id']}: {scored.failures}")
    assert not failed


def test_scorer_rejects_empty_and_ai_admission():
    bank = load_bank()
    item = next(row for row in bank["itens"] if row["id"] == "o-02")
    empty = score_reply("", item, bank["global_checks"])
    assert not empty.passed
    assert "empty_reply" in empty.failures

    leaked = score_reply(
        "Sou uma IA e a tomada liga o primeiro bloco.",
        item,
        bank["global_checks"],
    )
    assert not leaked.passed
    assert any(flag.startswith("forbid:") for flag in leaked.failures)


def test_scorer_catches_missing_fact_and_refuse():
    bank = load_bank()
    materials = next(row for row in bank["itens"] if row["id"] == "o-11")
    missing = score_reply(
        "Na esteira passam engrenagens e porcas.",
        materials,
        bank["global_checks"],
    )
    assert not missing.passed
    assert "contain_all:vazio" in missing.failures

    off_topic = next(row for row in bank["itens"] if row["id"] == "x-01")
    paris = score_reply("Paris.", off_topic, bank["global_checks"])
    assert not paris.passed
    assert "expect_refuse" in paris.failures

    refused = score_reply(
        "Desculpa, mas não tenho conhecimento sobre isso.",
        off_topic,
        bank["global_checks"],
    )
    assert refused.passed


def test_term_in_accepts_portuguese_plurals():
    haystack = "engrenagens cartoes porcas"
    assert term_in(haystack, "engrenagem")
    assert term_in(haystack, "cartao")
    assert term_in(haystack, "porca")
    assert not term_in(haystack, "parafuso")


def test_max_sentences_counts_reply_length():
    assert count_sentences("Bom dia, vamos ao trabalho?") == 1
    item = {
        "id": "g-02",
        "checks": {"contain_all": ["bom dia"], "max_sentences": 2},
    }
    long_reply = "Bom dia. Objetivo: esquerda. Depois direita. Ainda tem mais."
    scored = score_reply(long_reply, item, {})
    assert not scored.passed
    assert any(flag.startswith("max_sentences:") for flag in scored.failures)
