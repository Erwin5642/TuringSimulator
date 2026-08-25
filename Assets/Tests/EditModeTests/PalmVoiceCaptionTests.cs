using NUnit.Framework;
using TuringSimulator.GameFlow.Events;

namespace EditModeTests
{
    public class PalmVoiceCaptionTests
    {
        [Test]
        public void TryMatchGesture_ShakaPerformed_Shows()
        {
            var matched = PalmVoiceCaption.TryMatchGesture(
                "Shaka",
                HandGesturePhase.Performed,
                "Shaka",
                out var show);

            Assert.That(matched, Is.True);
            Assert.That(show, Is.True);
        }

        [Test]
        public void TryMatchGesture_ShakaEnded_Hides()
        {
            var matched = PalmVoiceCaption.TryMatchGesture(
                "shaka",
                HandGesturePhase.Ended,
                "Shaka",
                out var show);

            Assert.That(matched, Is.True);
            Assert.That(show, Is.False);
        }

        [Test]
        public void TryMatchGesture_OtherGesture_IsIgnored()
        {
            var matched = PalmVoiceCaption.TryMatchGesture(
                "ThumbsUp",
                HandGesturePhase.Performed,
                "Shaka",
                out var show);

            Assert.That(matched, Is.False);
            Assert.That(show, Is.False);
        }

        [Test]
        public void FormatLive_TrimsRecordedText()
        {
            Assert.That(PalmVoiceCaption.FormatLive("  olá mundo  "), Is.EqualTo("olá mundo"));
        }

        [Test]
        public void AppendStoppedCue_AddsCambioWithoutChangingSource()
        {
            const string recorded = "quero ajuda";
            var display = PalmVoiceCaption.AppendStoppedCue(recorded);

            Assert.That(display, Is.EqualTo("quero ajuda\nCambio"));
            Assert.That(recorded, Is.EqualTo("quero ajuda"));
        }

        [Test]
        public void AppendStoppedCue_EmptyText_IsJustCambio()
        {
            Assert.That(PalmVoiceCaption.AppendStoppedCue("  "), Is.EqualTo("Cambio"));
        }

        [Test]
        public void AppendStoppedCue_DoesNotDuplicate()
        {
            var once = PalmVoiceCaption.AppendStoppedCue("oi");
            var twice = PalmVoiceCaption.AppendStoppedCue(once);

            Assert.That(twice, Is.EqualTo("oi\nCambio"));
        }
    }
}
