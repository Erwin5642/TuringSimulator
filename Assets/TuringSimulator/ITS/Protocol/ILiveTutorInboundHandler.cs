namespace ITS.Protocol
{
    /// <summary>Handles parsed server→client live messages (for tests or custom routing).</summary>
    public interface ILiveTutorInboundHandler
    {
        void OnHandshakeAck(HandshakeAckPayloadDto ack);
        void OnAdvisoryText(string kind, string text);
        void OnError(string code, string message);
    }
}
