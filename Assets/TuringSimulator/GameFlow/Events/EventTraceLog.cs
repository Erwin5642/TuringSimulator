using System.Collections.Generic;

namespace TuringSimulator.GameFlow.Events
{
    public static class EventTraceLog
    {
        private static IEventTraceLog _implementation = new EventTraceRingBuffer();

        public static IEventTraceLog Implementation
        {
            get => _implementation;
            set => _implementation = value ?? throw new System.ArgumentNullException(nameof(value));
        }

        public static bool Enabled
        {
            get => _implementation.Enabled;
            set => _implementation.Enabled = value;
        }

        public static int Capacity => _implementation.Capacity;

        public static void Configure(int capacity, bool enabled = true)
        {
            _implementation.Configure(capacity, enabled);
        }

        public static EventTraceEntry? Record(string eventName, string payloadSummary, UnityEngine.Object source = null)
        {
            return _implementation.Record(eventName, payloadSummary, source);
        }

        public static IReadOnlyList<EventTraceEntry> Snapshot()
        {
            return _implementation.Snapshot();
        }

        public static void Clear()
        {
            _implementation.Clear();
        }
    }
}
