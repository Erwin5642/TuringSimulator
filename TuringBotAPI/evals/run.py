"""Run the tutor benchmark against Gemini or the offline fallback."""

from __future__ import annotations

import argparse
import asyncio
import json
import os
import sys
from pathlib import Path
from typing import Any

from dotenv import load_dotenv

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from agent import SEARCH_DOCS_TOOL, answer_question
from evals.bank import BANK_PATH, load_bank
from evals.score import score_reply
from rag.store import KnowledgeStore
from tutor_provider import FallbackTutorProvider, build_tutor_provider

# Free-tier Gemini is about 10 requests/minute and 250k input tokens/minute.
# 12s keeps a live run near 5 calls/minute so a large prompt does not empty the window.
DEFAULT_LIVE_REQUEST_INTERVAL_S = 12.0


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Score Claudio replies against evals/perguntas.json."
    )
    parser.add_argument(
        "--bank",
        type=Path,
        default=BANK_PATH,
        help="Benchmark JSON (default: evals/perguntas.json).",
    )
    parser.add_argument("--ids", default="", help="Comma-separated item ids.")
    parser.add_argument("--category", default="", help="Filter by categoria.")
    parser.add_argument(
        "--offline",
        action="store_true",
        help="Use keyword RAG fallback instead of Gemini.",
    )
    parser.add_argument(
        "--json-out",
        type=Path,
        default=None,
        help="Write a machine-readable report to this path.",
    )
    parser.add_argument(
        "--interval",
        type=float,
        default=None,
        help=(
            "Seconds between Gemini requests in a live run "
            f"(default {DEFAULT_LIVE_REQUEST_INTERVAL_S:g}; 0 disables). "
            "Ignored with --offline."
        ),
    )
    return parser.parse_args(argv)


def resolve_request_interval(*, live: bool, interval: float | None) -> float:
    if not live:
        return 0.0
    chosen = DEFAULT_LIVE_REQUEST_INTERVAL_S if interval is None else interval
    if chosen < 0:
        raise SystemExit("--interval must be >= 0.")
    return chosen


_CATEGORY_ALIASES = {"errors": "erros"}


def select_items(bank: dict[str, Any], *, ids: str, category: str) -> list[dict[str, Any]]:
    items = list(bank["itens"])
    wanted_ids = [part.strip() for part in ids.split(",") if part.strip()]
    if wanted_ids:
        by_id = {item["id"]: item for item in items}
        missing = [item_id for item_id in wanted_ids if item_id not in by_id]
        if missing:
            raise SystemExit(f"Unknown ids: {', '.join(missing)}")
        items = [by_id[item_id] for item_id in wanted_ids]
    if category.strip():
        wanted = _CATEGORY_ALIASES.get(category.strip(), category.strip())
        items = [item for item in items if item["categoria"] == wanted]
        if not items:
            raise SystemExit(f"No items in categoria {category!r}.")
    return items


async def run_item(
    *,
    store: KnowledgeStore,
    provider,
    item: dict[str, Any],
    global_checks: dict[str, Any],
) -> dict[str, Any]:
    generation = await answer_question(
        store=store,
        provider=provider,
        level_id=item["level_id"],
        question=item["pergunta"],
    )
    scored = score_reply(generation.text, item, global_checks)
    return {
        "id": item["id"],
        "categoria": item["categoria"],
        "level_id": item["level_id"],
        "passed": scored.passed,
        "failures": scored.failures,
        "reply": generation.text,
        "tokens_in": generation.tokens_in,
        "tokens_out": generation.tokens_out,
        "provider": provider.name,
    }


async def run_benchmark(args: argparse.Namespace) -> dict[str, Any]:
    load_dotenv(ROOT / ".env")
    bank = load_bank(args.bank)
    items = select_items(bank, ids=args.ids, category=args.category)
    global_checks = bank.get("global_checks") or {}

    live = not args.offline and bool(os.getenv("GEMINI_API_KEY", "").strip())
    interval = resolve_request_interval(live=live, interval=args.interval)
    if not live:
        provider = FallbackTutorProvider()
        if not args.offline and provider.name == "fallback":
            print(
                "GEMINI_API_KEY missing; running offline fallback.",
                file=sys.stderr,
            )
    else:
        if interval > 0:
            print(
                f"Pacing Gemini requests every {interval:g}s.",
                file=sys.stderr,
            )
        provider = build_tutor_provider(
            tools=[SEARCH_DOCS_TOOL],
            request_interval_s=interval,
        )

    store = KnowledgeStore.from_directory(
        Path(os.getenv("KNOWLEDGE_DIR", ROOT / "knowledge")),
        embedder=provider,
        cache_path=Path(os.getenv("RAG_CACHE_PATH", ROOT / "embeddings.sqlite")),
    )

    rows: list[dict[str, Any]] = []
    for item in items:
        row = await run_item(
            store=store,
            provider=provider,
            item=item,
            global_checks=global_checks,
        )
        rows.append(row)
        mark = "PASS" if row["passed"] else "FAIL"
        extra = "" if row["passed"] else " " + ",".join(row["failures"])
        print(f"{row['id']:6} {mark}{extra}")

    passed = sum(1 for row in rows if row["passed"])
    report = {
        "bank": str(args.bank),
        "provider": provider.name,
        "passed": passed,
        "total": len(rows),
        "items": rows,
    }
    print(f"{passed}/{len(rows)} passed ({provider.name})")
    if args.json_out:
        args.json_out.parent.mkdir(parents=True, exist_ok=True)
        args.json_out.write_text(
            json.dumps(report, ensure_ascii=False, indent=2) + "\n",
            encoding="utf-8",
        )
    return report


def main(argv: list[str] | None = None) -> int:
    report = asyncio.run(run_benchmark(parse_args(argv)))
    return 0 if report["passed"] == report["total"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
