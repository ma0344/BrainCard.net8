using System.Collections.Generic;
using Newtonsoft.Json;

namespace BrainCard.Models.FileFormatV2;

public sealed class Bcf2Stroke
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("tool")]
    public string Tool { get; set; } = "pen";

    [JsonProperty("color")]
    public string Color { get; set; } = "#FF000000";

    [JsonProperty("size")]
    public double Size { get; set; } = 2.0;

    [JsonProperty("opacity")]
    public double? Opacity { get; set; }

    [JsonProperty("deviceKind")]
    public string? DeviceKind { get; set; }

    [JsonProperty("points")]
    public List<Bcf2Point> Points { get; set; } = new();
}
