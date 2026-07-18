"""Tutor text provider boundary used by the orchestration layer.

The API must remain usable when Gemini is not configured.  The provider
interface keeps that fallback explicit and makes the orchestration layer
testable without network calls.
"""

from __future__ import annotations

from typing import Protocol


class TutorProviderUnavailable(RuntimeError):
    """Raised when no remote tutor provider can answer a request."""


class TutorProvider(Protocol):
    """Small contract for an async text-generation provider."""

    @property
    def name(self) -> str:
        ...

    async def generate(self, prompt: str, context: str) -> str:
        ...


class FallbackTutorProvider:
    """Explicit offline provider used when Gemini is unavailable."""

    @property
    def name(self) -> str:
        return "fallback"

    async def generate(self, prompt: str, context: str) -> str:
        raise TutorProviderUnavailable(
            f"No remote tutor provider is configured for {context}."
        )


class GeminiTutorProvider:
    """Thin adapter around the legacy Gemini SDK used by this MVP."""

    def __init__(self, api_key: str, model_name: str) -> None:
        if not api_key.strip():
            raise TutorProviderUnavailable("GEMINI_API_KEY is empty.")

        try:
            import google.generativeai as genai
        except ImportError as exc:
            raise TutorProviderUnavailable(
                "google-generativeai is not installed."
            ) from exc

        genai.configure(api_key=api_key)
        self._model = genai.GenerativeModel(model_name)

    @property
    def name(self) -> str:
        return "gemini"

    async def generate(self, prompt: str, context: str) -> str:
        response = await self._model.generate_content_async(
            [{"role": "user", "parts": [prompt]}]
        )
        text = getattr(response, "text", "").strip()
        if not text:
            raise TutorProviderUnavailable(
                f"Gemini returned an empty response for {context}."
            )
        return text


def build_tutor_provider() -> TutorProvider:
    """Build Gemini when configured, otherwise return the offline provider."""

    import os

    try:
        return GeminiTutorProvider(
            api_key=os.getenv("GEMINI_API_KEY", ""),
            model_name=os.getenv("GEMINI_MODEL", "gemini-1.5-flash"),
        )
    except TutorProviderUnavailable:
        return FallbackTutorProvider()
