using BACnet.Types.Values;

namespace BACnet.Types.Common;

internal struct PropertyValue
{
    public PropertyReference property;
    public IList<Asn1Value> value;
    public byte priority;

    public override string ToString() => property.ToString();
}