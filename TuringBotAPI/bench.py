"""Compact tape window and program payload sanitizers for ITS /ask."""

from __future__ import annotations

from typing import Any, Optional

VAZIO = "vazio"
ENGRENAGEM = "engrenagem"
PORCA = "porca"
PARAFUSO = "parafuso"
MARCADOR = "marcador"

FACTORY_SYMBOLS = frozenset({VAZIO, ENGRENAGEM, PORCA, PARAFUSO, MARCADOR})
MAX_TAPE_CELLS = 64
MAX_PROGRAM_BLOCKS = 32

TIPOS = frozenset({"movimento", "materiais", "condicao", "aceitar", "rejeitar"})
PORTAS = frozenset({"saida", "verdadeiro", "falso"})
CARTOES = frozenset(
    {"esquerda", "direita", VAZIO, ENGRENAGEM, PORCA, PARAFUSO, MARCADOR}
)


def normalize_cell(token: Any) -> str:
    text = str(token or "").strip().lower()
    return text if text in FACTORY_SYMBOLS else VAZIO


def compact_tape(cells: Optional[list[Any]], head_offset: Any) -> Optional[dict[str, Any]]:
    if cells is None:
        return None
    if not isinstance(cells, list):
        return None
    mapped = [normalize_cell(cell) for cell in cells]
    try:
        head = int(head_offset)
    except (TypeError, ValueError):
        head = 0

    occupied = [index for index, token in enumerate(mapped) if token != VAZIO]
    if not occupied:
        lo = min(0, head)
        hi = max(0, head)
        if mapped:
            hi = max(hi, len(mapped) - 1)
    else:
        lo = occupied[0] - 1
        hi = occupied[-1] + 1
        if head < lo:
            lo = head - 1
        if head > hi:
            hi = head + 1

    if head < lo:
        lo = head
    if head > hi:
        hi = head

    while hi - lo + 1 > MAX_TAPE_CELLS:
        if head - lo >= hi - head:
            lo += 1
        else:
            hi -= 1

    window = [_read_cell(mapped, index) for index in range(lo, hi + 1)]
    return {"cells": window, "head_offset": head - lo}


def _read_cell(mapped: list[str], index: int) -> str:
    if 0 <= index < len(mapped):
        return mapped[index]
    return VAZIO


def normalize_program(raw: Optional[dict[str, Any]]) -> Optional[dict[str, Any]]:
    if raw is None:
        return None
    if not isinstance(raw, dict):
        return {
            "tomada_ligada": False,
            "entrada": None,
            "blocos": [],
            "fios": [],
        }

    blocos_in = raw.get("blocos") or []
    if not isinstance(blocos_in, list):
        blocos_in = []
    blocos: list[dict[str, Any]] = []
    kept_ids: set[str] = set()
    for item in blocos_in[:MAX_PROGRAM_BLOCKS]:
        if not isinstance(item, dict):
            continue
        block_id = str(item.get("id") or "").strip()
        if not block_id:
            continue
        tipo = str(item.get("tipo") or "").strip().lower()
        if tipo not in TIPOS:
            tipo = "movimento"
        cartao_raw = item.get("cartao")
        cartao = None
        if cartao_raw is not None and str(cartao_raw).strip():
            cartao_s = str(cartao_raw).strip().lower()
            cartao = cartao_s if cartao_s in CARTOES else None
        kept_ids.add(block_id)
        block = {"id": block_id, "tipo": tipo}
        if cartao is not None:
            block["cartao"] = cartao
        blocos.append(block)

    fios_in = raw.get("fios") or []
    if not isinstance(fios_in, list):
        fios_in = []
    fios: list[dict[str, str]] = []
    for item in fios_in:
        if not isinstance(item, dict):
            continue
        src = str(item.get("de") or "").strip()
        dst = str(item.get("para") or "").strip()
        porta = str(item.get("porta") or "").strip().lower()
        if src not in kept_ids or dst not in kept_ids:
            continue
        if porta not in PORTAS:
            porta = "saida"
        fios.append({"de": src, "porta": porta, "para": dst})

    entrada_raw = raw.get("entrada")
    entrada = str(entrada_raw).strip() if entrada_raw is not None else ""
    tomada = bool(raw.get("tomada_ligada")) and entrada in kept_ids
    return {
        "tomada_ligada": tomada,
        "entrada": entrada if tomada else None,
        "blocos": blocos,
        "fios": fios,
    }
