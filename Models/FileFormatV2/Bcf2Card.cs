using Newtonsoft.Json;

namespace BrainCard.Models.FileFormatV2;

public sealed class Bcf2Card
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("x")]
    public double X { get; set; }

    [JsonProperty("y")]
    public double Y { get; set; }

    [JsonProperty("z")]
    public int Z { get; set; }

    [JsonProperty("recognizedText")]
    public string? RecognizedText { get; set; }

    [JsonProperty("previewPng")]
    public string? PreviewPng { get; set; }

    [JsonProperty("ink")]
    public Bcf2InkData? Ink { get; set; }
}
