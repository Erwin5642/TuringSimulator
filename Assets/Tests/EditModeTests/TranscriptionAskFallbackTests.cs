using NUnit.Framework;

namespace EditModeTests
{
    public class TranscriptionAskFallbackTests
    {
        [Test]
        public void ShouldEcho_WhenAskCannotBePosted_AndTextPresent()
        {
            Assert.That(TranscriptionAskFallback.ShouldEcho("olá", canPostAsk: false), Is.True);
        }

        [Test]
        public void ShouldEcho_WhenAskCanBePosted_IsFalse()
        {
            Assert.That(TranscriptionAskFallback.ShouldEcho("olá", canPostAsk: true), Is.False);
        }

        [Test]
        public void ShouldEcho_WhenTextEmpty_IsFalse()
        {
            Assert.That(TranscriptionAskFallback.ShouldEcho("  ", canPostAsk: false), Is.False);
            Assert.That(TranscriptionAskFallback.ShouldEcho(null, canPostAsk: false), Is.False);
        }

        [Test]
        public void ResolveEchoText_Trims()
        {
            Assert.That(TranscriptionAskFallback.ResolveEchoText("  oi  "), Is.EqualTo("oi"));
        }
    }
}
