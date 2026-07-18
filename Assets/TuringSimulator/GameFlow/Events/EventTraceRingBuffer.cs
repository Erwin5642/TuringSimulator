using System;
using System.Collections.Generic;
using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    public sealed class EventTraceRingBuffer : IEventTraceLog
    {
        const int _defaultCapacity = 128;

        readonly object _sync = new object();
        EventTraceEntry[] _ring = new EventTraceEntry[_defaultCapacity];
        int _nextIndex;
        int _count;
        long _sequence;

        public bool Enabled { get; set; } = true;

        public int Capacity
        {
            get
            {
                lock (_sync)
                {
                    return _ring.Length;
                }
            }
        }

        public void Configure(int capacity, bool enabled = true)
        {
            if (capacity < 8)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be at least 8.");

            lock (_sync)
            {
                _ring = new EventTraceEntry[capacity];
                _nextIndex = 0;
                _count = 0;
                _sequence = 0;
                Enabled = enabled;
            }
        }

        public EventTraceEntry? Record(string eventName, string payloadSummary, UnityEngine.Object source = null)
        {
            if (!Enabled)
                return null;

            lock (_sync)
            {
                var entry = new EventTraceEntry(
                    ++_sequence,
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    string.IsNullOrWhiteSpace(eventName) ? "<unnamed-event>" : eventName,
                    source != null ? source.name : "<runtime>",
                    payloadSummary ?? "<null>");

                _ring[_nextIndex] = entry;
                _nextIndex = (_nextIndex + 1) % _ring.Length;
                if (_count < _ring.Length)
                    _count++;

                return entry;
            }
        }

        public IReadOnlyList<EventTraceEntry> Snapshot()
        {
            lock (_sync)
            {
                if (_count == 0)
                    return Array.Empty<EventTraceEntry>();

                var snapshot = new List<EventTraceEntry>(_count);
                var start = (_nextIndex - _count + _ring.Length) % _ring.Length;
                for (var i = 0; i < _count; i++)
                {
                    var index = (start + i) % _ring.Length;
                    snapshot.Add(_ring[index]);
                }

                return snapshot;
            }
        }

        public void Clear()
        {
            lock (_sync)
            {
                _ring = new EventTraceEntry[_ring.Length];
                _nextIndex = 0;
                _count = 0;
                _sequence = 0;
            }
        }
    }
}
