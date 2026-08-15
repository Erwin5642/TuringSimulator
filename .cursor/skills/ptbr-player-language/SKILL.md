---
name: ptbr-player-language
description: Enforce pt-BR language for player-facing copy. Use when adding or editing UI text, level titles/descriptions, tutor dialogue, hints, ask replies, fallback strings, or TTS-visible content.
---
# PT-BR Player Language

Make sure to use portuguese for any text that may be displayed on UI and agent dialogue.

## Apply this to

- Unity UI text shown to players.
- Agent dialogue text and fallback responses.
- `LevelDefinition.title` and `LevelDefinition.description`.
- Any subtitles, prompt labels, or text spoken by TTS.

## Do not block on

- Developer-facing logs.
- Test scenario IDs and internal identifiers.
- Internal code comments unless the user asks otherwise.

## Quick check before finishing a change

- Any new player-visible string is in Portuguese (pt-BR).
- Existing English player-visible strings touched by the change are translated.
- Server-generated user-facing fallback text remains pt-BR.
