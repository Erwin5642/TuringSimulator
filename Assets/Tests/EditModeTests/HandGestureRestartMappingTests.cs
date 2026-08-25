using NUnit.Framework;
using TuringSimulator.Controller.Hands;
using TuringSimulator.GameFlow.Events;

namespace EditModeTests
{
    public class HandGestureRestartMappingTests
    {
        [Test]
        public void ShouldReload_ThumbsDownPerformed_ReturnsTrue()
        {
            var shouldReload = HandGestureRestartMapping.ShouldReload(
                "ThumbsDown",
                HandGesturePhase.Performed,
                "ThumbsDown");

            Assert.That(shouldReload, Is.True);
        }

        [Test]
        public void ShouldReload_IgnoresCase()
        {
            var shouldReload = HandGestureRestartMapping.ShouldReload(
                "thumbsdown",
                HandGesturePhase.Performed,
                "ThumbsDown");

            Assert.That(shouldReload, Is.True);
        }

        [Test]
        public void ShouldReload_ThumbsDownEnded_ReturnsFalse()
        {
            var shouldReload = HandGestureRestartMapping.ShouldReload(
                "ThumbsDown",
                HandGesturePhase.Ended,
                "ThumbsDown");

            Assert.That(shouldReload, Is.False);
        }

        [Test]
        public void ShouldReload_OtherGesture_IsIgnored()
        {
            var shouldReload = HandGestureRestartMapping.ShouldReload(
                "ThumbsUp",
                HandGesturePhase.Performed,
                "ThumbsDown");

            Assert.That(shouldReload, Is.False);
        }

        [Test]
        public void ShouldReload_EmptyIds_ReturnsFalse()
        {
            Assert.That(
                HandGestureRestartMapping.ShouldReload(string.Empty, HandGesturePhase.Performed, "ThumbsDown"),
                Is.False);
            Assert.That(
                HandGestureRestartMapping.ShouldReload("ThumbsDown", HandGesturePhase.Performed, string.Empty),
                Is.False);
        }
    }
}
