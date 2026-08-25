"""Gemini / offline tutor provider with optional tool calling."""

from __future__ import annotations

import logging
import os
from typing import Any, Callable, Optional, Protocol

from rag.embeddings import NullEmbedder

_LOG = logging.getLogger("tutor_provider")

DEFAULT_GEMINI_MODEL = "gemini-2.5-flash"
DEFAULT_GEMINI_EMBED_MODEL = "models/gemini-embedding-001"

ExecuteTool = Callable[[str, dict[str, Any]], dict[str, Any]]


class TutorProviderUnavailable(RuntimeError):
    """Raised when no remote tutor provider can answer a request."""


class TutorProvider(Protocol):
    @property
    def name(self) -> str:
        ...

    def embed_document(self, text: str) -> Optional[list[float]]:
        ...

    def embed_query(self, text: str) -> Optional[list[float]]:
        ...

    async def generate_with_tools(
        self,
        *,
        system: str,
        user: str,
        execute_tool: ExecuteTool,
        max_rounds: int = 3,
    ) -> str:
        ...


class FallbackTutorProvider(NullEmbedder):
    @property
    def name(self) -> str:
        return "fallback"

    async def generate_with_tools(
        self,
        *,
        system: str,
        user: str,
        execute_tool: ExecuteTool,
        max_rounds: int = 3,
    ) -> str:
        raise TutorProviderUnavailable(
            "No remote tutor provider is configured for ask."
        )


class GeminiTutorProvider:
    def __init__(
        self,
        api_key: str,
        model_name: str,
        tools: Optional[list[dict[str, Any]]] = None,
        embed_model: str = DEFAULT_GEMINI_EMBED_MODEL,
    ) -> None:
        if not api_key.strip():
            raise TutorProviderUnavailable("GEMINI_API_KEY is empty.")

        try:
            import google.generativeai as genai
        except ImportError as exc:
            raise TutorProviderUnavailable(
                "google-generativeai is not installed."
            ) from exc

        genai.configure(api_key=api_key)
        self._genai = genai
        self._embed_model = embed_model
        self._model = genai.GenerativeModel(
            model_name,
            tools=tools or None,
        )

    @property
    def name(self) -> str:
        return "gemini"

    def embed_document(self, text: str) -> Optional[list[float]]:
        return self._embed(text, "retrieval_document")

    def embed_query(self, text: str) -> Optional[list[float]]:
        return self._embed(text, "retrieval_query")

    def _embed(self, text: str, task_type: str) -> Optional[list[float]]:
        try:
            result = self._genai.embed_content(
                model=self._embed_model,
                content=text,
                task_type=task_type,
            )
        except Exception:
            _LOG.exception("gemini_embed_failed")
            return None
        vector = result["embedding"] if isinstance(result, dict) else getattr(result, "embedding", None)
        if not vector:
            return None
        return [float(x) for x in vector]

    async def generate_with_tools(
        self,
        *,
        system: str,
        user: str,
        execute_tool: ExecuteTool,
        max_rounds: int = 3,
    ) -> str:
        contents: list[Any] = [
            {"role": "user", "parts": [{"text": f"{system}\n\n{user}"}]},
        ]
        response = await self._model.generate_content_async(
            contents,
            tool_config={"function_calling_config": {"mode": "auto"}},
        )

        rounds = 0
        while rounds < max_rounds:
            calls = _function_calls(response)
            if not calls:
                text = _response_text(response)
                if text:
                    return text
                break

            model_content = response.candidates[0].content
            contents.append(model_content)
            parts = []
            for name, args in calls:
                payload = execute_tool(name, args)
                parts.append(
                    {
                        "function_response": {
                            "name": name,
                            "response": payload,
                        }
                    }
                )
            contents.append({"role": "user", "parts": parts})
            rounds += 1
            mode = "none" if rounds >= max_rounds else "auto"
            response = await self._model.generate_content_async(
                contents,
                tool_config={"function_calling_config": {"mode": mode}},
            )

        text = _response_text(response)
        if text:
            return text
        raise TutorProviderUnavailable("Gemini returned no text after tool calls.")


def build_tutor_provider(
    tools: Optional[list[dict[str, Any]]] = None,
) -> TutorProvider:
    try:
        return GeminiTutorProvider(
            api_key=os.getenv("GEMINI_API_KEY", ""),
            model_name=os.getenv("GEMINI_MODEL", DEFAULT_GEMINI_MODEL),
            tools=tools,
            embed_model=os.getenv("GEMINI_EMBED_MODEL", DEFAULT_GEMINI_EMBED_MODEL),
        )
    except TutorProviderUnavailable:
        return FallbackTutorProvider()


def _function_calls(response: Any) -> list[tuple[str, dict[str, Any]]]:
    try:
        parts = response.candidates[0].content.parts
    except (AttributeError, IndexError, TypeError):
        return []
    found: list[tuple[str, dict[str, Any]]] = []
    for part in parts:
        fc = getattr(part, "function_call", None)
        name = getattr(fc, "name", "") if fc is not None else ""
        if not name:
            continue
        found.append((name, _args_dict(getattr(fc, "args", None))))
    return found


def _args_dict(raw: Any) -> dict[str, Any]:
    if raw is None:
        return {}
    if isinstance(raw, dict):
        return {str(k): v for k, v in raw.items()}
    try:
        return {str(k): v for k, v in dict(raw).items()}
    except Exception:
        return {}


def _response_text(response: Any) -> str:
    try:
        text = getattr(response, "text", None)
        if text:
            return str(text).strip()
    except Exception:
        pass
    try:
        parts = response.candidates[0].content.parts
    except (AttributeError, IndexError, TypeError):
        return ""
    chunks: list[str] = []
    for part in parts:
        piece = getattr(part, "text", None)
        if piece:
            chunks.append(str(piece))
    return "\n".join(chunks).strip()
