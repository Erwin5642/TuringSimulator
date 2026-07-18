using System;

namespace TuringSimulator.GameFlow.Events
{
    public readonly struct EventTraceEntry
    {
        public EventTraceEntry(
            long sequence,
            long utcUnixMs,
            string eventName,
            string sourceName,
            string payloadSummary)
        {
            Sequence = sequence;
            UtcUnixMs = utcUnixMs;
            EventName = eventName ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            PayloadSummary = payloadSummary ?? string.Empty;
        }

        public long Sequence { get; }
        public long UtcUnixMs { get; }
        public string EventName { get; }
        public string SourceName { get; }
        public string PayloadSummary { get; }

        public DateTime UtcTimestamp => DateTimeOffset.FromUnixTimeMilliseconds(UtcUnixMs).UtcDateTime;

        public override string ToString()
        {
            return $"#{Sequence} [{UtcUnixMs}] {EventName} src={SourceName} payload={PayloadSummary}";
        }
    }
}
