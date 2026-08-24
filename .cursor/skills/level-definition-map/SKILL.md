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
- Server goal docs: `TuringBotAPI/knowledge/goals/*.md` (`level_id` frontmatter)
- Unity constants: `Assets/TuringSimulator/ITS/ITSModel.cs` (`LevelID` class)

## Level ID contract

- `levelId` in Unity `LevelDefinition` is the cross-system key.
- Active Unity progression has **eight** levels in `LevelDatabase` (no `AppendScrew`).
- IDs that ship in Unity must have a matching server goal document.
- Keep `levelId` values aligned across the Unity asset, `LevelID`, and the goal markdown `level_id`.

## Edit workflow

1. Create or update the Unity level asset (`title`, `description`, `levelId`, tests).
2. Add or reorder the level in `LevelDatabase.asset`.
3. Add or update `TuringBotAPI/knowledge/goals/` with the same `level_id`, pt-BR goal, and allowed blocks.
4. Update tests/docs that mention the level ID.

## Guardrails

- Player-facing copy in Unity/server responses must be pt-BR.
- Keep `levelId` stable after release; treat renames as migrations.
- Validate with repo-wide search for the old/new level ID before finishing.
