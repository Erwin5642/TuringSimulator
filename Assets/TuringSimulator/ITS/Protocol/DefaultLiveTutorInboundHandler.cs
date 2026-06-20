using UnityEngine;

namespace ITS.Protocol
{
    /// <summary>Default routing: log handshake, speak advisory text, log errors.</summary>
    public sealed class DefaultLiveTutorInboundHandler : ILiveTutorInboundHandler
    {
        public static readonly DefaultLiveTutorInboundHandler Instance = new DefaultLiveTutorInboundHandler();

        public void OnHandshakeAck(HandshakeAckPayloadDto ack)
        {
            if (ack != null)
                Debug.Log($"[LiveTutor] handshake_ack server={ack.Server} version={ack.AcceptedProtocolVersion}");
        }

        public void OnAdvisoryText(string kind, string text)
        {
            if (!string.IsNullOrEmpty(text) && AgentDialogue.Instance != null)
                AgentDialogue.Instance.SayAndSpeak(text);
        }

        public void OnError(string code, string message) =>
            Debug.LogWarning($"[LiveTutor] server error code={code} message={message}");
    }
}
