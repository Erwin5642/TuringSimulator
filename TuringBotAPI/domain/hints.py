"""
domain/hints.py  —  Hint trees for the Turing Machine ITS.
"""
from __future__ import annotations
import re, sys, types

# allow running without pydantic installed
try:
    from pydantic import BaseModel, Field
except ImportError:
    class Field:
        def __new__(cls, default=None, **kw): return default
    class BaseModel:
        def __init__(self, **kw):
            for k, v in kw.items(): setattr(self, k, v)

from enum import IntEnum
from typing import Optional


class HintLevel(IntEnum):
    SOCRATIC   = 1
    CONCEPTUAL = 2
    PARTIAL    = 3
    DIRECT     = 4


class HintStep(BaseModel):
    level        : HintLevel
    template     : str
    placeholders : list = []

class HintTree(BaseModel):
    level_id : str
    skill_id : str
    context  : str
    steps    : list

    def get_step(self, level):
        for s in self.steps:
            if s.level == level: return s
        raise KeyError(f"HintLevel {level} not in ({self.level_id},{self.skill_id})")

class HintForest(BaseModel):
    trees: list

    def get_tree(self, level_id, skill_id):
        for t in self.trees:
            if t.level_id == level_id and t.skill_id == skill_id: return t
        raise KeyError(f"No tree for level='{level_id}' skill='{skill_id}'")

    def get_hint(self, level_id, skill_id, level):
        return self.get_tree(level_id, skill_id).get_step(level)

    def trees_for_level(self, level_id):
        return [t for t in self.trees if t.level_id == level_id]

    def trees_for_skill(self, skill_id):
        return [t for t in self.trees if t.skill_id == skill_id]


def _tree(level_id, skill_id, context, socratic, conceptual, partial, direct, placeholders=None):
    def ph(tmpl):
        return sorted(set(re.findall(r'\{(\w+)\}', tmpl)))
    return HintTree(
        level_id=level_id, skill_id=skill_id, context=context,
        steps=[
            HintStep(level=HintLevel.SOCRATIC,   template=socratic,   placeholders=ph(socratic)),
            HintStep(level=HintLevel.CONCEPTUAL, template=conceptual, placeholders=ph(conceptual)),
            HintStep(level=HintLevel.PARTIAL,    template=partial,    placeholders=ph(partial)),
            HintStep(level=HintLevel.DIRECT,     template=direct,     placeholders=ph(direct)),
        ]
    )


_TREES = [

    # ── Level 1: MoveLeftRight ────────────────────────────────────────────

    _tree("MoveLeftRight","S3.1",
        "Student does not know how to place or configure a move block.",
        "The robot needs to travel along the conveyor belt. What kind of block do you think controls where it goes?",
        "The move block shifts the robot one slot in a chosen direction. You pick whether it moves left or right when you place it.",
        "Drag a move block onto the grid, set its direction to right, and connect its input port to the energy source.",
        "Place a move-right block and wire the energy source output to its input port. Add a second move-right block and chain them: source → move-right → move-right. The robot will advance two slots.",
    ),

    _tree("MoveLeftRight","S1.2",
        "Student has placed a block but does not know how to wire it.",
        "You have a block on the grid. How do you think the factory knows in what order to run them?",
        "Blocks communicate through ports. Every block has an input port that receives the activation signal and an output port that passes it on. Wires connect them in sequence.",
        "Click the output port of the energy source and drag a wire to the input port of your first move block.",
        "The energy source has a single output port on its right side. Click it, drag to the small circle on the left of the move block, and release. A wire appears connecting them.",
    ),

    _tree("MoveLeftRight","S1.3",
        "Student does not understand that the number of move blocks controls how far the head travels.",
        "Where is the robot standing right now? Where does it need to be when the program finishes?",
        "The robot always stands on exactly one cell. Each move block shifts it by exactly one cell. To move two cells right you need two move-right blocks in sequence.",
        "Count the cells between the robot start and the target. You need that many move blocks chained together.",
        "The robot starts at cell {start_cell} and must reach cell {target_cell} — that is {steps} step(s) to the right. Place {steps} move-right block(s) wired in sequence.",
        placeholders=["start_cell","target_cell","steps"],
    ),

    # ── Level 2: PlaceGear ────────────────────────────────────────────────

    _tree("PlaceGear","S2.2",
        "Student does not know how to place or configure the write block.",
        "You have moved the robot to the right cell. Now you want to change the piece there. Which block do you think performs that action?",
        "The write block replaces whatever piece is currently under the robot with a piece of your choosing. You configure which piece it writes when you place it.",
        "Drag a write block onto the grid after the last move block. Set it to write a gear, then wire the move block output to the write block input.",
        "Place a write block, open its settings, and select 'gear'. Wire: last move block → write block. When the program runs, the robot will stamp a gear onto cell {target_cell}.",
        placeholders=["target_cell"],
    ),

    _tree("PlaceGear","S3.2",
        "Student places the write block before the move blocks, acting on the wrong cell.",
        "The robot wrote a gear, but not on the right cell. Does the robot decide where to write before or after it moves?",
        "The write block acts on whichever cell the robot currently occupies. If you write before moving, you change the starting cell, not the target. Position the robot first, then write.",
        "Make sure all move blocks appear before the write block in the circuit. The robot must reach the target cell first.",
        "Reconnect the circuit in this order: energy source → move-right → move-right → write(gear). The robot will travel to cell {target_cell} before stamping.",
        placeholders=["target_cell"],
    ),

    # ── Level 3: AppendScrew ──────────────────────────────────────────────

    _tree("AppendScrew","S4.1",
        "Student does not understand what the condition block does or how to configure it.",
        "The tape has different pieces and the robot must react differently depending on what it finds. How could the circuit know what is currently under the robot?",
        "The condition block is the only way to inspect the tape. You configure it with a target symbol — for example, blank. If the piece under the robot matches, the signal exits the true port; otherwise the false port.",
        "Place a condition block configured to check for blank. Wire your last move block output to this condition block input. Now wire both output ports — true and false — to something.",
        "Place a condition block set to 'blank'. Wire: move → condition. True port → write(screw) → accept. False port → back to move (the loop).",
    ),

    _tree("AppendScrew","S4.5",
        "Student does not know how to create a loop by feeding an output back to an earlier block.",
        "After the robot checks a cell and finds it is not blank, what should it do next? Can a wire go backward in the circuit?",
        "A loop is just a wire from an output port back to a block that already ran. When the condition false port fires, wiring it back to the move block makes the robot move and check again — repeating until it finds the blank.",
        "Connect the false output of the condition block back to the input of the move-right block. The circuit will repeat: move → condition → (false) → move → …",
        "Wire condition(false) → move-right input. This creates the scan loop. The robot moves right, checks for blank, and if not found it moves again. It stops only when blank is reached.",
    ),

    _tree("AppendScrew","S4.2",
        "Student has wired only one output port of the condition block.",
        "Your condition block has two output ports. What happens to the signal when it reaches a port with no wire?",
        "Both output ports of a condition block must always be wired. The true port fires on a match; the false port fires otherwise. Leaving one unwired means that branch has nowhere to go and the program gets stuck.",
        "Check that both the true and false ports of your condition block have wires. True should lead toward write and accept; false should loop back to move.",
        "Wire condition true → write(screw) → accept. Wire condition false → move-right → back to condition. Both ports must be connected before the program can run.",
    ),

    _tree("AppendScrew","S5.2a",
        "Student has not placed an accept block or does not know where to connect it.",
        "Once the robot writes the screw, is the job done? How does the program signal that everything finished successfully?",
        "The accept block is the factory approval stamp. When the signal reaches it, the program halts and marks the result as passed. Without accept, the program has no way to declare success.",
        "Place an accept block after the write block. Wire: write → accept.",
        "After the write(screw) block add an accept block and wire write(screw) → accept. The program halts successfully when the robot writes the screw and reaches accept.",
    ),

    _tree("AppendScrew","S1.4",
        "Student does not understand that a blank cell marks the end of the tape.",
        "The tape has pieces on it but eventually runs out. How do you think the robot knows it has reached the end?",
        "An empty cell — a blank — marks the end of the tape. The conveyor belt ends with an empty slot. Your condition block can detect blank just like any other piece.",
        "Set your condition block to check for 'blank'. When it finds one, the robot has passed all pieces and reached the tape end.",
        "Configure the condition block to check for 'blank'. Wire its true port to write(screw) → accept. The robot scans right until it finds the empty slot, then writes the screw there and accepts.",
    ),

    # ── Level 4: ReplaceAllWithNuts ───────────────────────────────────────

    _tree("ReplaceAllWithNuts","S4.5",
        "Student tries to handle each cell with individual blocks instead of a loop.",
        "The tape has an unknown number of pieces. How many move blocks would you need to handle each cell one by one? Is there a better way?",
        "When the tape length is unknown you cannot count blocks in advance. A loop handles any length automatically: move, write, check blank, and if not blank, loop back and repeat.",
        "Build one iteration: move → write(nut) → condition(blank). Wire condition false back to the move block. Wire condition true to accept.",
        "Full loop: move-right → write(nut) → condition(blank). condition false → back to move-right. condition true → accept. The loop repeats for every cell and exits when blank is found.",
    ),

    _tree("ReplaceAllWithNuts","S2.2",
        "Student has the loop but the write block is not inside it — pieces are not being replaced.",
        "Your robot is scanning the tape but the pieces are not changing. At which point in the loop should the replacement happen?",
        "The write block acts on the current cell at the moment it fires. For replacement to happen at every cell, the write block must be inside the loop — between move and the condition check.",
        "Place write(nut) between the move block and the condition block. Wire: move → write(nut) → condition(blank).",
        "Insert write(nut) between move and condition: move-right → write(nut) → condition(blank). Every loop iteration will overwrite the current piece with a nut before checking for blank.",
    ),

    _tree("ReplaceAllWithNuts","S4.2",
        "Student checks for blank before writing, so the write block is on the wrong branch.",
        "Your robot checks for blank before writing. On the very first cell — is it blank? Does the write block ever get to run?",
        "Order matters. If blank is checked first, non-blank cells exit through the false port. The write must happen on that false branch before looping back.",
        "Move write(nut) onto the false branch of condition(blank). Circuit: condition(blank) false → write(nut) → move-right → back to condition.",
        "Restructure: condition(blank) at top. true → accept. false → write(nut) → move-right → back to condition(blank). The robot checks first, writes only on non-blank cells, then advances.",
    ),

    # ── Level 5: RejectIfGearExists ───────────────────────────────────────

    _tree("RejectIfGearExists","S5.2b",
        "Student wires the gear-found branch back into the loop instead of to reject.",
        "When the robot finds a gear, should the factory keep running or stop immediately? What should happen to this batch?",
        "Reject is a terminal block. Once reached the program stops permanently and marks the batch as failed. Finding a gear means the batch is defective — stop right away.",
        "Place a reject block and wire condition(gear) true → reject. Do not put any move or loop between them.",
        "condition(gear) true → reject. condition(gear) false → condition(blank). condition(blank) true → accept. condition(blank) false → move-right → back to condition(gear).",
    ),

    _tree("RejectIfGearExists","S5.1",
        "Student does not understand that accept and reject both permanently halt the program.",
        "What is the difference between the robot reaching accept versus reaching reject? Can the program keep going after either?",
        "Both accept and reject are terminal states — the program stops the moment it reaches either one. Accept means the batch passed; reject means it failed. Neither continues execution.",
        "Make sure both accept and reject have no output wires. They are always endpoints in your circuit.",
        "Accept and reject blocks have only an input port, no output. Your circuit must have exactly two endpoints: accept (reached on blank) and reject (reached on gear).",
    ),

    _tree("RejectIfGearExists","S4.5",
        "Student rejects on the first cell without scanning — does not account for gear appearing anywhere.",
        "The gear might not be on the first cell. How can the robot check every cell, not just the first one?",
        "A loop lets the robot visit every cell in sequence. The condition fires on the current cell and branches. If not a gear the robot moves and checks the next cell. Only a gear — or blank — ends the loop.",
        "condition(gear) false → condition(blank). condition(blank) false → move-right → back to condition(gear).",
        "condition(gear): true → reject. false → condition(blank): true → accept. false → move-right → back to condition(gear). The robot scans every cell and rejects only when a gear is found.",
    ),

    # ── Level 6: SwapNutsAndScrews ────────────────────────────────────────

    _tree("SwapNutsAndScrews","S4.2",
        "Student uses a single condition block and cannot handle both nut and screw cases.",
        "Your condition block checks for one symbol at a time, but the tape has both nuts and screws. How could you handle each case separately?",
        "You can chain condition blocks. The first checks for nut — true writes a screw. False leads to the second check for screw — true writes a nut. The second block's false port handles blank.",
        "Place two condition blocks in the loop: condition(nut) first, condition(screw) on the false branch. Each true port leads to its own write block.",
        "condition(nut) true → write(screw) → move → back to top. condition(nut) false → condition(screw). condition(screw) true → write(nut) → move → back to top. condition(screw) false → condition(blank). condition(blank) true → accept. condition(blank) false → move → back to top.",
    ),

    _tree("SwapNutsAndScrews","S2.2",
        "Student writes the same symbol on both branches instead of swapping them.",
        "Both write blocks are writing the same piece. What should each one write to make the swap work correctly?",
        "Swapping means each symbol becomes the other. The write triggered by a nut should produce a screw, and the write triggered by a screw should produce a nut.",
        "Check each write block's settings. Nut-found branch → write set to 'screw'. Screw-found branch → write set to 'nut'.",
        "Write block on the nut branch: set to 'screw'. Write block on the screw branch: set to 'nut'. Each write fires only when its matching condition is true, so the replacements never conflict.",
    ),

    _tree("SwapNutsAndScrews","S4.3",
        "Student is overwhelmed by the circuit size and does not know how to start.",
        "This circuit needs several blocks. Instead of placing them all at once, which single block should run first when the program starts?",
        "Build in layers. Start with just the nut case: condition(nut) → write(screw) → move → accept. Once that runs, add the screw case on the false branch, then replace accept with the loop-back.",
        "Start: energy source → condition(nut). condition(nut) true → write(screw) → move → back to condition(nut). condition(nut) false → condition(screw). Build from there.",
        "energy source → condition(nut). nut true → write(screw) → move-right → back to condition(nut). nut false → condition(screw). screw true → write(nut) → move-right → back to condition(nut). screw false → condition(blank). blank true → accept. blank false → move-right → back to condition(nut).",
    ),

    # ── Level 7: PatternRepeated ──────────────────────────────────────────

    _tree("PatternRepeated","S4.4",
        "Student uses a single loop but cannot track position within the gear-nut-screw cycle.",
        "The pattern has three steps: gear, nut, screw. After verifying screw and moving on, what should the robot expect to find next?",
        "Tracking position within a repeating pattern is a form of state. Different parts of the circuit represent different phases: 'expecting gear', 'expecting nut', 'expecting screw'. Each phase has its own condition block and its own reject path.",
        "Build three phases in sequence. Phase 1: condition(gear) — true → phase 2, false → reject (or accept if blank). Phase 2: condition(nut) — true → phase 3, false → reject. Phase 3: condition(screw) — true → phase 1, false → reject.",
        "Phase 1: condition(blank) true → accept. condition(blank) false → condition(gear) true → move → phase 2. condition(gear) false → reject. Phase 2: condition(nut) true → move → phase 3. false → reject. Phase 3: condition(screw) true → move → phase 1. false → reject.",
    ),

    _tree("PatternRepeated","S5.3",
        "Student does not understand what it means for a program to accept or reject based on a pattern.",
        "Imagine every possible tape. Which ones should your program accept? What do they all have in common?",
        "Your program defines a language — the set of all tapes it accepts. Here that language is: any tape of complete gear-nut-screw blocks followed by blank. Every tape not matching this is rejected.",
        "Make sure reject is reachable whenever a symbol is out of order. Accept must only be reachable at a phase-1 boundary — never in the middle of an incomplete block.",
        "Accept is only reachable from the blank check at the start of phase 1. This guarantees the tape ended on a complete block boundary. Any wrong symbol or early blank leads to reject.",
    ),

    # ── Level 8: BalancedPairs ────────────────────────────────────────────

    _tree("BalancedPairs","S2.3",
        "Student does not understand how to use write as memory across multiple passes.",
        "After one pass, how will the robot remember which pieces it has already matched? What could it leave on the tape to mark them?",
        "The tape is the robot's only memory. Writing a special marker symbol over a matched piece records that it has been processed. On the next pass, condition blocks can distinguish marked pieces from unmatched ones.",
        "Choose a marker symbol — for example, a stamp. When you match a gear, overwrite it with the marker. When you match a nut, overwrite it too. On the next pass, skip all markers.",
        "Each pass: scan right for an unmarked gear, write marker over it. Then scan right for an unmarked nut, write marker over it. Return to start and repeat. Accept when a full pass finds no unmarked gear. Reject if a nut remains when no gear is found.",
    ),

    _tree("BalancedPairs","S4.4",
        "Student builds one scanning loop but cannot structure the multi-pass logic.",
        "One pass matches one pair. How do you organise the circuit so it repeats the whole matching process until the tape is exhausted?",
        "A multi-pass program has an outer loop (one iteration per pair) containing inner scanning phases. Each outer iteration: find and mark a gear, find and mark a nut, return to the left edge, and restart.",
        "Structure: outer-loop-start → scan-right for gear → mark → scan-right for nut → mark → move back to left edge → check if unmarked pieces remain → accept or repeat.",
        "Phase 1: scan right for unmarked gear. Found → mark, go to phase 2. Blank before gear → check for unmarked nut; none → accept; else → reject. Phase 2: scan right for unmarked nut. Found → mark, go to phase 3. Blank before nut → reject. Phase 3: scan left to leftmost cell. Go to phase 1.",
    ),

    _tree("BalancedPairs","S5.3",
        "Student accepts too early, before the whole tape has been verified.",
        "You are accepting after the first pass. What if there are still unmatched pieces further on the tape?",
        "Acceptance can only be declared when the full tape is processed and no unmatched piece remains. One pass matches one pair — you need as many passes as there are pairs.",
        "Move your accept block to fire only after a complete pass finds no remaining unmarked gears or nuts.",
        "Accept only when: scanning right for a gear, you reach blank without finding one, and a second scan confirms no unmarked nut. This guarantees both sides are fully exhausted.",
    ),

    # ── Level 9: PatternSomewhere ─────────────────────────────────────────

    _tree("PatternSomewhere","S4.4",
        "Student tries to match the pattern from the start of the tape only.",
        "The pattern can start at any position. What should the robot do when a match fails partway through — give up, or try again from the next position?",
        "You need two loops: an outer loop that advances the start position one cell at a time, and an inner loop that attempts to match the full pattern from that position. A failed inner match triggers the outer loop to advance and retry.",
        "Outer loop: advance one cell at a time. At each position attempt the inner match (gear → nut → screw). Success → accept. Failure → discard and continue outer scan.",
        "Outer loop: advance one cell, mark position. Inner phase 1: check gear — fail → restore marker, outer loop continues. Inner phase 2: check nut — fail → same. Inner phase 3: check screw — success → accept. Outer loop reaches blank with no match → reject.",
    ),

    _tree("PatternSomewhere","S2.3",
        "Student cannot remember where the outer scan was after a failed inner match.",
        "After the inner match fails, the robot's head has moved. How can it find its way back to where the outer scan should continue?",
        "Before starting each inner match, write a marker on the current outer-scan position. If the inner match fails, scan back to that marker, erase it, move one step right, and try again. The marker is a bookmark the robot leaves for itself.",
        "At the start of each outer iteration: write a position marker. On inner failure: scan left to find the marker, erase it, move right one cell, begin next outer iteration.",
        "Outer step: write marker(M) on current cell. Inner match: attempt gear-nut-screw from this cell. On inner fail: scan left for marker(M), erase it, move right, continue outer loop. On inner success: accept. Outer blank with no match: reject.",
    ),

    _tree("PatternSomewhere","S5.3",
        "Student rejects immediately when the first match attempt fails.",
        "Your program rejects when the pattern does not start at the first cell. But the pattern can be anywhere. Should one failed attempt make the whole tape fail?",
        "A failed match at one position only means the pattern does not start there. The tape still needs to be searched. Reject should only fire after the entire tape has been searched with no match found anywhere.",
        "Move reject to the end of the outer loop — it fires only when the outer scan passes blank without ever succeeding. Failed inner matches must resume the outer scan, not reject.",
        "Reject is only reachable when the outer condition(blank) fires true and no accept was triggered. Every failed inner match must route back into the outer loop to continue scanning.",
    ),
]

HINT_FOREST = HintForest(trees=_TREES)

def get_hint_tree(level_id, skill_id):
    return HINT_FOREST.get_tree(level_id, skill_id)

def get_hint(level_id, skill_id, level):
    return HINT_FOREST.get_hint(level_id, skill_id, level)

def get_hint_template(level_id, skill_id, hint_level, **context):
    step = get_hint(level_id, skill_id, hint_level)
    return step.template.format(**context) if context else step.template