"""
orchestrator.py

Assembles the system prompt from the domain model and student state,
then calls Google Gemini Flash to generate the agent's response.

The agent character (default name: Bolt) is an embodied factory worker
inside the game who helps the student understand Turing Machines through
the factory metaphor. It never breaks the fourth wall.
"""

from __future__ import annotations

import logging
import os
import time
from typing import Optional

import google.generativeai as genai
from dotenv import load_dotenv

from domain.concepts import CONCEPT_MAP
from domain.hints import HINT_FOREST, HintLevel, get_hint_template
from student_model import STUDENT_MODEL

load_dotenv()

_log = logging.getLogger("orchestrator")

# ── Gemini setup ────────────────────────────────────────────────────────────
genai.configure(api_key=os.environ["GEMINI_API_KEY"])

_MODEL_NAME  = os.getenv("GEMINI_MODEL", "gemini-1.5-flash")
_AGENT_NAME  = os.getenv("AGENT_NAME", "MarquinhosDoGrau")
_gemini      = genai.GenerativeModel(_MODEL_NAME)


async def _timed_gemini_generate(parts: list, context: str) -> str:
    t0 = time.perf_counter()
    try:
        response = await _gemini.generate_content_async(
            [{"role": "user", "parts": parts}]
        )
        ms = (time.perf_counter() - t0) * 1000.0
        _log.info(
            "gemini_ok ctx=%s latency_ms=%.1f",
            context,
            ms,
            extra={"latency_ms": round(ms, 2)},
        )
        return response.text
    except Exception:
        ms = (time.perf_counter() - t0) * 1000.0
        _log.exception(
            "gemini_fail ctx=%s latency_ms=%.1f",
            context,
            ms,
            extra={"latency_ms": round(ms, 2)},
        )
        raise


# ── Prompt constants ────────────────────────────────────────────────────────

_BASE_PERSONA = f"""
You are {_AGENT_NAME}, a friendly factory worker robot who lives inside
a Turing Machine factory. You help trainee operators (the player) learn
how to wire up instruction blocks to control the factory's conveyor robot.

Rules you must always follow:
- Stay in character as a factory worker. Use factory metaphors naturally.
- Never reveal that you are an AI or an LLM.
- Never give the full circuit solution unprompted.
- Speak in short, clear sentences. The player is a beginner.
- If the player asks a theory question, relate it back to the factory.
- If you are given a hint template, expand it naturally in your voice —
  do not reproduce it word for word, but do not add extra information
  beyond what the template contains.
""".strip()

_DOMAIN_CONTEXT_TEMPLATE = """
=== Current level ===
ID      : {level_id}
Goal    : {level_goal}

=== Blocks available this level ===
{blocks_available}

=== Key concepts in this level ===
{concepts}

=== Theory ↔ factory bridge ===
{theory_bridge}
""".strip()

_STUDENT_CONTEXT_TEMPLATE = """
=== Student knowledge state ===
{skill_states}

Weakest skills right now: {weakest}
""".strip()


# ── Level metadata (minimal, enough for MVP) ────────────────────────────────

LEVEL_META: dict[str, dict] = {
    "MoveLeftRight": {
        "goal": "Move the robot head left and right along the tape.",
        "blocks": ["move"],
    },
    "PlaceGear": {
        "goal": "Move to a specific cell and write a gear symbol there.",
        "blocks": ["move", "write"],
    },
    "AppendScrew": {
        "goal": "Scan right until blank is found, write a screw there",
        "blocks": ["move", "write", "condition"],
    },
    "ReplaceAllWithNuts": {
        "goal": "Scan the whole tape and replace every piece with a nut.",
        "blocks": ["move", "write", "condition"],
    },
    "RejectIfGearExists": {
        "goal": "Scan the tape and reject immediately if a gear is found; accept at blank.",
        "blocks": ["move", "condition", "accept", "reject"],
    },
    "SwapNutsAndScrews": {
        "goal": "Scan the tape and swap every nut with a screw and vice versa.",
        "blocks": ["move", "write", "condition", "accept", "reject"],
    },
    "PatternRepeated": {
        "goal": "Accept only tapes that consist of complete gear-nut-screw blocks.",
        "blocks": ["move", "condition", "accept", "reject"],
    },
    "BalancedPairs": {
        "goal": "Accept if the tape has equal numbers of gears and nuts, reject otherwise.",
        "blocks": ["move", "write", "condition", "accept", "reject"],
    },
    "PatternSomewhere": {
        "goal": "Accept if the sequence gear-nut-screw appears anywhere on the tape.",
        "blocks": ["move", "write", "condition", "accept", "reject"],
    },
}


# ── Helper builders ─────────────────────────────────────────────────────────

def _build_domain_context(level_id: str) -> str:
    meta   = LEVEL_META.get(level_id, {})
    skills = CONCEPT_MAP.get_skills_for_level(level_id)

    concepts = "\n".join(
        f"- {s.id} {s.name}: {s.description}" for s in skills
    )
    bridge = "\n".join(
        f"- {s.name}: {s.game_mapping}" for s in skills if s.game_mapping
    )
    blocks = ", ".join(meta.get("blocks", []))

    return _DOMAIN_CONTEXT_TEMPLATE.format(
        level_id        = level_id,
        level_goal      = meta.get("goal", ""),
        blocks_available= blocks,
        concepts        = concepts,
        theory_bridge   = bridge,
    )


def _build_student_context(student_id: str, level_id: str) -> str:
    state    = STUDENT_MODEL.knowledge_state(student_id)
    weakest  = STUDENT_MODEL.weakest_skills(student_id, level_id, top_n=3)

    if not state:
        skill_states = "No observations yet — first time in this session."
    else:
        lines = []
        for skill_id, p in sorted(state.items()):
            try:
                name = CONCEPT_MAP.get_skill(skill_id).name
            except KeyError:
                name = skill_id
            mastered = "✓" if p >= float(os.getenv("MASTERY_THRESHOLD","0.80")) else " "
            lines.append(f"  [{mastered}] {skill_id} {name}: P(know)={p:.2f}")
        skill_states = "\n".join(lines)

    weakest_names = []
    for sid in weakest:
        try:
            weakest_names.append(f"{sid} {CONCEPT_MAP.get_skill(sid).name}")
        except KeyError:
            weakest_names.append(sid)

    return _STUDENT_CONTEXT_TEMPLATE.format(
        skill_states = skill_states,
        weakest      = ", ".join(weakest_names) if weakest_names else "none yet",
    )


def _select_hint_tree(
    level_id   : str,
    skill_id   : Optional[str],
    student_id : str,
) -> Optional[tuple[str, int, str]]:
    """
    Returns (skill_id, hint_level_int, filled_template) or None.
    If skill_id is not provided, picks the weakest skill that has a tree.
    """
    if skill_id is None:
        weakest = STUDENT_MODEL.weakest_skills(student_id, level_id, top_n=5)
        skill_id = next(
            (s for s in weakest
             if any(t.level_id == level_id and t.skill_id == s
                    for t in HINT_FOREST.trees)),
            None,
        )
    if skill_id is None:
        return None

    try:
        HINT_FOREST.get_tree(level_id, skill_id)
    except KeyError:
        return None

    hint_int   = STUDENT_MODEL.next_hint_level(student_id, skill_id)
    hint_level = HintLevel(hint_int)
    template   = get_hint_template(level_id, skill_id, hint_level)
    return skill_id, hint_int, template


# ── Public API ───────────────────────────────────────────────────────────────

async def generate_ask_response(
    student_id : str,
    level_id   : str,
    question   : str,
) -> str:
    """
    Handle a free-form question from the student.
    Returns the agent's reply as a plain string.
    """
    system = "\n\n".join([
        _BASE_PERSONA,
        _build_domain_context(level_id),
        _build_student_context(student_id, level_id),
        "The student is asking a question. Answer helpfully but do not "
        "reveal the full circuit solution. Guide, do not solve.",
    ])

    prompt = f"Student question: {question}"
    return await _timed_gemini_generate(
        [system + "\n\n" + prompt],
        "ask",
    )


async def generate_hint_response(
    student_id : str,
    level_id   : str,
    skill_id   : Optional[str] = None,
) -> dict:
    """
    Select and deliver a graduated hint.
    Returns {"reply": str, "skill_id": str, "hint_level": int}.
    """
    result = _select_hint_tree(level_id, skill_id, student_id)

    if result is None:
        return {
            "reply"      : (f"I can see you are working hard on this level, "
                            f"trainee! Try tracing the wire from the energy "
                            f"source step by step and see where it leads."),
            "skill_id"   : None,
            "hint_level" : 0,
        }

    chosen_skill, hint_int, template = result

    system = "\n\n".join([
        _BASE_PERSONA,
        _build_domain_context(level_id),
        _build_student_context(student_id, level_id),
        (
            "The student has asked for a hint. "
            "Expand the hint template below into natural speech in your "
            "factory-worker voice. Do not add more information than the "
            "template contains. Do not reveal the full circuit.\n\n"
            f"Hint template:\n{template}"
        ),
    ])

    response_text = await _timed_gemini_generate([system], "hint")
    return {
        "reply"      : response_text,
        "skill_id"   : chosen_skill,
        "hint_level" : hint_int,
    }


async def generate_event_comment(
    student_id  : str,
    level_id    : str,
    event_type  : str,
    correct     : bool,
    skill_ids   : list[str],
) -> Optional[str]:
    """
    Optionally generate a short reactive comment after a game event.
    Returns None if no comment is warranted (keeps the agent quiet
    most of the time to avoid noise).
    """
    # Only comment on incorrect actions or level completion
    if correct and event_type != "level_complete":
        return None

    if event_type == "level_complete":
        prompt_extra = "The student just completed the level. Congratulate them briefly."
    else:
        prompt_extra = (
            "The student just made an incorrect action. "
            "Give a very short (one sentence) encouraging nudge "
            "without revealing the solution."
        )

    system = "\n\n".join([
        _BASE_PERSONA,
        _build_domain_context(level_id),
        prompt_extra,
    ])

    return await _timed_gemini_generate([system], "event_comment")