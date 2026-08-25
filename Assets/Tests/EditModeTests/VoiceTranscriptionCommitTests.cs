using NUnit.Framework;

namespace EditModeTests
{
    public class VoiceTranscriptionCommitTests
    {
        [Test]
        public void Capture_ReplacesWithLatestWitText()
        {
            var first = VoiceTranscriptionCommit.Capture("oi", nowUnscaled: 1f);
            var second = VoiceTranscriptionCommit.Capture("oi como vai", nowUnscaled: 2f);

            Assert.That(VoiceTranscriptionCommit.ResolveCommitText(second), Is.EqualTo("oi como vai"));
            Assert.That(first.AccumulatedText, Is.EqualTo("oi"));
        }

        [Test]
        public void Capture_DoesNotJoinPreviousText()
        {
            VoiceTranscriptionCommit.Capture("oi", nowUnscaled: 1f);
            var latest = VoiceTranscriptionCommit.Capture("como vai", nowUnscaled: 2f);

            Assert.That(VoiceTranscriptionCommit.ResolveCommitText(latest), Is.EqualTo("como vai"));
        }

        [Test]
        public void ShouldCommitOnSilence_BeforeWindow_IsFalse()
        {
            var buffer = VoiceTranscriptionCommit.Capture("olá", nowUnscaled: 10f);

            Assert.That(
                VoiceTranscriptionCommit.ShouldCommitOnSilence(buffer, nowUnscaled: 24.9f, silenceSeconds: 15f),
                Is.False);
        }

        [Test]
        public void ShouldCommitOnSilence_AfterWindow_IsTrue()
        {
            var buffer = VoiceTranscriptionCommit.Capture("olá", nowUnscaled: 10f);

            Assert.That(
                VoiceTranscriptionCommit.ShouldCommitOnSilence(buffer, nowUnscaled: 25f, silenceSeconds: 15f),
                Is.True);
        }

        [Test]
        public void ShouldCommitOnSilence_WithoutText_IsFalse()
        {
            Assert.That(
                VoiceTranscriptionCommit.ShouldCommitOnSilence(
                    VoiceUtteranceBufferData.Empty,
                    nowUnscaled: 20f,
                    silenceSeconds: 15f),
                Is.False);
        }

        [Test]
        public void ShouldCommitOnSilence_WhenDisabled_IsFalse()
        {
            var buffer = VoiceTranscriptionCommit.Capture("olá", nowUnscaled: 0f);

            Assert.That(
                VoiceTranscriptionCommit.ShouldCommitOnSilence(buffer, nowUnscaled: 100f, silenceSeconds: 0f),
                Is.False);
        }

        [Test]
        public void ResolveCommitText_Trims()
        {
            var buffer = VoiceTranscriptionCommit.Capture("  olá  ", nowUnscaled: 1f);

            Assert.That(VoiceTranscriptionCommit.ResolveCommitText(buffer), Is.EqualTo("olá"));
        }
    }
}
