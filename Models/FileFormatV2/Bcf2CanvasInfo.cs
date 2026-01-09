using Newtonsoft.Json;

namespace BrainCard.Models.FileFormatV2;

public sealed class Bcf2CanvasInfo
{
    [JsonProperty("width")]
    public double Width { get; set; }

    [JsonProperty("height")]
    public double Height { get; set; }

    [JsonProperty("dpi")]
    public double Dpi { get; set; } = 96;
}
