using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ITS.Protocol
{
    // --- Client → server (outbound from Unity) ---------------------------------

    public sealed class HandshakePayloadDto
    {
        public string Client { get; set; }
        public string ClientVersion { get; set; }
        public bool SupportsCompression { get; set; }
    }

    public sealed class RunLifecyclePayloadDto
    {
        public string Phase { get; set; }
    }

    public sealed class LevelSnapshotPayloadDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public List<string> TapeSymbols { get; set; }
        public int HeadIndex { get; set; }
        public JObject ProgramSummary { get; set; }
    }

    public sealed class SimStepPayloadDto
    {
        public string Phase { get; set; }
        public int StepIndex { get; set; }
        public int PreviousState { get; set; }
        public int NextState { get; set; }
        public string SymbolRead { get; set; }
        public string SymbolWritten { get; set; }
        public int HeadIndexBefore { get; set; }
        public int HeadIndexAfter { get; set; }
        public string GameFlowState { get; set; }
    }

    public sealed class SimHaltPayloadDto
    {
        public string HaltStatus { get; set; }
    }

    // --- Server → client (inbound to Unity) ----------------------------------

    public sealed class HandshakeAckPayloadDto
    {
        public string Server { get; set; }
        public string AcceptedProtocolVersion { get; set; }
        public List<string> Capabilities { get; set; }
    }

    public sealed class AdvisoryHintPayloadDto
    {
        public string Text { get; set; }
        public string SkillId { get; set; }
        public int? HintLevel { get; set; }
        public string Urgency { get; set; }
    }

    public sealed class AdvisoryWarningPayloadDto
    {
        public string Text { get; set; }
        public string SkillId { get; set; }
        public string Urgency { get; set; }
    }

    public sealed class AdvisoryNudgePayloadDto
    {
        public string Text { get; set; }
    }

    public sealed class LiveErrorPayloadDto
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public JObject Details { get; set; }
    }
}
