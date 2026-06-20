namespace ITS.Protocol
{
    /// <summary>Newtonsoft settings for REST ITS endpoints (snake_case, matches FastAPI).</summary>
    public static class ItsRestJson
    {
        public static Newtonsoft.Json.JsonSerializerSettings Settings => LiveV1Json.WireSettings;
    }
}
