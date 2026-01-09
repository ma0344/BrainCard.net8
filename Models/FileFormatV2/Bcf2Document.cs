using System.Collections.Generic;
using Newtonsoft.Json;

namespace BrainCard.Models.FileFormatV2;

public sealed class Bcf2Document
{
    [JsonProperty("format")]
    public string Format { get; set; } = "BrainCard";

    [JsonProperty("version")]
    public int Version { get; set; } = 2;

    [JsonProperty("createdUtc")]
    public string? CreatedUtc { get; set; }

    [JsonProperty("canvas")]
    public Bcf2CanvasInfo? Canvas { get; set; }

    [JsonProperty("cards")]
    public List<Bcf2Card> Cards { get; set; } = new();
}
