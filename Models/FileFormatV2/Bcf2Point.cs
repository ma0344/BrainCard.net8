using Newtonsoft.Json;

namespace BrainCard.Models.FileFormatV2;

public sealed class Bcf2Point
{
    [JsonProperty("x")]
    public double X { get; set; }

    [JsonProperty("y")]
    public double Y { get; set; }

    [JsonProperty("pr")]
    public double Pressure { get; set; } = 1.0;

    [JsonProperty("t")]
    public int T { get; set; }
}
