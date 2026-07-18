---
name: unity-atomic-event-architecture
description: Use this skill whenever a user wants to make a Unity C# game more modular, atomic, and traceable via events - including requests to make a game "event-driven", decouple MonoBehaviours, split monolithic scripts into smaller pieces, wire components through the Inspector instead of GetComponent/FindObjectOfType, add validation so missing references fail loudly instead of causing runtime null-reference errors, or add visibility into what game events fired and when. Trigger on phrases like "make my Unity scripts more modular", "atomic scripts", "event channels", "ScriptableObject events", "decouple my game systems", or "I can't see what's happening in my game". Covers choosing between ScriptableObject event channels, plain C# events, and UnityEvents; splitting monolithic scripts into atomic condition-publisher and action-listener components; Inspector-first wiring conventions; OnValidate and startup validation patterns; and a lightweight event trace logger for debugging.
---

# Unity Atomic Event Architecture

A pattern for turning tangled, monolithic Unity scripts into small, traceable, event-wired pieces without losing Inspector-level visibility - useful for solo/small-team projects where debuggability matters more than architectural purity.

## The core pattern: condition -> event channel -> action

Split every piece of coupled logic into three roles instead of one script that does everything:

- **Condition publisher** - a MonoBehaviour that detects exactly one thing (an input, a state change, a threshold being crossed) and raises exactly one event. It performs no side effects beyond raising the event.
- **Event channel** - a ScriptableObject asset representing one event type with a typed payload (a small struct/record, not a loose object). It lives as an asset, is assignable in the Inspector, and is what makes the wiring visible instead of implicit.
- **Action listener** - a MonoBehaviour that subscribes to one channel and performs one resulting effect. If an action needs to trigger further effects, it raises a *new*, explicitly named event (a new phase) rather than chaining silently.
- **Coordinators** - keep these for state ownership and ordering (e.g. an async single-flight lock, "only one run active at a time"), but shrink them to just that. Don't eliminate coordinators entirely, and don't let them hold business logic again.

Avoid building a single central event bus that everything routes through anonymously - it's fast to write but becomes invisible at the scene level. Named channel assets keep the wiring inspectable.

## Choosing the wiring mechanism

| Mechanism | Use when | Avoid when |
|---|---|---|
| ScriptableObject event channel | The event involves/affects MonoBehaviours, scene objects, UI, or needs to cross scenes/systems, and Inspector visibility matters | It's a pure data transform with no MonoBehaviour involved and no debugging benefit from Inspector wiring |
| Plain C# event/delegate | Pure logic or service-layer code with no MonoBehaviour involved | You need Inspector-level wiring or cross-scene decoupling |
| UnityEvent | Simple designer-facing hookups with no need for a strongly typed payload | Using it as the main architecture backbone - weak typing makes it harder to trace and debug at scale |

## Editor-wiring convention

Any reference that touches a MonoBehaviour should be a serialized field assigned by dragging in the Inspector - not resolved at runtime via `GetComponent`, `FindObjectOfType`, or a singleton lookup. This keeps wiring visible and editable without touching code.

```csharp
public class HaltReachedListener : MonoBehaviour
{
    [SerializeField] private HaltReachedEventChannel channel;
    [SerializeField] private ValidationAction validationAction;

    private void OnEnable() => channel.OnRaised += HandleHalt;
    private void OnDisable() => channel.OnRaised -= HandleHalt;

    private void HandleHalt(HaltReachedEventData data) => validationAction.Run(data);
}
```

## Validation pattern

Because Inspector-wiring is manual, add checks that fail loudly instead of silently:

```csharp
private void OnValidate()
{
    if (channel == null) Debug.LogWarning($"{name}: missing HaltReachedEventChannel reference", this);
    if (validationAction == null) Debug.LogWarning($"{name}: missing ValidationAction reference", this);
}
```

Pair this with a single startup validation pass (one component or editor script) that scans the scene before Play and checks every required channel/listener/reference is wired. Give it two severities: hard errors for anything on the core/demo path, and soft warnings for optional or non-critical systems - don't let an optional missing reference block the whole run.

## Observability: event trace logger

Add a small ring-buffer logger that every channel writes to when it raises (event name, sequence number, timestamp, source object), toggleable via a debug flag. This is what actually lets a developer *watch* events fire in order - the architecture change alone doesn't solve visibility, the trace log does. Keep it cheap: no per-frame string formatting unless the overlay is actively open, and throttle high-frequency events (e.g. per-frame or per-step signals) rather than logging every one.

## Anti-patterns to avoid

- **Deep, unnamed event chains** - an action that raises an event, whose listener raises another event, with no clear phase boundary. Name each new stage explicitly (e.g. `ValidationCompleted`) instead of letting chains form implicitly.
- **Re-tangling logic inside listeners** - if a single listener ends up doing five unrelated things "since it's already subscribed," it has become the same monolithic script in a new shape. One listener, one job.
- **Routing everything through events** - pure algorithmic or data-transform code that never touches a MonoBehaviour usually doesn't need an event channel; a direct method call is simpler and no less clear.
- **Migrating everything at once** - convert one system's monolithic entry point into publishers/listeners at a time, keeping the old direct calls temporarily alongside if needed, rather than rewriting every system in a single pass.
