namespace ITS.Protocol
{
    /// <summary>Parsed inbound live frame (server → client kinds only).</summary>
    public sealed class LiveInboundMessage
    {
        public string Kind { get; set; }

        public HandshakeAckPayloadDto HandshakeAck { get; set; }
        public AdvisoryHintPayloadDto AdvisoryHint { get; set; }
        public AdvisoryWarningPayloadDto AdvisoryWarning { get; set; }
        public AdvisoryNudgePayloadDto AdvisoryNudge { get; set; }
        public LiveErrorPayloadDto Error { get; set; }
    }
}
