using System;
using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    public abstract class EventChannelSO<TPayload> : ScriptableObject, IEventChannel<TPayload>, IUntypedEventChannel
    {
        [Header("Debug Trace")]
        [SerializeField] private bool _traceEvents = true;
        [SerializeField] private bool _logToConsole;
        [SerializeField] private int _traceEveryNthRaise = 1;

        private int _raiseCount;

        public event Action<TPayload> OnRaised;
        public event Action<object> OnRaisedUntyped;

        public void Raise(TPayload payload, UnityEngine.Object source = null)
        {
            _raiseCount++;
            if (_traceEvents && (_raiseCount % _traceEveryNthRaise == 0))
            {
                var entry = EventTraceLog.Record(name, FormatPayload(payload), source);
                if (_logToConsole && entry.HasValue)
                    Debug.Log($"[EventTrace] {entry.Value}", source);
            }

            OnRaised?.Invoke(payload);
            OnRaisedUntyped?.Invoke(payload);
        }

        protected virtual string FormatPayload(TPayload payload)
        {
            object boxed = payload;
            return boxed?.ToString() ?? "<null>";
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (_traceEveryNthRaise < 1)
                _traceEveryNthRaise = 1;
        }
#endif
    }
}
