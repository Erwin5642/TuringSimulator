"""Tutor benchmark: load `perguntas.json`, score replies, run the bank."""

from evals.bank import load_bank
from evals.score import ScoreResult, score_reply

__all__ = ["load_bank", "score_reply", "ScoreResult"]
