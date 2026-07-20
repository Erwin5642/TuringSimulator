"""
domain/concepts.py

Concept map for the Turing Machine ITS.
Defines every traceable BKT skill, its metadata, prerequisite graph,
and which game levels introduce / exercise it.

Usage:
    from domain.concepts import CONCEPT_MAP, get_skill, get_prerequisites

    skill = get_skill("S4.1")
    prereqs = get_prerequisites("S4.5")
"""

from __future__ import annotations

from enum import Enum
from typing import Optional
from pydantic import BaseModel, Field


# ---------------------------------------------------------------------------
# Enumerations
# ---------------------------------------------------------------------------

class SkillCluster(str, Enum):
    """Top-level grouping that mirrors the five conceptual areas of the game."""
    INTERFACE   = "interface"       # Grid, wires, ports, tape navigation
    SYMBOL_OPS  = "symbol_ops"      # Reading and writing symbols
    HEAD_MOTION = "head_motion"     # Moving the read/write head
    CONTROL     = "control"         # Condition block, branching, loops
    TM_THEORY   = "tm_theory"       # Halting, accept/reject, languages


class BlockType(str, Enum):
    """The five instruction block types available in the game."""
    MOVE      = "move"
    WRITE     = "write"
    CONDITION = "condition"
    ACCEPT    = "accept"
    REJECT    = "reject"


class DifficultyTier(str, Enum):
    """Coarse difficulty label used by the pedagogical model."""
    FOUNDATIONAL = "foundational"   # Levels 1-2: no condition, no terminals
    SCANNING     = "scanning"       # Levels 3-4: loops and condition
    BRANCHING    = "branching"      # Levels 5-6: reject, multi-condition
    MULTI_STATE  = "multi_state"    # Levels 7-8: pattern / bidirectional
    RECOGNITION  = "recognition"    # Levels 8-9: memory, language


# ---------------------------------------------------------------------------
# Core models
# ---------------------------------------------------------------------------

class BKTParameters(BaseModel):
    """
    Bayesian Knowledge Tracing parameters for a single skill.

    p_know_0  : Prior probability the student already knows this skill
                before any evidence is observed.
    p_learn   : Probability of transitioning from not-knowing to knowing
                after a single learning opportunity (P(T) in BKT).
    p_slip    : Probability of answering incorrectly despite knowing (P(S)).
    p_guess   : Probability of answering correctly without knowing (P(G)).

    Defaults follow published BKT norms for introductory CS concepts
    (Corbett & Anderson, 1995). Slip and guess are elevated for the two
    hardest skill clusters (control / tm_theory) to reflect higher
    error rates observed in novice populations.
    """
    p_know_0 : float = Field(default=0.25, ge=0.0, le=1.0)
    p_learn  : float = Field(default=0.30, ge=0.0, le=1.0)
    p_slip   : float = Field(default=0.10, ge=0.0, le=1.0)
    p_guess  : float = Field(default=0.20, ge=0.0, le=1.0)


class Skill(BaseModel):
    """
    A single traceable knowledge component in the BKT student model.

    id              : Canonical skill identifier (e.g. "S4.1").
                      Format: S{cluster_index}.{skill_index}[a|b]
    name            : Short human-readable label.
    description     : One-sentence description of what the student can do
                      when this skill is mastered.
    cluster         : Which of the five conceptual clusters this belongs to.
    difficulty      : Coarse tier used by the pedagogical model.
    introduced_in   : Level ID where this skill first appears.
    exercised_in    : All level IDs where this skill is practised.
    prerequisites   : Skill IDs that must be sufficiently learned
                      (P(know) >= mastery_threshold) before this skill
                      is expected to be teachable.
    blocks_required : Which game blocks must be available for this skill
                      to be exercisable.
    bkt             : BKT parameter overrides. If None, cluster defaults apply.
    theory_mapping  : The formal TM concept this skill corresponds to,
                      expressed in plain language for the agent to use
                      when bridging game mechanics to theory.
    game_mapping    : How this concept manifests in the game's factory theme.
    """
    id              : str
    name            : str
    description     : str
    cluster         : SkillCluster
    difficulty      : DifficultyTier
    introduced_in   : str                       # level ID, e.g. "MoveLeftRight"
    exercised_in    : list[str]                 # level IDs
    prerequisites   : list[str] = Field(default_factory=list)
    blocks_required : list[BlockType] = Field(default_factory=list)
    bkt             : Optional[BKTParameters] = None
    theory_mapping  : str = ""
    game_mapping    : str = ""


# ---------------------------------------------------------------------------
# BKT parameter presets per cluster
# (used when a skill has no override)
# ---------------------------------------------------------------------------

CLUSTER_BKT_DEFAULTS: dict[SkillCluster, BKTParameters] = {
    SkillCluster.INTERFACE:   BKTParameters(p_know_0=0.30, p_learn=0.40, p_slip=0.08, p_guess=0.20),
    SkillCluster.SYMBOL_OPS:  BKTParameters(p_know_0=0.25, p_learn=0.35, p_slip=0.10, p_guess=0.20),
    SkillCluster.HEAD_MOTION: BKTParameters(p_know_0=0.30, p_learn=0.40, p_slip=0.08, p_guess=0.20),
    SkillCluster.CONTROL:     BKTParameters(p_know_0=0.15, p_learn=0.25, p_slip=0.15, p_guess=0.20),
    SkillCluster.TM_THEORY:   BKTParameters(p_know_0=0.10, p_learn=0.20, p_slip=0.18, p_guess=0.15),
}


# ---------------------------------------------------------------------------
# Skill definitions
# ---------------------------------------------------------------------------

_SKILLS: list[Skill] = [

    # ── S1 · Interface ──────────────────────────────────────────────────────

    Skill(
        id="S1.1",
        name="Place wire",
        description="Student can place a wire between two ports on the grid.",
        cluster=SkillCluster.INTERFACE,
        difficulty=DifficultyTier.FOUNDATIONAL,
        introduced_in="MoveLeftRight",
        exercised_in=["MoveLeftRight", "PlaceGear",
                      "ReplaceAllWithNuts", "RejectIfGearExists",
                      "SwapNutsAndScrews", "PatternRepeated",
                      "BalancedPairs", "PatternSomewhere"],
        theory_mapping="Wires are the transition function δ — they define "
                       "which instruction executes after the current one.",
        game_mapping="In the factory, wires carry the activation signal "
                     "between instruction blocks on the grid.",
    ),

    Skill(
        id="S1.2",
        name="Connect port",
        description="Student can identify input and output ports and connect "
                    "them correctly.",
        cluster=SkillCluster.INTERFACE,
        difficulty=DifficultyTier.FOUNDATIONAL,
        introduced_in="MoveLeftRight",
        exercised_in=["MoveLeftRight", "PlaceGear"],
        prerequisites=["S1.1"],
        theory_mapping="Input/output ports correspond to the directionality "
                       "of state transitions in a TM.",
        game_mapping="Each instruction block exposes ports: one input "
                     "(receives activation) and one or two outputs "
                     "(passes activation on).",
    ),

    Skill(
        id="S1.3",
        name="Tape position awareness",
        description="Student understands that the head occupies exactly one "
                    "cell at a time and tracks its position mentally.",
        cluster=SkillCluster.INTERFACE,
        difficulty=DifficultyTier.FOUNDATIONAL,
        introduced_in="MoveLeftRight",
        exercised_in=["MoveLeftRight", "PlaceGear",
                      "ReplaceAllWithNuts", "RejectIfGearExists"],
        prerequisites=["S1.1"],
        theory_mapping="Head position corresponds to the tape head in the "
                       "formal TM 7-tuple.",
        game_mapping="The robot on the factory conveyor occupies one slot "
                     "at a time. Moving it changes which piece it can act on.",
    ),

    Skill(
        id="S1.4",
        name="Blank as tape terminator",
        description="Student recognises that a blank cell signals the end of "
                    "the tape and uses it to exit loops.",
        cluster=SkillCluster.INTERFACE,
        difficulty=DifficultyTier.SCANNING,
        introduced_in="ReplaceAllWithNuts",
        exercised_in=["ReplaceAllWithNuts",
                      "RejectIfGearExists", "SwapNutsAndScrews",
                      "PatternRepeated", "BalancedPairs", "PatternSomewhere"],
        prerequisites=["S1.3", "S4.1"],
        theory_mapping="In a formal TM, the blank symbol (⊔) marks cells "
                       "that have never been written — effectively the "
                       "boundary of the input.",
        game_mapping="An empty factory slot means the conveyor belt has "
                     "ended. The robot must detect it to know when to stop.",
    ),

    # ── S2 · Symbol operations ───────────────────────────────────────────────

    Skill(
        id="S2.1",
        name="Identify symbol",
        description="Student can name which factory piece (gear, nut, screw, "
                    "blank) is at the current head position.",
        cluster=SkillCluster.SYMBOL_OPS,
        difficulty=DifficultyTier.FOUNDATIONAL,
        introduced_in="PlaceGear",
        exercised_in=["PlaceGear", "ReplaceAllWithNuts",
                      "RejectIfGearExists", "SwapNutsAndScrews"],
        prerequisites=["S1.3"],
        theory_mapping="Symbol identification is the read operation of the "
                       "TM head — the precondition for every transition.",
        game_mapping="The robot looks at the piece currently under it. "
                     "Only condition blocks can perform this inspection.",
    ),

    Skill(
        id="S2.2",
        name="Use write block",
        description="Student can place a write block configured for a target "
                    "symbol and wire it correctly into a circuit.",
        cluster=SkillCluster.SYMBOL_OPS,
        difficulty=DifficultyTier.FOUNDATIONAL,
        introduced_in="PlaceGear",
        exercised_in=["PlaceGear", "ReplaceAllWithNuts",
                      "SwapNutsAndScrews", "BalancedPairs", "PatternSomewhere"],
        prerequisites=["S1.2", "S3.1"],
        blocks_required=[BlockType.WRITE],
        theory_mapping="The write block corresponds to the write symbol "
                       "component of a TM transition: δ(q,a) → (q', b, d) "
                       "where b is the symbol written.",
        game_mapping="The stamping machine on the factory floor replaces "
                     "whatever piece is currently under the robot with a "
                     "chosen piece.",
    ),

    Skill(
        id="S2.3",
        name="Write as memory (marker)",
        description="Student deliberately writes a marker symbol to record "
                    "information for a later pass.",
        cluster=SkillCluster.SYMBOL_OPS,
        difficulty=DifficultyTier.MULTI_STATE,
        introduced_in="BalancedPairs",
        exercised_in=["BalancedPairs", "PatternSomewhere"],
        prerequisites=["S2.2", "S4.5"],
        blocks_required=[BlockType.WRITE, BlockType.CONDITION],
        bkt=BKTParameters(p_know_0=0.05, p_learn=0.20, p_slip=0.18,
                          p_guess=0.12),
        theory_mapping="Using the tape as working memory is one of the core "
                       "powers of a Turing Machine — it separates TMs from "
                       "finite automata, which have no external memory.",
        game_mapping="Stamping a special mark on a slot is the robot's way "
                     "of leaving a note to itself for the next pass through "
                     "the conveyor.",
    ),

    # ── S3 · Head motion ─────────────────────────────────────────────────────

    Skill(
        id="S3.1",
        name="Move left and right",
        description="Student can place and configure move blocks to shift the "
                    "head one cell left or right.",
        cluster=SkillCluster.HEAD_MOTION,
        difficulty=DifficultyTier.FOUNDATIONAL,
        introduced_in="MoveLeftRight",
        exercised_in=["MoveLeftRight", "PlaceGear",
                      "ReplaceAllWithNuts", "RejectIfGearExists",
                      "SwapNutsAndScrews", "PatternRepeated",
                      "BalancedPairs", "PatternSomewhere"],
        blocks_required=[BlockType.MOVE],
        theory_mapping="Head movement corresponds to the direction component "
                       "d ∈ {L, R} of a TM transition δ(q,a) → (q', b, d).",
        game_mapping="The robot slides left or right along the conveyor belt "
                     "by one slot per move block.",
    ),

    Skill(
        id="S3.2",
        name="Chain move with action",
        description="Student sequences a move block before a write or "
                    "condition block to act at the correct position.",
        cluster=SkillCluster.HEAD_MOTION,
        difficulty=DifficultyTier.FOUNDATIONAL,
        introduced_in="PlaceGear",
        exercised_in=["PlaceGear", "ReplaceAllWithNuts"],
        prerequisites=["S3.1", "S2.2"],
        blocks_required=[BlockType.MOVE],
        theory_mapping="In a TM, movement and symbol operations happen "
                       "atomically per transition. In the game they are "
                       "separate blocks — the student must order them "
                       "correctly to reproduce the same effect.",
        game_mapping="The robot must be in the right position before it can "
                     "stamp or inspect a piece. Position first, act second.",
    ),

    # ── S4 · Control flow ────────────────────────────────────────────────────

    Skill(
        id="S4.1",
        name="Condition block",
        description="Student can place a condition block, configure it for a "
                    "target symbol, and wire both output ports.",
        cluster=SkillCluster.CONTROL,
        difficulty=DifficultyTier.SCANNING,
        introduced_in="ReplaceAllWithNuts",
        exercised_in=["ReplaceAllWithNuts",
                      "RejectIfGearExists", "SwapNutsAndScrews",
                      "PatternRepeated", "BalancedPairs", "PatternSomewhere"],
        prerequisites=["S1.2", "S2.1"],
        blocks_required=[BlockType.CONDITION],
        theory_mapping="The condition block is the symbol-matching part of "
                       "the TM transition function δ(q, a). The true port "
                       "fires when the read symbol matches; the false port "
                       "fires otherwise.",
        game_mapping="The inspector at the factory gate checks what piece is "
                     "under the robot. It always routes the signal one of two "
                     "ways — never skips the check.",
    ),

    Skill(
        id="S4.2",
        name="Branch logic",
        description="Student wires the two outputs of a condition block to "
                    "distinct, meaningful sequences of instructions.",
        cluster=SkillCluster.CONTROL,
        difficulty=DifficultyTier.SCANNING,
        introduced_in="ReplaceAllWithNuts",
        exercised_in=["ReplaceAllWithNuts",
                      "RejectIfGearExists", "SwapNutsAndScrews",
                      "PatternRepeated", "BalancedPairs", "PatternSomewhere"],
        prerequisites=["S4.1"],
        blocks_required=[BlockType.CONDITION],
        theory_mapping="Branching on a symbol is how a TM selects which "
                       "transition rule to apply — different symbols at the "
                       "same state lead to different transitions.",
        game_mapping="Depending on what the inspector finds, the conveyor "
                     "either routes the part for processing or sends it "
                     "down a different path.",
    ),

    Skill(
        id="S4.3",
        name="Chain all five blocks",
        description="Student constructs circuits that use all five block "
                    "types in a single coherent program.",
        cluster=SkillCluster.CONTROL,
        difficulty=DifficultyTier.BRANCHING,
        introduced_in="SwapNutsAndScrews",
        exercised_in=["SwapNutsAndScrews", "PatternRepeated",
                      "BalancedPairs", "PatternSomewhere"],
        prerequisites=["S4.2", "S5.2b"],
        blocks_required=[BlockType.MOVE, BlockType.WRITE, BlockType.CONDITION,
                         BlockType.ACCEPT, BlockType.REJECT],
        theory_mapping="A complete TM program uses all transition types: "
                       "movement, symbol rewriting, conditional branching, "
                       "and both terminal states.",
        game_mapping="The full factory pipeline: the robot moves, stamps, "
                     "inspects, approves, and rejects — all in one circuit.",
    ),

    Skill(
        id="S4.4",
        name="Multi-state program",
        description="Student designs circuits where different parts of the "
                    "program represent distinct logical phases of execution.",
        cluster=SkillCluster.CONTROL,
        difficulty=DifficultyTier.MULTI_STATE,
        introduced_in="PatternRepeated",
        exercised_in=["PatternRepeated", "BalancedPairs", "PatternSomewhere"],
        prerequisites=["S4.3", "S4.5"],
        blocks_required=[BlockType.CONDITION, BlockType.MOVE],
        bkt=BKTParameters(p_know_0=0.08, p_learn=0.18, p_slip=0.20,
                          p_guess=0.15),
        theory_mapping="States in a TM encode the machine's 'memory' of "
                       "what phase of computation it is in. Different parts "
                       "of the circuit correspond to different TM states.",
        game_mapping="The factory has different operating modes — scanning "
                     "mode, matching mode, return mode. Each phase of the "
                     "circuit is one such mode.",
    ),

    Skill(
        id="S4.5",
        name="Loop construction",
        description="Student connects an output port back to an earlier block "
                    "to create a scanning loop that repeats until a condition "
                    "is met.",
        cluster=SkillCluster.CONTROL,
        difficulty=DifficultyTier.SCANNING,
        introduced_in="ReplaceAllWithNuts",
        exercised_in=["ReplaceAllWithNuts",
                      "RejectIfGearExists", "SwapNutsAndScrews",
                      "PatternRepeated", "BalancedPairs", "PatternSomewhere"],
        prerequisites=["S4.2"],
        blocks_required=[BlockType.CONDITION, BlockType.MOVE],
        bkt=BKTParameters(p_know_0=0.10, p_learn=0.22, p_slip=0.18,
                          p_guess=0.18),
        theory_mapping="Loops in the circuit correspond to cycles in the TM "
                       "state diagram — states that transition back to "
                       "themselves or to earlier states.",
        game_mapping="The robot keeps moving down the conveyor and looping "
                     "back until it finds what it is looking for — like a "
                     "quality inspector walking the line repeatedly.",
    ),

    # ── S5 · TM theory ───────────────────────────────────────────────────────

    Skill(
        id="S5.1",
        name="Halting",
        description="Student understands that every execution path must "
                    "eventually reach a terminal block, and that the machine "
                    "stops permanently at that point.",
        cluster=SkillCluster.TM_THEORY,
        difficulty=DifficultyTier.BRANCHING,
        introduced_in="RejectIfGearExists",
        exercised_in=["RejectIfGearExists", "SwapNutsAndScrews",
                      "PatternRepeated", "BalancedPairs", "PatternSomewhere"],
        prerequisites=["S4.2", "S5.2a"],
        theory_mapping="A TM halts when it enters an accept or reject state "
                       "and has no further transitions defined. The halting "
                       "problem asks whether we can always predict this — "
                       "and it is undecidable in general.",
        game_mapping="When the robot reaches an accept or reject block, the "
                     "factory line stops. There is no going back.",
    ),

    Skill(
        id="S5.2a",
        name="Accept block",
        description="Student uses the accept block as a deliberate positive "
                    "terminal state that the program is designed to reach.",
        cluster=SkillCluster.TM_THEORY,
        difficulty=DifficultyTier.SCANNING,
        introduced_in="RejectIfGearExists",
        exercised_in=["ReplaceAllWithNuts",
                      "RejectIfGearExists", "SwapNutsAndScrews",
                      "PatternRepeated", "BalancedPairs", "PatternSomewhere"],
        prerequisites=["S4.1"],
        blocks_required=[BlockType.ACCEPT],
        theory_mapping="The accept state q_accept is one of two halting "
                       "states in the formal TM 7-tuple. Entering it means "
                       "the input string is in the language being recognised.",
        game_mapping="The green approval stamp at the end of the factory "
                     "line — the robot reaches it when the batch passes "
                     "quality control.",
    ),

    Skill(
        id="S5.2b",
        name="Accept vs reject distinction",
        description="Student deliberately chooses between accept and reject "
                    "based on what was found on the tape, understanding they "
                    "are semantically opposite outcomes.",
        cluster=SkillCluster.TM_THEORY,
        difficulty=DifficultyTier.BRANCHING,
        introduced_in="RejectIfGearExists",
        exercised_in=["RejectIfGearExists", "SwapNutsAndScrews",
                      "PatternRepeated", "BalancedPairs", "PatternSomewhere"],
        prerequisites=["S5.2a", "S4.2"],
        blocks_required=[BlockType.ACCEPT, BlockType.REJECT],
        theory_mapping="q_accept and q_reject are the two halting states. "
                       "A TM that accepts all inputs is not useful — the "
                       "reject state is what gives the machine discriminating "
                       "power.",
        game_mapping="Some batches pass, some fail. The robot must be wired "
                     "to reach the red rejection bin when the batch is "
                     "defective.",
    ),

    Skill(
        id="S5.3",
        name="Language recognition",
        description="Student understands that a TM program defines a set of "
                    "accepted tapes (a language) and can reason about which "
                    "inputs will be accepted or rejected.",
        cluster=SkillCluster.TM_THEORY,
                difficulty=DifficultyTier.RECOGNITION,
        introduced_in="PatternRepeated",
        exercised_in=["PatternRepeated", "BalancedPairs", "PatternSomewhere"],
        prerequisites=["S5.2b", "S4.4"],
        bkt=BKTParameters(p_know_0=0.05, p_learn=0.15, p_slip=0.20,
                          p_guess=0.12),
        theory_mapping="A TM recognises a language L if it accepts every "
                       "string in L and rejects every string not in L. "
                       "The class of languages recognisable this way is "
                       "called Turing-recognisable (recursively enumerable).",
        game_mapping="The factory robot's circuit defines exactly which "
                     "batches are good and which are defective. The set of "
                     "all good batches is the language the machine recognises.",
    ),
]


# ---------------------------------------------------------------------------
# Concept map — the public interface
# ---------------------------------------------------------------------------

class ConceptMap(BaseModel):
    """
    The complete concept map for the ITS domain model.
    Loaded once at server startup and queried by the orchestrator.
    """
    skills: list[Skill]

    # ------------------------------------------------------------------ #
    # Lookup helpers                                                       #
    # ------------------------------------------------------------------ #

    def get_skill(self, skill_id: str) -> Skill:
        for s in self.skills:
            if s.id == skill_id:
                return s
        raise KeyError(f"Skill '{skill_id}' not found in concept map.")

    def get_prerequisites(self, skill_id: str) -> list[Skill]:
        """Return the full Skill objects for all direct prerequisites."""
        skill = self.get_skill(skill_id)
        return [self.get_skill(pid) for pid in skill.prerequisites]

    def get_skills_for_level(self, level_id: str) -> list[Skill]:
        """Return all skills exercised in a given level."""
        return [s for s in self.skills if level_id in s.exercised_in]

    def get_introduced_skills(self, level_id: str) -> list[Skill]:
        """Return only the skills first introduced in a given level."""
        return [s for s in self.skills if s.introduced_in == level_id]

    def get_skills_by_cluster(self, cluster: SkillCluster) -> list[Skill]:
        return [s for s in self.skills if s.cluster == cluster]

    def get_bkt_params(self, skill_id: str) -> BKTParameters:
        """
        Return effective BKT parameters for a skill.
        Uses the skill's own override if present, else cluster defaults.
        """
        skill = self.get_skill(skill_id)
        if skill.bkt is not None:
            return skill.bkt
        return CLUSTER_BKT_DEFAULTS[skill.cluster]

    def prerequisite_graph(self) -> dict[str, list[str]]:
        """
        Return the full prerequisite graph as an adjacency dict.
        Keys are skill IDs; values are lists of prerequisite skill IDs.
        """
        return {s.id: s.prerequisites for s in self.skills}

    def topological_order(self) -> list[str]:
        """
        Return skill IDs in a topological order (prerequisites before
        dependents). Raises ValueError on cyclic dependencies.
        """
        graph = self.prerequisite_graph()
        visited: set[str] = set()
        order: list[str] = []

        def visit(sid: str, ancestors: set[str]) -> None:
            if sid in ancestors:
                raise ValueError(f"Cyclic prerequisite detected at '{sid}'.")
            if sid in visited:
                return
            ancestors.add(sid)
            for pre in graph.get(sid, []):
                visit(pre, ancestors)
            ancestors.discard(sid)
            visited.add(sid)
            order.append(sid)

        for skill in self.skills:
            visit(skill.id, set())

        return order


# ---------------------------------------------------------------------------
# Module-level singleton — import this in other modules
# ---------------------------------------------------------------------------

CONCEPT_MAP = ConceptMap(skills=_SKILLS)


# ---------------------------------------------------------------------------
# Convenience functions (thin wrappers around the singleton)
# ---------------------------------------------------------------------------

def get_skill(skill_id: str) -> Skill:
    return CONCEPT_MAP.get_skill(skill_id)

def get_prerequisites(skill_id: str) -> list[Skill]:
    return CONCEPT_MAP.get_prerequisites(skill_id)

def get_bkt_params(skill_id: str) -> BKTParameters:
    return CONCEPT_MAP.get_bkt_params(skill_id)

def get_skills_for_level(level_id: str) -> list[Skill]:
    return CONCEPT_MAP.get_skills_for_level(level_id)