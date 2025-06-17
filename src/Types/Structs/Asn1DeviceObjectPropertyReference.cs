using BACnet.Contracts;
using BACnet.Serializer;
using BACnet.Types.Common;
using BACnet.Types.Object;

namespace BACnet.Types.Structs;

internal struct Asn1DeviceObjectPropertyReference : IEncode
{
    public ObjectId objectIdentifier;
    public PropertyIds propertyIdentifier;
    public uint arrayIndex;
    public ObjectId deviceIndentifier;

    public Asn1DeviceObjectPropertyReference(ObjectId objectIdentifier, PropertyIds propertyIdentifier, ObjectId? deviceIndentifier = null, uint arrayIndex = TlvEncoder.BACNET_ARRAY_ALL)
    {
        this.objectIdentifier = objectIdentifier;
        this.propertyIdentifier = propertyIdentifier;
        this.arrayIndex = arrayIndex;
        this.deviceIndentifier = deviceIndentifier ?? new ObjectId(ObjectTypes.MAX_BACNET_OBJECT_TYPE, 0);
    }

    public void Encode(EncodeBuffer buffer) => TlvEncoder.bacapp_encode_device_obj_property_ref(buffer, this);

    public ObjectId ObjectId
    {
        get => objectIdentifier;
        set => objectIdentifier = value;
    }

    public int ArrayIndex // shows -1 when it's Asn1.BACNET_ARRAY_ALL
    {
        get => arrayIndex != TlvEncoder.BACNET_ARRAY_ALL
            ? (int)arrayIndex
            : -1;
        set => arrayIndex = value < 0
            ? TlvEncoder.BACNET_ARRAY_ALL
            : (uint)value;
    }

    public ObjectId? DeviceId  // shows null when it's not OBJECT_DEVICE
    {
        get
        {
            return deviceIndentifier.type == ObjectTypes.OBJECT_DEVICE
                ? deviceIndentifier
                : null;
        }

        set => deviceIndentifier = value ?? new ObjectId();
    }

    public PropertyIds PropertyId
    {
        get => propertyIdentifier;
        set => propertyIdentifier = value;
    }

    public static object Parse(string value)
    {
        var parts = value.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

        ObjectId? deviceId = null;
        ObjectId objectId;

        switch (parts.Length)
        {
            case 2:
                objectId = ObjectId.Parse(parts[0]);
                break;

            case 3:
                deviceId = ObjectId.Parse(parts[0]);
                objectId = ObjectId.Parse(parts[1]);
                break;

            default:
                throw new ArgumentException("Invalid format", nameof(value));
        }

        if (!Enum.TryParse(parts.Last(), out PropertyIds propertyId))
        {
            if (!uint.TryParse(parts.Last(), out var vendorSpecificPropertyId))
                throw new ArgumentException("Invalid format of property id", nameof(value));

            propertyId = (PropertyIds)vendorSpecificPropertyId;
        }

        return new Asn1DeviceObjectPropertyReference
        {
            DeviceId = deviceId,
            ObjectId = objectId,
            PropertyId = propertyId,
            ArrayIndex = -1
        };
    }

    public override string ToString()
    {
        return DeviceId != null
            ? $"{DeviceId}.{ObjectId}.{PropertyId}"
            : $"{ObjectId}.{PropertyId}";
    }
}