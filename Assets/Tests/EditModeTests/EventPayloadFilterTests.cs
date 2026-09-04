using NUnit.Framework;
using TuringSimulator.GameFlow.Events;

namespace EditModeTests
{
    public class EventPayloadFilterTests
    {
        sealed class PayloadWithProperty
        {
            public string Outcome { get; set; }
        }

        sealed class PayloadWithField
        {
            public string Phase;
        }

        [Test]
        public void EmptyMatchProperty_MatchesAnyPayload()
        {
            Assert.That(
                EventPayloadFilter.Matches(new PayloadWithProperty { Outcome = "Victory" }, null, "Victory", out var readable),
                Is.True);
            Assert.That(readable, Is.True);
            Assert.That(
                EventPayloadFilter.Matches(null, "  ", "x", out _),
                Is.True);
        }

        [Test]
        public void Property_MatchIsCaseInsensitive()
        {
            var payload = new PayloadWithProperty { Outcome = "Victory" };

            Assert.That(
                EventPayloadFilter.Matches(payload, "outcome", "victory", out var readable),
                Is.True);
            Assert.That(readable, Is.True);
        }

        [Test]
        public void Property_DifferentValue_DoesNotMatch()
        {
            var payload = new PayloadWithProperty { Outcome = "Victory" };

            Assert.That(
                EventPayloadFilter.Matches(payload, "Outcome", "Defeat", out var readable),
                Is.False);
            Assert.That(readable, Is.True);
        }

        [Test]
        public void MissingMember_DoesNotMatch()
        {
            var payload = new PayloadWithProperty { Outcome = "Victory" };

            Assert.That(
                EventPayloadFilter.Matches(payload, "Missing", "Victory", out var readable),
                Is.False);
            Assert.That(readable, Is.False);
        }

        [Test]
        public void Field_MatchUsesToString()
        {
            var payload = new PayloadWithField { Phase = "Started" };

            Assert.That(
                EventPayloadFilter.Matches(payload, "Phase", "Started", out var readable),
                Is.True);
            Assert.That(readable, Is.True);
        }
    }
}
