using NUnit.Framework;
using TuringSimulator.Controller.Hands;
using TuringSimulator.GameFlow.Events;

namespace EditModeTests
{
    public class HandGestureMicMappingTests
    {
        [Test]
        public void TryMapListenMode_ShakaPerformed_ReturnsStart()
        {
            var mapped = HandGestureMicMapping.TryMapListenMode(
                "Shaka",
                HandGesturePhase.Performed,
                "Shaka",
                out var mode);

            Assert.That(mapped, Is.True);
            Assert.That(mode, Is.EqualTo(MicListenMode.Start));
        }

        [Test]
        public void TryMapListenMode_ShakaEnded_ReturnsStop()
        {
            var mapped = HandGestureMicMapping.TryMapListenMode(
                "shaka",
                HandGesturePhase.Ended,
                "Shaka",
                out var mode);

            Assert.That(mapped, Is.True);
            Assert.That(mode, Is.EqualTo(MicListenMode.Stop));
        }

        [Test]
        public void TryMapListenMode_OtherGesture_IsIgnored()
        {
            var mapped = HandGestureMicMapping.TryMapListenMode(
                "ThumbsUp",
                HandGesturePhase.Performed,
                "Shaka",
                out var mode);

            Assert.That(mapped, Is.False);
            Assert.That(mode, Is.EqualTo(default(MicListenMode)));
        }

        [Test]
        public void TryApplyHoldCount_TwoHands_StopsOnlyWhenBothDrop()
        {
            var holdCount = 0;

            Assert.That(
                HandGestureMicMapping.TryApplyHoldCount(ref holdCount, MicListenMode.Start, out var first),
                Is.True);
            Assert.That(first, Is.EqualTo(MicListenMode.Start));
            Assert.That(holdCount, Is.EqualTo(1));

            Assert.That(
                HandGestureMicMapping.TryApplyHoldCount(ref holdCount, MicListenMode.Start, out _),
                Is.False);
            Assert.That(holdCount, Is.EqualTo(2));

            Assert.That(
                HandGestureMicMapping.TryApplyHoldCount(ref holdCount, MicListenMode.Stop, out _),
                Is.False);
            Assert.That(holdCount, Is.EqualTo(1));

            Assert.That(
                HandGestureMicMapping.TryApplyHoldCount(ref holdCount, MicListenMode.Stop, out var last),
                Is.True);
            Assert.That(last, Is.EqualTo(MicListenMode.Stop));
            Assert.That(holdCount, Is.EqualTo(0));
        }
    }
}
