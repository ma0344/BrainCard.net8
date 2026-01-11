using System.Runtime.Serialization;

namespace BrainCard.Legacy;

[DataContract]
public class LegacySavedImage
{
    [DataMember]
    public string Id { get; set; }

    [DataMember]
    public double X { get; set; }

    [DataMember]
    public double Y { get; set; }

    [DataMember]
    public int Z { get; set; }

    [DataMember]
    public string RecogText { get; set; }

    [DataMember]
    public string InkData { get; set; }
}
