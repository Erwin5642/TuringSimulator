using System;

namespace TuringSimulator.GameFlow.Events
{
    public static class EventContextFactory
    {
        public static EventContextData Create(string sourceName, string correlationId)
        {
            return new EventContextData(
                sourceName ?? string.Empty,
                correlationId ?? string.Empty,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }
    }
}
