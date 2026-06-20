using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ITS.Protocol
{
    /// <summary>Snake_case on wire to match FastAPI / Pydantic JSON.</summary>
    public static class LiveV1Json
    {
        public static JsonSerializerSettings WireSettings { get; } = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            },
            NullValueHandling = NullValueHandling.Ignore
        };

        public static JsonSerializer CreateSerializer() => JsonSerializer.Create(WireSettings);
    }
}
