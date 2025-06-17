using BACnet.Types.Common;
using BACnet.Types.Object;

namespace BACnet.Types.Structs;

internal struct Asn1ReadAccessSpecification(ObjectId objectIdentifier, IList<PropertyReference> propertyReferences)
{
    public ObjectId objectIdentifier = objectIdentifier;
    public IList<PropertyReference> propertyReferences = propertyReferences;

    public static object Parse(string value)
    {
        var ret = new Asn1ReadAccessSpecification();
        if (string.IsNullOrEmpty(value)) return ret;
        var tmp = value.Split(':');
        if (tmp.Length < 2) return ret;
        ret.objectIdentifier.type = Enum.Parse<ObjectTypes>(tmp[0]);
        ret.objectIdentifier.instance = uint.Parse(tmp[1]);
        var refs = new List<PropertyReference>();
        for (var i = 2; i < tmp.Length; i++)
        {
            refs.Add(new PropertyReference
            {
                propertyArrayIndex = Serializer.TlvEncoder.BACNET_ARRAY_ALL,
                propertyIdentifier = (uint)Enum.Parse<PropertyIds>(tmp[i])
            });
        }
        ret.propertyReferences = refs;
        return ret;
    }

    public override string ToString()
    {
        return propertyReferences.Aggregate(objectIdentifier.ToString(), (current, r) =>
            $"{current}:{(PropertyIds)r.propertyIdentifier}");
    }
}