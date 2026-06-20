"""
student_model.py

Bayesian Knowledge Tracing (BKT) student model for the ITS.

One BKT state per (student_id, skill_id) pair.
State is kept in memory during the session and persisted to a JSON
file so it survives server restarts.

BKT update equations (Corbett & Anderson, 1995):
  P(know | correct)   = P(know) * (1 - P(slip))
                        ─────────────────────────────────────────────
                        P(know)*(1-P(slip)) + (1-P(know))*P(guess)

  P(know | incorrect) = P(know) * P(slip)
                        ─────────────────────────────────────────
                        P(know)*P(slip) + (1-P(know))*(1-P(guess))

  P(know_next)        = P(know | evidence) + (1 - P(know | evidence)) * P(learn)

Public API:
    model = StudentModel()
    model.observe(student_id, skill_id, correct=True)
    p = model.p_know(student_id, skill_id)
    state = model.knowledge_state(student_id)        # all skills for student
    model.save() / model.load()
"""

from __future__ import annotations

import json
import logging
import os
from pathlib import Path
from typing import Optional

from domain.concepts import CONCEPT_MAP, BKTParameters

_LOG = logging.getLogger("bkt")

# Where to persist state between server restarts
_STATE_PATH = Path(os.getenv("STUDENT_STATE_PATH", "student_state.json"))


class SkillState:
    """Mutable BKT state for one (student, skill) pair."""

    def __init__(self, p_know: float, params: BKTParameters):
        self.p_know  : float        = p_know
        self.params  : BKTParameters = params
        self.attempts: int          = 0
        self.correct : int          = 0
        # consecutive hint requests at the current level (for escalation)
        self.hint_streak: int       = 0
        self.hint_level : int       = 1   # 1=Socratic … 4=Direct

    def observe(self, correct: bool) -> None:
        """Update P(know) given one observation."""
        p  = self.p_know
        ps = self.params.p_slip
        pg = self.params.p_guess
        pt = self.params.p_learn

        if correct:
            p_posterior = (p * (1 - ps)) / (p * (1 - ps) + (1 - p) * pg)
        else:
            p_posterior = (p * ps) / (p * ps + (1 - p) * (1 - pg))

        self.p_know = p_posterior + (1 - p_posterior) * pt
        self.attempts += 1
        if correct:
            self.correct += 1

    def to_dict(self) -> dict:
        return {
            "p_know"      : self.p_know,
            "attempts"    : self.attempts,
            "correct"     : self.correct,
            "hint_streak" : self.hint_streak,
            "hint_level"  : self.hint_level,
        }

    @classmethod
    def from_dict(cls, data: dict, params: BKTParameters) -> "SkillState":
        obj = cls(p_know=data["p_know"], params=params)
        obj.attempts    = data.get("attempts",    0)
        obj.correct     = data.get("correct",     0)
        obj.hint_streak = data.get("hint_streak", 0)
        obj.hint_level  = data.get("hint_level",  1)
        return obj


class StudentModel:
    """
    In-memory BKT model for all students.
    Thread-safe for single-worker FastAPI (asyncio event loop).
    """

    def __init__(self) -> None:
        # { student_id: { skill_id: SkillState } }
        self._store: dict[str, dict[str, SkillState]] = {}
        self._mastery_threshold = float(
            os.getenv("MASTERY_THRESHOLD", "0.80")
        )
        self._hint_escalation_after = int(
            os.getenv("HINT_ESCALATION_AFTER", "1")
        )

    # ------------------------------------------------------------------ #
    # Internal helpers                                                     #
    # ------------------------------------------------------------------ #

    def _get_state(self, student_id: str, skill_id: str) -> SkillState:
        if student_id not in self._store:
            self._store[student_id] = {}
        student = self._store[student_id]
        if skill_id not in student:
            params = CONCEPT_MAP.get_bkt_params(skill_id)
            student[skill_id] = SkillState(
                p_know=params.p_know_0, params=params
            )
        return student[skill_id]

    # ------------------------------------------------------------------ #
    # Public API                                                           #
    # ------------------------------------------------------------------ #

    def observe(
        self,
        student_id: str,
        skill_id  : str,
        correct   : bool,
    ) -> float:
        """
        Record one observation (correct or incorrect) for a skill.
        Returns the updated P(know).
        """
        skill_state = self._get_state(student_id, skill_id)
        p_before = skill_state.p_know
        skill_state.observe(correct)
        p_after = skill_state.p_know
        _LOG.info(
            "bkt_observe %s correct=%s p_know %.4f -> %.4f",
            skill_id,
            correct,
            p_before,
            p_after,
            extra={
                "student_id": student_id,
                "skill_id": skill_id,
                "source": "rest_event",
            },
        )
        # A correct answer resets the hint streak
        if correct:
            skill_state.hint_streak = 0
            skill_state.hint_level = 1
        return skill_state.p_know

    def p_know(self, student_id: str, skill_id: str) -> float:
        """Return current P(know) for a skill. Initialises if unseen."""
        return self._get_state(student_id, skill_id).p_know

    def is_mastered(self, student_id: str, skill_id: str) -> bool:
        return self.p_know(student_id, skill_id) >= self._mastery_threshold

    def knowledge_state(self, student_id: str) -> dict[str, float]:
        """Return {skill_id: p_know} for all skills the student has touched."""
        if student_id not in self._store:
            return {}
        return {
            sid: s.p_know
            for sid, s in self._store[student_id].items()
        }

    def weakest_skills(
        self,
        student_id: str,
        level_id  : str,
        top_n     : int = 3,
    ) -> list[str]:
        """
        Return the top_n skill IDs with the lowest P(know) for the
        given level. Used by the orchestrator to focus hints.
        """
        level_skills = CONCEPT_MAP.get_skills_for_level(level_id)
        scored = [
            (s.id, self.p_know(student_id, s.id))
            for s in level_skills
        ]
        scored.sort(key=lambda x: x[1])
        return [sid for sid, _ in scored[:top_n]]

    def next_hint_level(
        self,
        student_id: str,
        skill_id  : str,
    ) -> int:
        """
        Return the current hint level for this skill and advance the
        streak counter. Auto-escalates after HINT_ESCALATION_AFTER
        consecutive requests.
        """
        state = self._get_state(student_id, skill_id)
        state.hint_streak += 1
        if (
            state.hint_streak >= self._hint_escalation_after
            and state.hint_level < 4
        ):
            state.hint_level  += 1
            state.hint_streak  = 0
        return state.hint_level

    def reset_hint(self, student_id: str, skill_id: str) -> None:
        state = self._get_state(student_id, skill_id)
        state.hint_level  = 1
        state.hint_streak = 0

    # ------------------------------------------------------------------ #
    # Persistence                                                          #
    # ------------------------------------------------------------------ #

    def save(self, path: Optional[Path] = None) -> None:
        target = path or _STATE_PATH
        data = {
            student_id: {
                skill_id: state.to_dict()
                for skill_id, state in skills.items()
            }
            for student_id, skills in self._store.items()
        }
        target.write_text(json.dumps(data, indent=2))

    def load(self, path: Optional[Path] = None) -> None:
        target = path or _STATE_PATH
        if not target.exists():
            return
        raw = target.read_text().strip()
        if not raw:
            return
        try:
            data = json.loads(raw)
        except json.JSONDecodeError:
            _LOG.warning("Ignoring invalid JSON in student state file %s", target)
            return
        for student_id, skills in data.items():
            self._store[student_id] = {}
            for skill_id, state_dict in skills.items():
                try:
                    params = CONCEPT_MAP.get_bkt_params(skill_id)
                    self._store[student_id][skill_id] = SkillState.from_dict(
                        state_dict, params
                    )
                except KeyError:
                    pass  # skill removed from concept map — skip gracefully


# Module-level singleton shared across the FastAPI app
STUDENT_MODEL = StudentModel()