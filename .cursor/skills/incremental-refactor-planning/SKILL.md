---
name: incremental-refactor-planning
description: Use this skill whenever a user needs to plan restructuring, decoupling, or modularizing an existing codebase - especially under a hard deadline, as a solo developer or small team, where the system must keep working and stay demoable/shippable throughout the whole process. Trigger on requests like "help me refactor this", "make this more modular", "plan a migration", "reduce coupling between systems", "this has scaled too much and I can't reason about it anymore", or any request for an implementation plan plus a risk analysis for restructuring software. Provides a phased, checkpoint-based migration method with a built-in rollback strategy, a structured risk-table format (technical, integration, schedule, and cost-of-inaction risks, each with likelihood/impact/mitigation/fallback), and a pragmatic "definition of done" that prevents scope creep into a full rewrite. Language- and engine-agnostic - applies to games, web apps, services, or any other codebase.
---

# Incremental Refactor Planning

A method for planning a mid-project architecture refactor that never leaves the system in a broken state, and for producing a risk analysis that a deadline-constrained developer can actually act on.

## When to use

Any time someone wants to restructure, decouple, or modularize working software while it still needs to ship - a course project, an MVP, a production system with an approaching release. The tighter the deadline and the smaller the team, the more this skill matters: it exists specifically to stop refactors from turning into unshippable rewrites.

## Core principles

1. **The working build is the constraint, not an afterthought.** Every phase of the plan must end with something runnable/demoable. No stretch of the timeline should leave the system broken with no fallback.
2. **Migrate one thing at a time.** Pick the smallest unit that can be extracted, migrated, and verified independently. Resist migrating an entire system in one pass just because it's conceptually one thing.
3. **Checkpoint every stable state.** A checkpoint (branch, tag, commit) at the end of each stable day/phase is what makes rollback possible. If a checkpoint doesn't exist, the next step isn't safe to start.
4. **Freeze scope early and explicitly.** Decide up front what does *not* need to be touched. State it in the plan. This is the main defense against scope creep eating the schedule.
5. **Every mitigation needs a fallback.** A mitigation that only works if everything goes according to plan isn't a mitigation - it's a hope. Pair each one with "if this doesn't work by [checkpoint], do [cheaper, uglier, but safe alternative] instead."

## Process

1. **Capture a baseline.** Write an explicit checklist of what currently works end-to-end (the exact user-facing flows that must survive the refactor). This becomes the regression check run after every phase - not a vague "make sure nothing broke" but a concrete list to walk through.
2. **Identify the highest-coupling entry points.** Find the small number of files/modules where unrelated concerns converge - god objects, bootstrap/installer classes, monolithic controllers. These are usually both the biggest problem and the highest-risk part of the migration; name them specifically rather than describing the refactor in the abstract.
3. **Break the timeline into phases with a runnable checkpoint at the end of each.** Sequence the riskiest, most-coupled work *first* - but only while there's still enough runway left to recover if it goes wrong. Never schedule the riskiest work last, and never schedule so much risk early that one bad day sinks the whole timeline. Build in explicit buffer time before the deadline for integration testing and polish, separate from refactor work.
4. **Write the rollback strategy into the plan itself**, not as an implicit assumption: which branch, how granular the commits are, and a per-phase rule of the form "if [phase] isn't stable by [day], revert to [last checkpoint] and ship with [specific narrower scope] instead."
5. **Produce a risk table** (see format below) covering four categories - most plans only think of the first:
   - **Technical risks** - things that can break during migration itself.
   - **Integration risks** - anything involving an external system, API, service, or third-party dependency the refactor touches.
   - **Schedule risks** - specific to the timeline and team size (solo-dev fatigue, underestimated migration time, scope creep).
   - **Risk of doing nothing** - what happens if the refactor is skipped entirely. This is the baseline that justifies doing the work at all, and it's the category plans most often omit.
6. **State a definition of done tied to the actual underlying problem**, not to "clean architecture" in the abstract. If the real problem was "I can't tell what's happening," done means observable event flow - not full test coverage or a particular design pattern. Explicitly list what is *not* required, so the developer (or agent) doesn't keep going past the point that solves the problem.

## Risk table format

For every risk, capture:

| Risk | Likelihood | Impact | Mitigation | Fallback |
|---|---|---|---|---|
| (short description) | Low/Med/High | Low/Med/High | (concrete action taken during the plan) | (what happens if the mitigation doesn't hold, by when) |

## Common failure modes to flag to the user

- **Big-bang rewrites** with no intermediate runnable state - if a phase can't be checkpointed, split it further.
- **Sunk-cost scope creep** - refactoring subsystems that don't actually block the stated goal, because "as long as we're in there."
- **Mitigations with no fallback** - a plan that only works if the plan works isn't a risk analysis.
- **No schedule buffer** before a hard deadline - testing and polish need dedicated time, not leftover time.
- **Vague risk items** ("might break something") instead of naming the specific coupling point, file, or integration that's actually fragile.
