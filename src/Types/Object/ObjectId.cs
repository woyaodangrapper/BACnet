using System.Text.RegularExpressions;

namespace BACnet.Types.Object;

[Serializable]
internal struct ObjectId : IComparable<ObjectId>
{
    internal ObjectTypes type;
    public uint instance;

    public ObjectTypes Type
    {
        readonly get => type;
        set => type = value;
    }

    public uint Instance
    {
        get => instance;
        set => instance = value;
    }

    public ObjectId(ObjectTypes type, uint instance)
    {
        this.type = type;
        this.instance = instance;
    }

    public override string ToString() => $"{Type}:{Instance}";

    public override int GetHashCode() => ToString().GetHashCode();

    public override bool Equals(object obj) => obj != null && obj.ToString().Equals(ToString());

    public int CompareTo(ObjectId other)
    {
        if (Type == other.Type)
            return Instance.CompareTo(other.Instance);

        if (Type == ObjectTypes.OBJECT_DEVICE)
            return -1;

        if (other.Type == ObjectTypes.OBJECT_DEVICE)
            return 1;

        // cast to int for comparison otherwise unpredictable behaviour with outbound enum (proprietary type)
        return ((int)Type).CompareTo((int)other.Type);
    }

    public static bool operator ==(ObjectId a, ObjectId b) => a.Equals(b);

    public static bool operator !=(ObjectId a, ObjectId b) => !(a == b);

    public static ObjectId Parse(string value)
    {
        var pattern = new Regex($"(?<{nameof(Type)}>.+):(?<{nameof(Instance)}>.+)");

        if (string.IsNullOrEmpty(value) || !pattern.IsMatch(value))
            return new ObjectId();

        var objectType = Enum.Parse<ObjectTypes>(pattern.Match(value).Groups[nameof(Type)].Value);

        var objectInstance = uint.Parse(pattern.Match(value).Groups[nameof(Instance)].Value);

        return new ObjectId(objectType, objectInstance);
    }
};