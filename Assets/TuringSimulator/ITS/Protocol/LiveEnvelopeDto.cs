using Newtonsoft.Json.Linq;

namespace ITS.Protocol
{
    /// <summary>Wire envelope for live protocol v1 (matches FastAPI live envelope fields).</summary>
    public sealed class LiveEnvelopeDto
    {
        public string ProtocolVersion { get; set; }
        public string MessageId { get; set; }
        public string CorrelationId { get; set; }
        public long SentAtUnixMs { get; set; }
        public string SessionId { get; set; }
        public string StudentId { get; set; }
        public string LevelId { get; set; }
        public string Kind { get; set; }
        public JObject Payload { get; set; }
    }
}
