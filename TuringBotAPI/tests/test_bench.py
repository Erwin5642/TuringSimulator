from bench import compact_tape, normalize_program


def test_compact_tape_empty_at_origin_is_single_vazio():
    assert compact_tape([], 0) == {"cells": ["vazio"], "head_offset": 0}
    assert compact_tape(None, 0) is None


def test_compact_tape_empty_keeps_actual_head_offset():
    moved = compact_tape([], 2)
    assert moved == {
        "cells": ["vazio", "vazio", "vazio"],
        "head_offset": 2,
    }
    assert compact_tape(moved["cells"], moved["head_offset"]) == moved
def test_compact_tape_empty_head_left_spans_to_origin():
    assert compact_tape([], -1) == {
        "cells": ["vazio", "vazio"],
        "head_offset": 0,
    }


def test_compact_tape_level1_main():
    result = compact_tape(["engrenagem", "parafuso", "porca"], 2)
    assert result == {
        "cells": ["vazio", "engrenagem", "parafuso", "porca", "vazio"],
        "head_offset": 3,
    }


def test_compact_tape_head_left_of_span():
    result = compact_tape(["vazio", "vazio", "vazio", "vazio", "vazio", "engrenagem"], 0)
    assert result["cells"][result["head_offset"]] == "vazio"
    assert "engrenagem" in result["cells"]
    assert result["cells"][0] == "vazio"
    assert result["cells"][-1] == "vazio"


def test_compact_tape_keeps_interior_blanks():
    result = compact_tape(["engrenagem", "vazio", "vazio", "porca"], 0)
    assert result["cells"] == [
        "vazio",
        "engrenagem",
        "vazio",
        "vazio",
        "porca",
        "vazio",
    ]
    assert result["head_offset"] == 1


def test_compact_tape_is_idempotent_on_window():
    first = compact_tape(["engrenagem", "parafuso", "porca"], 2)
    second = compact_tape(first["cells"], first["head_offset"])
    assert second == first


def test_normalize_program_factory_names():
    raw = {
        "tomada_ligada": True,
        "entrada": "m1",
        "blocos": [
            {"id": "m1", "tipo": "movimento", "cartao": "esquerda"},
            {"id": "c1", "tipo": "condicao", "cartao": "engrenagem"},
        ],
        "fios": [{"de": "m1", "porta": "saida", "para": "c1"}],
    }
    out = normalize_program(raw)
    assert out["tomada_ligada"] is True
    assert out["blocos"][0]["cartao"] == "esquerda"
    assert out["fios"][0]["porta"] == "saida"
