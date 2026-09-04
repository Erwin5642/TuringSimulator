using System;
using System.Reflection;

namespace TuringSimulator.GameFlow.Events
{
    public static class EventPayloadFilter
    {
        public static bool Matches(
            object payload,
            string matchProperty,
            string matchValue,
            out bool memberReadable)
        {
            memberReadable = true;
            if (string.IsNullOrWhiteSpace(matchProperty))
                return true;

            if (!TryReadMemberString(payload, matchProperty, out var value))
            {
                memberReadable = false;
                return false;
            }

            return string.Equals(value, matchValue ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryReadMemberString(object payload, string memberName, out string value)
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
    }
}
