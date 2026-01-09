using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BrainCard.Serialization;

internal static class JsonSettings
{
    public static readonly JsonSerializerSettings Default = new()
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new DefaultNamingStrategy()
        },
        NullValueHandling = NullValueHandling.Ignore,
        Formatting = Formatting.Indented
    };
}
