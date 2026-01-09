using System.Collections.Generic;
using Newtonsoft.Json;

namespace BrainCard.Models.FileFormatV2;

public sealed class Bcf2InkData
{
    [JsonProperty("model")]
    public string Model { get; set; } = "stroke-v1";

    [JsonProperty("units")]
    public string Units { get; set; } = "dip";

    [JsonProperty("strokes")]
    public List<Bcf2Stroke> Strokes { get; set; } = new();
}
