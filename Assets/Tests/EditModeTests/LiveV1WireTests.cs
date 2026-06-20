using ITS.Protocol;
using NUnit.Framework;
using TuringSimulator.Core.Types;
using TuringSimulator.Core.Simulation.Step;

namespace EditModeTests
{
    public class LiveV1WireTests
    {
        [Test]
        public void SimStepFromDiff_RoundTrip_Envelope()
        {
            var diff = new StepDiff(Symbol.Gear, Symbol.Blank, 0, 1, 0, 1, 0);
            var body = LiveV1Wire.SimStepFromDiff(diff);
            var env = LiveV1Wire.BuildEnvelope(
                LiveV1Kinds.SimStep,
                "sess",
                "stu",
                "MoveLeftRight",
                body);
            var json = LiveV1Wire.SerializeEnvelope(env);
            Assert.That(json, Does.Contain("\"kind\":\"live.sim_step\""));
            Assert.That(json, Does.Contain("\"phase\":\"played\""));
            Assert.That(json, Does.Contain("\"step_index\":0"));
        }

        [Test]
        public void TryDeserializeInbound_LiveError_ParsesCodeAndMessage()
        {
            var json = "{\"protocol_version\":\"1.0.0\",\"message_id\":\"m1\",\"sent_at_unix_ms\":1," +
                       "\"session_id\":\"s\",\"student_id\":\"st\",\"level_id\":\"lv\",\"kind\":\"live.error\"," +
                       "\"payload\":{\"code\":\"invalid_payload_body\",\"message\":\"bad\"}}";
            Assert.That(LiveV1Wire.TryDeserializeInbound(json, out var msg), Is.True);
            Assert.That(msg.Error, Is.Not.Null);
            Assert.That(msg.Error.Code, Is.EqualTo("invalid_payload_body"));
            Assert.That(msg.Error.Message, Is.EqualTo("bad"));
        }
    }
}
