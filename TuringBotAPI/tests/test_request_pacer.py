import asyncio
import json
import time

import pytest

from evals.run import (
    DEFAULT_LIVE_REQUEST_INTERVAL_S,
    parse_args,
    resolve_request_interval,
    run_benchmark,
)
from request_pacer import RequestPacer
from tutor_provider import GeminiTutorProvider


class _RecordingPacer:
    def __init__(self) -> None:
        self.waits = 0

    def wait(self) -> None:
        self.waits += 1


class _TextPart:
    def __init__(self, text: str) -> None:
        self.text = text
        self.function_call = None


class _ToolPart:
    def __init__(self) -> None:
        self.text = None
        self.function_call = type(
            "Call",
            (),
            {"name": "search_docs", "args": {"query": "tomada"}},
        )()


class _Response:
    def __init__(self, parts: list) -> None:
        content = type("Content", (), {"parts": parts})()
        self.candidates = [type("Candidate", (), {"content": content})()]
        self.usage_metadata = None
        self.text = parts[0].text if parts else ""


class _Model:
    def __init__(self, responses: list[_Response]) -> None:
        self._responses = list(responses)

    async def generate_content_async(self, contents, tool_config=None):
        return self._responses.pop(0)


def _provider(pacer: _RecordingPacer, model: _Model | None = None) -> GeminiTutorProvider:
    provider = object.__new__(GeminiTutorProvider)
    provider._pacer = pacer
    provider._model = model or _Model([])
    provider._embed_model = "embed"
    provider._genai = type(
        "Genai",
        (),
        {"embed_content": staticmethod(lambda **kwargs: {"embedding": [0.2, 0.4]})},
    )()
    return provider


def test_pacer_spaces_requests_by_the_remainder(monkeypatch):
    clock = {"now": 100.0}
    slept: list[float] = []

    monkeypatch.setattr(time, "monotonic", lambda: clock["now"])

    def fake_sleep(seconds: float) -> None:
        slept.append(seconds)
        clock["now"] += seconds

    monkeypatch.setattr(time, "sleep", fake_sleep)
    pacer = RequestPacer(2.0)
    pacer.wait()
    assert slept == []

    clock["now"] = 101.0
    pacer.wait()
    assert slept == [1.0]

    pacer.wait()
    assert slept == [1.0, 2.0]


def test_zero_interval_never_sleeps(monkeypatch):
    monkeypatch.setattr(time, "sleep", lambda seconds: pytest.fail("slept"))
    pacer = RequestPacer(0)
    pacer.wait()
    pacer.wait()


def test_negative_interval_is_rejected():
    with pytest.raises(ValueError):
        RequestPacer(-0.1)


def test_generate_waits_before_every_remote_round():
    pacer = _RecordingPacer()
    model = _Model(
        [
            _Response([_ToolPart()]),
            _Response([_TextPart("A tomada liga o primeiro bloco.")]),
        ]
    )
    provider = _provider(pacer, model)
    result = asyncio.run(
        provider.generate_with_tools(
            system="sistema",
            user="pergunta",
            execute_tool=lambda name, args: {"chunks": []},
        )
    )
    assert pacer.waits == 2
    assert "tomada" in result.text


def test_embed_waits_before_each_call():
    pacer = _RecordingPacer()
    provider = _provider(pacer)
    assert provider.embed_document("tomada") == [0.2, 0.4]
    assert provider.embed_query("energia") == [0.2, 0.4]
    assert pacer.waits == 2


def test_resolve_request_interval():
    assert resolve_request_interval(live=False, interval=30) == 0.0
    assert resolve_request_interval(live=True, interval=None) == DEFAULT_LIVE_REQUEST_INTERVAL_S
    assert resolve_request_interval(live=True, interval=0) == 0.0
    with pytest.raises(SystemExit):
        resolve_request_interval(live=True, interval=-1)


def _bank(path) -> None:
    path.write_text(
        json.dumps(
            {
                "idioma": "pt-BR",
                "global_checks": {"forbid": ["sou uma ia"]},
                "itens": [
                    {
                        "id": "t-01",
                        "categoria": "fora_de_contexto",
                        "level_id": "MoveLeftRight",
                        "pergunta": "Qual a capital da França?",
                        "referencia": "Desculpa, mas não tenho conhecimento sobre isso.",
                        "notes": "",
                        "checks": {"expect_refuse": True},
                    }
                ],
            }
        ),
        encoding="utf-8",
    )


class _ScriptedProvider:
    name = "scripted"

    def embed_document(self, text: str):
        return None

    def embed_query(self, text: str):
        return None

    async def generate_with_tools(self, *, system, user, execute_tool, max_rounds=3):
        return "Desculpa, mas não tenho conhecimento sobre isso."


def test_live_benchmark_paces_gemini(monkeypatch, tmp_path):
    bank = tmp_path / "bank.json"
    _bank(bank)
    monkeypatch.setenv("GEMINI_API_KEY", "test-key")
    seen: dict[str, float] = {}

    def fake_build(tools=None, request_interval_s=0.0):
        seen["interval"] = request_interval_s
        return _ScriptedProvider()

    monkeypatch.setattr("evals.run.build_tutor_provider", fake_build)
    report = asyncio.run(run_benchmark(parse_args(["--bank", str(bank)])))
    assert seen["interval"] == DEFAULT_LIVE_REQUEST_INTERVAL_S
    assert report["total"] == 1
    assert report["passed"] == 1

    report = asyncio.run(
        run_benchmark(parse_args(["--bank", str(bank), "--interval", "3"]))
    )
    assert seen["interval"] == 3.0


def test_offline_benchmark_does_not_build_gemini(monkeypatch, tmp_path):
    bank = tmp_path / "bank.json"
    _bank(bank)
    monkeypatch.setenv("GEMINI_API_KEY", "test-key")

    def fake_build(tools=None, request_interval_s=0.0):
        raise AssertionError("offline run built a remote provider")

    monkeypatch.setattr("evals.run.build_tutor_provider", fake_build)
    report = asyncio.run(
        run_benchmark(parse_args(["--bank", str(bank), "--offline", "--interval", "9"]))
    )
    assert report["provider"] == "fallback"
    assert report["total"] == 1
