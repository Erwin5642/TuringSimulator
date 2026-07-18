import asyncio

from tutor_provider import (
    FallbackTutorProvider,
    TutorProviderUnavailable,
    build_tutor_provider,
)


def test_provider_falls_back_without_gemini_key(monkeypatch):
    monkeypatch.delenv("GEMINI_API_KEY", raising=False)

    provider = build_tutor_provider()

    assert provider.name == "fallback"


def test_fallback_provider_is_explicitly_unavailable():
    provider = FallbackTutorProvider()

    try:
        asyncio.run(provider.generate("prompt", "test"))
    except TutorProviderUnavailable as exc:
        assert "provider is configured" in str(exc).lower()
    else:
        raise AssertionError("fallback provider unexpectedly generated text")
