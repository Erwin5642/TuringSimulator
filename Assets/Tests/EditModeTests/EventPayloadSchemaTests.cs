using System.Linq;
using NUnit.Framework;
using TuringSimulator.GameFlow.Events;
using UnityEngine;

namespace EditModeTests
{
    public class EventPayloadSchemaTests
    {
        [Test]
        public void TryGetPayloadType_ReadsGenericChannelArgument()
        {
            Assert.That(
                EventPayloadSchema.TryGetPayloadType(typeof(TapeMovedEventChannel)),
                Is.EqualTo(typeof(TapeMovedEventData)));
        }

        [Test]
        public void TryGetPayloadType_UnknownType_ReturnsNull()
        {
            Assert.That(EventPayloadSchema.TryGetPayloadType(typeof(ScriptableObject)), Is.Null);
        }

        [Test]
        public void TapeMoved_ListsFilterableMembersAndEnumValues()
        {
            var members = EventPayloadSchema.ListMembers(typeof(TapeMovedEventData));
            var phase = members.Single(member => member.Name == "Phase" && member.Depth == 0);

            Assert.That(phase.TypeName, Is.EqualTo(nameof(TapeMovePhase)));
            Assert.That(phase.MatchValues, Is.EqualTo("Started | Finished"));
            Assert.That(members.Any(member => member.Name == "Direction" && member.Depth == 0), Is.True);
            Assert.That(members.Any(member => member.Name == "WorldPosition" && member.Depth == 0), Is.True);
        }

        [Test]
        public void TapeRead_BoolMatchValuesAreTrueFalse()
        {
            var members = EventPayloadSchema.ListMembers(typeof(TapeReadEventData));
            var isMatch = members.Single(member => member.Name == "IsMatch" && member.Depth == 0);

            Assert.That(isMatch.MatchValues, Is.EqualTo("True | False"));
        }

        [Test]
        public void NestedContextMembers_AreIndentedAndNotTopLevel()
        {
            var members = EventPayloadSchema.ListMembers(typeof(LevelOutcomeEventData));
            var context = members.Single(member => member.Name == "Context" && member.Depth == 0);
            var sourceName = members.Single(member => member.Name == "SourceName");

            Assert.That(context.MatchValues, Is.Empty);
            Assert.That(sourceName.Depth, Is.EqualTo(1));
        }

        [Test]
        public void FormatInspectorDocs_IncludesPayloadNameAndMatchValues()
        {
            var docs = EventPayloadSchema.FormatInspectorDocs(typeof(LevelOutcomeEventData));

            Assert.That(docs, Does.Contain("LevelOutcomeEventData"));
            Assert.That(docs, Does.Contain("Outcome"));
            Assert.That(docs, Does.Contain("Victory | Defeat"));
            Assert.That(docs, Does.Contain("MatchProperty"));
        }

        [Test]
        public void TryFormatInspectorDocs_ChannelAsset_ReturnsPayloadDocs()
        {
            var channel = ScriptableObject.CreateInstance<TapeMovedEventChannel>();
            try
            {
                Assert.That(EventPayloadSchema.TryFormatInspectorDocs(channel, out var docs), Is.True);
                Assert.That(docs, Does.Contain("TapeMovedEventData"));
                Assert.That(docs, Does.Contain("Started | Finished"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(channel);
            }
        }

        [Test]
        public void HandGesture_IncludesGestureKey()
        {
            var members = EventPayloadSchema.ListMembers(typeof(HandGesturePerformedEventData));
            Assert.That(members.Any(member => member.Name == "GestureKey" && member.Depth == 0), Is.True);
        }
    }
}
