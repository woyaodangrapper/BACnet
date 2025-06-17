using BACnet.Types.Common;
using BACnet.Types.Object;

namespace BACnet.Types.Structs;

internal struct Asn1ReadAccessResult
{
    public ObjectId objectIdentifier;
    public IList<PropertyValue> values;

    public Asn1ReadAccessResult(ObjectId objectIdentifier, IList<PropertyValue> values)
    {
        this.objectIdentifier = objectIdentifier;
        this.values = values;
    }
}