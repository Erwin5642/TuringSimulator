using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace TuringSimulator.GameFlow.Events
{
    public readonly struct EventPayloadMemberDocs
    {
        public EventPayloadMemberDocs(string name, string typeName, string matchValues, int depth)
        {
            Name = name ?? string.Empty;
            TypeName = typeName ?? string.Empty;
            MatchValues = matchValues ?? string.Empty;
            Depth = depth;
        }

        public string Name { get; }
        public string TypeName { get; }
        public string MatchValues { get; }
        public int Depth { get; }
    }

    /// <summary>
    /// Read-only payload member list for event-channel Inspector docs.
    /// Uses the same public fields/properties <see cref="EventPayloadFilter"/> can match.
    /// </summary>
    public static class EventPayloadSchema
    {
        const int NestedMemberDepth = 1;

        public static Type TryGetPayloadType(Type channelType)
        {
            if (channelType == null)
                return null;

            for (var type = channelType; type != null; type = type.BaseType)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EventChannelSO<>))
                    return type.GetGenericArguments()[0];
            }

            var interfaces = channelType.GetInterfaces();
            for (var i = 0; i < interfaces.Length; i++)
            {
                var iface = interfaces[i];
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEventChannel<>))
                    return iface.GetGenericArguments()[0];
            }

            return null;
        }

        public static IReadOnlyList<EventPayloadMemberDocs> ListMembers(Type payloadType)
        {
            if (payloadType == null)
                throw new ArgumentNullException(nameof(payloadType));

            var members = new List<EventPayloadMemberDocs>();
            CollectMembers(payloadType, 0, members);
            return members;
        }

        public static string FormatMatchValues(Type memberType)
        {
            if (memberType == null)
                return string.Empty;

            if (memberType == typeof(bool))
                return "True | False";

            if (memberType.IsEnum)
                return string.Join(" | ", Enum.GetNames(memberType));

            return string.Empty;
        }

        public static bool TryFormatInspectorDocs(UnityEngine.Object channelAsset, out string docs)
        {
            docs = string.Empty;
            if (channelAsset == null)
                return false;

            var payloadType = TryGetPayloadType(channelAsset.GetType());
            if (payloadType == null)
            {
                docs = "Not an EventChannelSO. Assign a TuringSimulator event channel asset.";
                return true;
            }

            docs = FormatInspectorDocs(payloadType);
            return true;
        }

        public static string FormatInspectorDocs(Type payloadType)
        {
            if (payloadType == null)
                throw new ArgumentNullException(nameof(payloadType));

            var members = ListMembers(payloadType);
            var text = new StringBuilder();
            text.Append("Payload: ").Append(payloadType.Name).AppendLine();
            text.Append(
                "Top-level names are MatchProperty (and TextProperty). Leave MatchProperty empty to run on every raise. MatchValue is a case-insensitive ToString compare.");

            var hasNested = false;
            for (var i = 0; i < members.Count; i++)
            {
                if (members[i].Depth > 0)
                {
                    hasNested = true;
                    break;
                }
            }

            if (hasNested)
            {
                text.AppendLine();
                text.Append("Indented names are nested inside the parent and cannot be used as MatchProperty.");
            }

            text.AppendLine();
            for (var i = 0; i < members.Count; i++)
            {
                var member = members[i];
                text.AppendLine();
                text.Append(member.Depth == 0 ? "• " : "    ");
                text.Append(member.Name).Append(" (").Append(member.TypeName).Append(')');
                if (!string.IsNullOrEmpty(member.MatchValues))
                    text.Append(" — ").Append(member.MatchValues);
            }

            return text.ToString();
        }

        static void CollectMembers(Type type, int depth, List<EventPayloadMemberDocs> members)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            var declared = new List<(int Token, string Name, Type MemberType)>();

            var properties = type.GetProperties(flags);
            for (var i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                if (!property.CanRead || property.GetIndexParameters().Length != 0)
                    continue;
                declared.Add((property.MetadataToken, property.Name, property.PropertyType));
            }

            var fields = type.GetFields(flags);
            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                declared.Add((field.MetadataToken, field.Name, field.FieldType));
            }

            declared.Sort((left, right) => left.Token.CompareTo(right.Token));

            for (var i = 0; i < declared.Count; i++)
            {
                var item = declared[i];
                members.Add(new EventPayloadMemberDocs(
                    item.Name,
                    PrettyTypeName(item.MemberType),
                    FormatMatchValues(item.MemberType),
                    depth));

                if (depth < NestedMemberDepth && ShouldExpand(item.MemberType))
                    CollectMembers(item.MemberType, depth + 1, members);
            }
        }

        static bool ShouldExpand(Type type)
        {
            if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal))
                return false;
            if (type.IsInterface || type.IsArray)
                return false;
            if (!type.IsValueType)
                return false;
            if (type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Quaternion) || type == typeof(Color))
                return false;
            return true;
        }

        static string PrettyTypeName(Type type)
        {
            if (type == typeof(bool))
                return "bool";
            if (type == typeof(int))
                return "int";
            if (type == typeof(long))
                return "long";
            if (type == typeof(float))
                return "float";
            if (type == typeof(string))
                return "string";
            return type.Name;
        }
    }
}
