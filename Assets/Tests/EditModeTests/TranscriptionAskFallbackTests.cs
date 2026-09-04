using NUnit.Framework;

namespace EditModeTests
{
    public class TranscriptionAskFallbackTests
    {
        [Test]
        public void ShouldPublishLocalFallback_WhenNoItsClient_AndTextPresent()
        {
            Assert.That(
                TranscriptionAskFallback.ShouldPublishLocalFallback("olá", itsClientPresent: false),
                Is.True);
        }

        [Test]
        public void ShouldPublishLocalFallback_WhenItsClientPresent_IsFalse()
        {
            Assert.That(
                TranscriptionAskFallback.ShouldPublishLocalFallback("olá", itsClientPresent: true),
                Is.False);
        }

        [Test]
        public void ShouldPublishLocalFallback_WhenTextEmpty_IsFalse()
        {
            Assert.That(
                TranscriptionAskFallback.ShouldPublishLocalFallback("  ", itsClientPresent: false),
                Is.False);
            Assert.That(
                TranscriptionAskFallback.ShouldPublishLocalFallback(null, itsClientPresent: false),
                Is.False);
        }

        [Test]
        public void UnreachableReply_IsRadioFallback()
        {
            Assert.That(
                TranscriptionAskFallback.UnreachableReply,
                Is.EqualTo("O rádio não ta muito bom."));
        }
    }
}
