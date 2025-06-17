using BACnet.Types.Common;
using BACnet.Types.Encoding;
using BACnet.Types.Object;
using BACnet.Types.Structs;

namespace BACnet.Types.Values;

internal struct Asn1Value
{
    public ApplicationTags Tag;
    public object Data; // Renamed from "Value" to "Data" to avoid conflict with the enclosing type name

    public Asn1Value(ApplicationTags tag, object data)
    {
        Tag = tag;
        Data = data;
    }

    public Asn1Value(object data)
    {
        Data = data;
        Tag = ApplicationTags.BACNET_APPLICATION_TAG_NULL;

        //guess at the tag
        if (data != null)
            Tag = TagFromType(data.GetType());
    }

    public ApplicationTags TagFromType(Type t)
    {
        if (t == typeof(string))
            return ApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING;
        if (t == typeof(int) || t == typeof(short) || t == typeof(sbyte))
            return ApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT;
        if (t == typeof(uint) || t == typeof(ushort) || t == typeof(byte))
            return ApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT;
        if (t == typeof(bool))
            return ApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN;
        if (t == typeof(float))
            return ApplicationTags.BACNET_APPLICATION_TAG_REAL;
        if (t == typeof(double))
            return ApplicationTags.BACNET_APPLICATION_TAG_DOUBLE;
        if (t == typeof(Asn1BitString))
            return ApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING;
        if (t == typeof(ObjectId))
            return ApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID;
        if (t == typeof(Error))
            return ApplicationTags.BACNET_APPLICATION_TAG_ERROR;
        if (t == typeof(Asn1DeviceObjectPropertyReference))
            return ApplicationTags.BACNET_APPLICATION_TAG_OBJECT_PROPERTY_REFERENCE;
        if (t.IsEnum)
            return ApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED;

        return ApplicationTags.BACNET_APPLICATION_TAG_CONTEXT_SPECIFIC_ENCODED;
    }

    public T As<T>()
    {
        if (typeof(T) == typeof(DateTime))
        {
            switch (Tag)
            {
                case ApplicationTags.BACNET_APPLICATION_TAG_DATE:
                case ApplicationTags.BACNET_APPLICATION_TAG_DATETIME:
                case ApplicationTags.BACNET_APPLICATION_TAG_TIME:
                case ApplicationTags.BACNET_APPLICATION_TAG_TIMESTAMP:
                    return (T)Data;
            }
        }

        if (typeof(T) == typeof(TimeSpan) && Tag == ApplicationTags.BACNET_APPLICATION_TAG_TIME)
            return (T)(dynamic)((DateTime)Data).TimeOfDay;

        if (typeof(T) != typeof(object) && TagFromType(typeof(T)) != Tag)
            throw new ArgumentException($"Value with tag {Tag} can't be converted to {typeof(T).Name}");

        // ReSharper disable once RedundantCast
        // This is needed for casting to enums
        return (T)(dynamic)Data;
    }

    public override string ToString()
    {
        if (Data == null)
            return string.Empty;

        if (Data.GetType() != typeof(byte[]))
            return Data.ToString();

        var tmp = (byte[])Data;
        return tmp.Aggregate(string.Empty, (current, b) =>
            current + b.ToString("X2"));
    }
}