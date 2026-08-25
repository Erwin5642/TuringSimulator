---
name: unity-editor-attention
description: Declare Unity Editor follow-up work after code or asset changes that require Inspector wiring, scene/prefab assignment, ScriptableObject setup, or other manual Editor steps. Use whenever finishing Unity C# or asset work that the Editor must complete before the change works at runtime.
---

# Unity Editor Attention

When a change cannot be fully completed in code/assets alone and needs manual Unity Editor work, end the relevant response with an attention block.

Use this heading **verbatim**:

### Attention on Editor

Then list concrete Editor actions the user must do.

## When to declare

Declare attention when the change requires any of:

- Assigning serialized fields, event channels, or references in the Inspector
- Wiring scene objects, prefabs, or bootstrap installers
- Creating/updating ScriptableObject assets and linking them
- Adding components to scene/prefab objects
- Rebuilding/rebaking scene or lighting data that was not done by the agent
- Any other Editor-only step needed for the change to work in Play Mode / device builds

## When not to declare

Do not add the block for:

- Pure code-only changes with no Inspector/scene/prefab follow-up
- Edits the agent already completed in assets/code
- Optional polish unrelated to making the change functional

## Format

```markdown
### Attention on Editor

- [Concrete action]: where to click / what to assign / which object
- [Concrete action]: expected result if useful
```

Keep items actionable and specific (object names, field names, assets). Prefer a short checklist over prose.

## Example

```markdown
### Attention on Editor

- On `TuringBootstrap`, assign `Simulation Step Produced Channel` to `Assets/TuringSimulator/Events/SimulationStepProducedChannel.asset`
- Open `BasicScene` and confirm `ProgramWorkbench.startOutputPort` points at the start/power output socket
```
