namespace ITS.Protocol
{
    /// <summary>Normative <c>kind</c> strings for live protocol v1.</summary>
    public static class LiveV1Kinds
    {
        public const string Handshake = "live.handshake";
        public const string HandshakeAck = "live.handshake_ack";
        public const string RunLifecycle = "live.run_lifecycle";
        public const string LevelSnapshot = "live.level_snapshot";
        public const string SimStep = "live.sim_step";
        public const string SimHalt = "live.sim_halt";
        public const string SessionPing = "live.session_ping";

        public const string AdvisoryHint = "live.advisory_hint";
        public const string AdvisoryWarning = "live.advisory_warning";
        public const string AdvisoryNudge = "live.advisory_nudge";
        public const string LiveError = "live.error";
    }
}
