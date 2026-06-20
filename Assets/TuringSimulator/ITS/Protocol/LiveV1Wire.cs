using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.Core.Types;

namespace ITS.Protocol
{
    /// <summary>Maps simulation state to live v1 wire payloads and parses inbound frames.</summary>
    public static class LiveV1Wire
    {
        public static LiveEnvelopeDto BuildEnvelope(
            string kind,
            string sessionId,
            string studentId,
            string levelId,
            object payloadDto)
        {
            var serializer = LiveV1Json.CreateSerializer();
            return new LiveEnvelopeDto
            {
                ProtocolVersion = LiveV1Constants.ProtocolVersion,
                MessageId = Guid.NewGuid().ToString(),
                SentAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                SessionId = sessionId,
                StudentId = studentId,
                LevelId = levelId,
                Kind = kind,
                Payload = JObject.FromObject(payloadDto, serializer)
            };
        }

        public static string SerializeEnvelope(LiveEnvelopeDto envelope) =>
            JsonConvert.SerializeObject(envelope, LiveV1Json.WireSettings);

        public static SimStepPayloadDto SimStepFromDiff(StepDiff d, string phase = "played") =>
            new SimStepPayloadDto
            {
                Phase = phase,
                StepIndex = d.StepIndex,
                PreviousState = d.PreviousState,
                NextState = d.NextState,
                SymbolRead = d.SymbolBefore.ToChar().ToString(),
                SymbolWritten = d.SymbolAfter.ToChar().ToString(),
                HeadIndexBefore = d.HeadIndexBefore,
                HeadIndexAfter = d.HeadIndexAfter
            };

        public static LevelSnapshotPayloadDto LevelSnapshotFromTape(
            string title,
            string description,
            IReadOnlyList<Symbol> symbols,
            int headIndex)
        {
            var tape = new List<string>(symbols.Count);
            for (var i = 0; i < symbols.Count; i++)
                tape.Add(symbols[i].ToChar().ToString());

            return new LevelSnapshotPayloadDto
            {
                Title = title ?? "",
                Description = description ?? "",
                TapeSymbols = tape,
                HeadIndex = headIndex
            };
        }

        public static HandshakePayloadDto DefaultHandshakePayload() =>
            new HandshakePayloadDto
            {
                Client = "unity-turing-simulator",
                ClientVersion = "mvp",
                SupportsCompression = false
            };

        /// <summary>
        /// Parses one WebSocket text frame. Returns false if JSON is invalid or kind is not a known server→client message.
        /// </summary>
        public static bool TryDeserializeInbound(string utf8, out LiveInboundMessage message)
        {
            message = null;
            try
            {
                var env = JsonConvert.DeserializeObject<LiveEnvelopeDto>(utf8, LiveV1Json.WireSettings);
                if (env?.Payload == null || string.IsNullOrEmpty(env.Kind))
                    return false;

                var serializer = LiveV1Json.CreateSerializer();

                switch (env.Kind)
                {
                    case LiveV1Kinds.HandshakeAck:
                        message = new LiveInboundMessage
                        {
                            Kind = env.Kind,
                            HandshakeAck = env.Payload.ToObject<HandshakeAckPayloadDto>(serializer)
                        };
                        return message.HandshakeAck != null;

                    case LiveV1Kinds.AdvisoryHint:
                        message = new LiveInboundMessage
                        {
                            Kind = env.Kind,
                            AdvisoryHint = env.Payload.ToObject<AdvisoryHintPayloadDto>(serializer)
                        };
                        return message.AdvisoryHint != null;

                    case LiveV1Kinds.AdvisoryWarning:
                        message = new LiveInboundMessage
                        {
                            Kind = env.Kind,
                            AdvisoryWarning = env.Payload.ToObject<AdvisoryWarningPayloadDto>(serializer)
                        };
                        return message.AdvisoryWarning != null;

                    case LiveV1Kinds.AdvisoryNudge:
                        message = new LiveInboundMessage
                        {
                            Kind = env.Kind,
                            AdvisoryNudge = env.Payload.ToObject<AdvisoryNudgePayloadDto>(serializer)
                        };
                        return message.AdvisoryNudge != null;

                    case LiveV1Kinds.LiveError:
                        message = new LiveInboundMessage
                        {
                            Kind = env.Kind,
                            Error = env.Payload.ToObject<LiveErrorPayloadDto>(serializer)
                        };
                        return message.Error != null;

                    default:
                        return false;
                }
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
