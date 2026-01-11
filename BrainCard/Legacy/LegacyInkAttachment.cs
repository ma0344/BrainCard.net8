using System;

namespace BrainCard.Legacy;

public sealed class LegacyInkAttachment
{
    public LegacyInkAttachment(byte[] isfBytes)
    {
        IsfBytes = isfBytes;
    }

    public byte[] IsfBytes { get; }

    public bool HasIsf => IsfBytes != null && IsfBytes.Length > 0;
}
