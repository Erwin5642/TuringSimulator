using System.Collections.Generic;
using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    public interface IEventTraceLog
    {
        bool Enabled { get; set; }
        int Capacity { get; }

        void Configure(int capacity, bool enabled = true);
        EventTraceEntry? Record(string eventName, string payloadSummary, Object source = null);
        IReadOnlyList<EventTraceEntry> Snapshot();
        void Clear();
    }
}
