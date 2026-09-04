using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TuringSimulator.GameFlow.Events
{
    /// <summary>
    /// Generic channel listener: drag any event-channel asset, optional payload
    /// filter, then invoke a UnityEvent (any component method, XR Interactor style).
    /// </summary>
    public sealed class EventChannelActionListener : MonoBehaviour
    {
        [Serializable]
        public sealed class Binding
        {
            [Tooltip("Optional label to identify this binding in the Inspector.")]
            public string Name = "New Binding";

            [Tooltip("Any ScriptableObject event channel that inherits EventChannelSO<TPayload>.")]
            public ScriptableObject SourceChannel;

            [Header("Trigger Filter (optional)")]
            [Tooltip("If set, reads this payload property/field and compares it to MatchValue. Names and values are listed on the assigned channel.")]
            public string MatchProperty;
            [Tooltip("Case-insensitive string compare against property value (enum/bool names are listed on the channel).")]
            public string MatchValue;

            [Header("Action")]
            [Tooltip("Persistent calls: drag a component and pick a method (e.g. ParticleSystem.Play).")]
            public UnityEvent OnMatched = new();
        }

        [Header("Bindings")]
        [SerializeField] private List<Binding> _bindings = new();

        readonly List<Subscription> _subscriptions = new();

        sealed class Subscription
        {
            public IUntypedEventChannel Channel;
            public Action<object> Handler;
        }

        void OnEnable()
        {
            SubscribeAll();
        }

        void OnDisable()
        {
            UnsubscribeAll();
        }

        void SubscribeAll()
        {
            if (_bindings == null)
                return;

            for (var i = 0; i < _bindings.Count; i++)
                TrySubscribe(_bindings[i]);
        }

        void UnsubscribeAll()
        {
            for (var i = 0; i < _subscriptions.Count; i++)
            {
                if (_subscriptions[i].Channel != null && _subscriptions[i].Handler != null)
                {
                    _subscriptions[i].Channel.OnRaisedUntyped -= _subscriptions[i].Handler;
                }
            }
            _subscriptions.Clear();
        }

        void TrySubscribe(Binding binding)
        {
            if (binding == null || binding.SourceChannel == null)
                return;

            if (binding.SourceChannel is not IUntypedEventChannel channel)
            {
                Debug.LogWarning(
                    $"[EventChannelActionListener] Binding '{binding.Name}' has unsupported channel type.",
                    this);
                return;
            }

            Action<object> handler = payload => HandleBinding(binding, payload);
            channel.OnRaisedUntyped += handler;
            _subscriptions.Add(new Subscription
            {
                Channel = channel,
                Handler = handler,
            });
        }

        static void HandleBinding(Binding binding, object payload)
        {
            if (!EventPayloadFilter.Matches(
                    payload,
                    binding.MatchProperty,
                    binding.MatchValue,
                    out _))
                return;

            binding.OnMatched?.Invoke();
        }

        void OnValidate()
        {
            // Always run the validation check warnings
            if (_bindings != null)
            {
                for (var i = 0; i < _bindings.Count; i++)
                {
                    var binding = _bindings[i];
                    if (binding?.SourceChannel == null)
                        continue;
                    if (binding.SourceChannel is not IUntypedEventChannel)
                        Debug.LogWarning(
                            $"{name}: binding '{binding.Name}' SourceChannel must be an EventChannelSO.",
                            this);
                }
            }

            // If we are actively in Play Mode, dynamically refresh subscriptions
            // so inspector modifications take effect immediately.
            if (Application.isPlaying && gameObject.activeInHierarchy)
            {
                UnsubscribeAll();
                SubscribeAll();
            }
        }
    }
}
