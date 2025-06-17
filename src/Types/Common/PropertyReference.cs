using BACnet.Types.Object;

namespace BACnet.Types.Common;

internal struct PropertyReference
{
    public uint propertyIdentifier;
    public uint propertyArrayIndex;        /* optional */

    public PropertyReference(uint id, uint arrayIndex)
    {
        propertyIdentifier = id;
        propertyArrayIndex = arrayIndex;
    }

    public readonly PropertyIds GetPropertyId() => (PropertyIds)propertyIdentifier;

    public override readonly string ToString() => $"{GetPropertyId()}";
}