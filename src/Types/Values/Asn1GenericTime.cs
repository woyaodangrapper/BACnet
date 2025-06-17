using BACnet.Types.Encoding;

namespace BACnet.Types.Values;

internal struct Asn1GenericTime
{
    public Asn1TimestampTags Tag;
    public DateTime Time;
    public ushort Sequence;

    public Asn1GenericTime(DateTime time, Asn1TimestampTags tag, ushort sequence = 0)
    {
        Time = time;
        Tag = tag;
        Sequence = sequence;
    }

    public override readonly string ToString() => $"{Time}";
}