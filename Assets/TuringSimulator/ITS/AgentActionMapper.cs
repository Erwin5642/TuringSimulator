using System;
using System.Collections.Generic;
using System.Reflection;
using TuringSimulator.GameFlow.Events;
using UnityEngine;

/// <summary>
/// Generic editor-driven mapping:
/// Event channel -> optional payload filter -> (text, animation) agent action.
/// </summary>
public sealed class AgentActionMapper : MonoBehaviour
{
    public enum ActionTextMode
    {
        Static = 0,
        PayloadProperty = 1,
        PayloadToString = 2,
        Empty = 3,
    }

    [Serializable]
    public sealed class EventActionRule
    {
        [Tooltip("Optional label to identify this rule in the Inspector.")]
        public string Name = "New Rule";

        [Tooltip("Any ScriptableObject event channel that inherits EventChannelSO<TPayload>.")]
        public ScriptableObject SourceChannel;

        [Header("Trigger Filter (optional)")]
        [Tooltip("If set, reads this payload property/field and compares it to MatchValue.")]
        public string MatchProperty;
        [Tooltip("Case-insensitive string compare against property value.")]
        public string MatchValue;

        [Header("Action")]
        public AgentAnimationKind Animation = AgentAnimationKind.None;
        public ActionTextMode TextMode = ActionTextMode.Static;
        [TextArea] public string StaticText;
        [Tooltip("Used when TextMode = PayloadProperty (e.g. Reply, Error, Text).")]
        public string TextProperty;
        [Tooltip("If true, rule is ignored when resolved text is empty.")]
        public bool SkipIfResolvedTextEmpty;
    }

    [Header("Rules")]
    [SerializeField] private List<EventActionRule> _rules = new();

    [Header("Output")]
    [SerializeField] private AgentActionRequestedEventChannel _agentActionChannel;

    [Header("Debug")]
    [SerializeField] private bool _logRuleMismatches;

    readonly List<RuleSubscription> _subscriptions = new();

    sealed class RuleSubscription
    {
        public IUntypedEventChannel Channel;
        public Action<object> Handler;
    }

    void OnEnable()
    {
        for (var i = 0; i < _rules.Count; i++)
            TrySubscribeRule(_rules[i], i);
    }

    void OnDisable()
    {
        for (var i = 0; i < _subscriptions.Count; i++)
            _subscriptions[i].Channel.OnRaisedUntyped -= _subscriptions[i].Handler;
        _subscriptions.Clear();
    }

    void TrySubscribeRule(EventActionRule rule, int ruleIndex)
    {
        if (rule == null || rule.SourceChannel == null)
            return;

        if (!(rule.SourceChannel is IUntypedEventChannel channel))
        {
            Debug.LogWarning($"[AgentActionMapper] Rule '{rule.Name}' has unsupported channel type.", this);
            return;
        }

        Action<object> handler = payload => HandleRule(rule, ruleIndex, payload);
        channel.OnRaisedUntyped += handler;
        _subscriptions.Add(new RuleSubscription
        {
            Channel = channel,
            Handler = handler,
        });
    }

    void HandleRule(EventActionRule rule, int ruleIndex, object payload)
    {
        if (!MatchesFilter(rule, payload))
            return;

        var text = ResolveText(rule, payload);
        if (rule.SkipIfResolvedTextEmpty && string.IsNullOrWhiteSpace(text))
            return;

        var context = TryResolveContext(payload)
            ?? EventContextFactory.Create(nameof(AgentActionMapper), $"rule-{ruleIndex}");
        PublishAction(context, text, rule.Animation);
    }

    bool MatchesFilter(EventActionRule rule, object payload)
    {
        if (string.IsNullOrWhiteSpace(rule.MatchProperty))
            return true;

        if (!TryReadMemberString(payload, rule.MatchProperty, out var value))
        {
            if (_logRuleMismatches)
                Debug.LogWarning($"[AgentActionMapper] Rule '{rule.Name}' could not read match member '{rule.MatchProperty}'.", this);
            return false;
        }

        return string.Equals(value, rule.MatchValue ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    string ResolveText(EventActionRule rule, object payload)
    {
        switch (rule.TextMode)
        {
            case ActionTextMode.Static:
                return rule.StaticText ?? string.Empty;
            case ActionTextMode.PayloadProperty:
                return TryReadMemberString(payload, rule.TextProperty, out var value) ? value : string.Empty;
            case ActionTextMode.PayloadToString:
                return payload?.ToString() ?? string.Empty;
            case ActionTextMode.Empty:
            default:
                return string.Empty;
        }
    }

    static bool TryReadMemberString(object payload, string memberName, out string value)
    {
        value = string.Empty;
        if (payload == null || string.IsNullOrWhiteSpace(memberName))
            return false;

        var payloadType = payload.GetType();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase;

        var property = payloadType.GetProperty(memberName, flags);
        if (property != null)
        {
            var propertyValue = property.GetValue(payload);
            value = propertyValue?.ToString() ?? string.Empty;
            return true;
        }

        var field = payloadType.GetField(memberName, flags);
        if (field != null)
        {
            var fieldValue = field.GetValue(payload);
            value = fieldValue?.ToString() ?? string.Empty;
            return true;
        }

        return false;
    }

    static EventContextData? TryResolveContext(object payload)
    {
        if (payload == null)
            return null;

        var payloadType = payload.GetType();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase;
        var property = payloadType.GetProperty("Context", flags);
        if (property == null || property.PropertyType != typeof(EventContextData))
            return null;

        var context = property.GetValue(payload);
        return context is EventContextData contextData ? contextData : null;
    }

    void PublishAction(EventContextData context, string text, AgentAnimationKind animation)
    {
        if (_agentActionChannel == null)
            return;

        var payload = new AgentActionRequestedEventData(context, text, animation);
        EventTraceLog.Record(nameof(AgentActionRequestedEventData), payload.ToString(), this);
        _agentActionChannel.Raise(payload, this);
    }
}
