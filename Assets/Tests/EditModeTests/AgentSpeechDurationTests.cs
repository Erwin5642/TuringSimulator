using NUnit.Framework;

namespace EditModeTests
{
    public class AgentSpeechDurationTests
    {
        [Test]
        public void EstimateSeconds_EmptyText_ReturnsMin()
        {
            Assert.That(AgentSpeechDuration.EstimateSeconds("", 14f, 1.2f, 8f), Is.EqualTo(1.2f));
        }

        [Test]
        public void EstimateSeconds_ShortText_ClampsToMin()
        {
            Assert.That(AgentSpeechDuration.EstimateSeconds("oi", 14f, 1.2f, 8f), Is.EqualTo(1.2f));
        }

        [Test]
        public void EstimateSeconds_LongText_ClampsToMax()
        {
            var text = new string('a', 500);
            Assert.That(AgentSpeechDuration.EstimateSeconds(text, 14f, 1.2f, 8f), Is.EqualTo(8f));
        }

        [Test]
        public void EstimateSeconds_MidLength_UsesRate()
        {
            var text = new string('a', 42);
            Assert.That(AgentSpeechDuration.EstimateSeconds(text, 14f, 1.2f, 8f), Is.EqualTo(3f).Within(0.01f));
        }
    }
}
