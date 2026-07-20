---
name: level-definition-map
description: Explain how levels are defined and wired across Unity and the Python ITS server. Use when creating, renaming, removing, or debugging level IDs and progression metadata.
---
# Level Definition Map

Use this skill when a request involves level creation, level IDs, progression order, or server/client level mismatches.

## Where a level is defined

- Unity runtime shape: `Assets/TuringSimulator/Core/Level/LevelDefinition.cs`
- Unity level assets: `Assets/Prefabs/Levels/Level */Level * Definition.asset`
- Unity progression list: `Assets/Prefabs/Levels/LevelDatabase.asset`
- Server level metadata: `TuringBotAPI/orchestrator.py` (`LEVEL_META`)
- Server skill progression: `TuringBotAPI/domain/concepts.py` (`introduced_in`, `exercised_in`)
- Server hint coverage: `TuringBotAPI/domain/hints.py` (`_tree(level_id, skill_id, ...)`)

## Level ID contract

- `levelId` in Unity `LevelDefinition` is the cross-system key.
- The same ID must exist in all three server places: `LEVEL_META`, `concepts.py`, and `hints.py`.
- Keep Unity constants aligned: `Assets/TuringSimulator/ITS/ITSModel.cs` (`LevelID` class).

## Edit workflow

1. Create or update the Unity level asset (`title`, `description`, `levelId`, tests).
2. Add or reorder the level in `LevelDatabase.asset`.
3. Mirror the same level ID in `LEVEL_META` with goal and allowed blocks.
4. Update `concepts.py` so skills introduced/exercised in that level stay valid.
5. Add or update hint trees for that level in `hints.py`.
6. Update tests/docs that mention the level ID.

## Guardrails

- Player-facing copy in Unity/server responses must be pt-BR.
- Keep `levelId` stable after release; treat renames as migrations.
- Validate with repo-wide search for the old/new level ID before finishing.
