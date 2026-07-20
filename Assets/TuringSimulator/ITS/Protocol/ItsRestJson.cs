using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ITS.Protocol
{
    /// <summary>Newtonsoft settings for REST ITS endpoints (snake_case, matches FastAPI).</summary>
    public static class ItsRestJson
    {
        public static JsonSerializerSettings Settings { get; } = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            },
            NullValueHandling = NullValueHandling.Ignore
        };
    }
}
