"""Load and validate the tutor benchmark JSON."""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

BANK_PATH = Path(__file__).resolve().parent / "perguntas.json"

CATEGORIES = frozenset(
    {
        "objetos",
        "conceitos",
        "erros",
        "niveis",
        "meta_solucao",
        "cumprimento",
        "fora_de_contexto",
        "agente",
        "ruido",
    }
)
LEVEL_IDS = frozenset(
    {
        "MoveLeftRight",
        "PlaceGear",
        "ReplaceAllWithNuts",
        "RejectIfGearExists",
        "SwapNutsAndScrews",
        "PatternRepeated",
        "BalancedPairs",
        "PatternSomewhere",
    }
)
ITEM_KEYS = frozenset(
    {"id", "categoria", "level_id", "pergunta", "referencia", "notes", "checks"}
)
CHECK_KEYS = frozenset(
    {
        "contain_all",
        "contain_any",
        "contain_groups",
        "forbid",
        "max_sentences",
        "expect_refuse",
    }
)


def load_bank(path: Path | None = None) -> dict[str, Any]:
    target = path or BANK_PATH
    raw = json.loads(target.read_text(encoding="utf-8"))
    validate_bank(raw)
    return raw


def validate_bank(data: dict[str, Any]) -> None:
    if data.get("idioma") != "pt-BR":
        raise ValueError("Benchmark idioma must be pt-BR.")
    items = data.get("itens")
    if not isinstance(items, list) or not items:
        raise ValueError("Benchmark needs a non-empty itens list.")

    ids: set[str] = set()
    for item in items:
        _validate_item(item, ids)

    global_checks = data.get("global_checks") or {}
    _validate_checks(global_checks, where="global_checks")


def _validate_item(item: Any, ids: set[str]) -> None:
    if not isinstance(item, dict):
        raise ValueError("Each item must be an object.")
    extra = set(item) - ITEM_KEYS
    if extra:
        raise ValueError(f"Unknown item fields {sorted(extra)} in {item.get('id')}.")

    item_id = item.get("id")
    if not isinstance(item_id, str) or not item_id.strip():
        raise ValueError("Each item needs a non-empty id.")
    if item_id in ids:
        raise ValueError(f"Duplicate item id {item_id!r}.")
    ids.add(item_id)

    category = item.get("categoria")
    if category not in CATEGORIES:
        raise ValueError(f"Unknown categoria {category!r} in {item_id}.")

    level_id = item.get("level_id")
    if level_id not in LEVEL_IDS:
        raise ValueError(f"Unknown level_id {level_id!r} in {item_id}.")

    question = item.get("pergunta")
    if not isinstance(question, str) or not question.strip():
        raise ValueError(f"Empty pergunta in {item_id}.")

    referencia = item.get("referencia", "")
    if referencia is not None and not isinstance(referencia, str):
        raise ValueError(f"referencia must be a string in {item_id}.")

    notes = item.get("notes", "")
    if notes is not None and not isinstance(notes, str):
        raise ValueError(f"notes must be a string in {item_id}.")

    checks = item.get("checks")
    if not isinstance(checks, dict) or not checks:
        raise ValueError(f"Item {item_id} needs a non-empty checks object.")
    _validate_checks(checks, where=item_id)


def _validate_checks(checks: dict[str, Any], *, where: str) -> None:
    extra = set(checks) - CHECK_KEYS
    if extra:
        raise ValueError(f"Unknown check fields {sorted(extra)} in {where}.")

    for key in ("contain_all", "contain_any", "forbid"):
        value = checks.get(key)
        if value is None:
            continue
        if not _is_str_list(value):
            raise ValueError(f"{where}.{key} must be a list of strings.")

    groups = checks.get("contain_groups")
    if groups is not None:
        if not isinstance(groups, list) or not groups:
            raise ValueError(f"{where}.contain_groups must be a non-empty list.")
        for group in groups:
            if not _is_str_list(group) or not group:
                raise ValueError(
                    f"{where}.contain_groups entries must be non-empty string lists."
                )

    max_sentences = checks.get("max_sentences")
    if max_sentences is not None:
        if not isinstance(max_sentences, int) or max_sentences < 1:
            raise ValueError(f"{where}.max_sentences must be a positive int.")

    expect_refuse = checks.get("expect_refuse")
    if expect_refuse is not None and not isinstance(expect_refuse, bool):
        raise ValueError(f"{where}.expect_refuse must be a boolean.")


def _is_str_list(value: Any) -> bool:
    return isinstance(value, list) and all(isinstance(item, str) and item.strip() for item in value)
