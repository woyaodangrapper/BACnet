using BACnet.Types.Common;
using BACnet.Types.Values;

namespace BACnet.Stack;

internal struct LogRecord
{
    public DateTime timestamp;

    /* logDatum: CHOICE { */
    public TrendLogValueType type;

    //private Asn1BitString log_status;
    //private bool boolean_value;
    //private float real_value;
    //private uint enum_value;
    //private uint unsigned_value;
    //private int signed_value;
    //private Asn1BitString bitstring_value;
    //private bool null_value;
    //private BacnetError failure;
    //private float time_change;
    private object any_value;

    /* } */

    public Asn1BitString statusFlags;

    public LogRecord(TrendLogValueType type, object value, DateTime stamp, uint status)
    {
        this.type = type;
        timestamp = stamp;
        statusFlags = Asn1BitString.ConvertFromInt(status);
        any_value = null;
        Value = value;
    }

    public object Value
    {
        get
        {
            switch (type)
            {
                case TrendLogValueType.TL_TYPE_ANY:
                    return any_value;

                case TrendLogValueType.TL_TYPE_BITS:
                    return (Asn1BitString)Convert.ChangeType(any_value, typeof(Asn1BitString));

                case TrendLogValueType.TL_TYPE_BOOL:
                    return (bool)Convert.ChangeType(any_value, typeof(bool));

                case TrendLogValueType.TL_TYPE_DELTA:
                    return (float)Convert.ChangeType(any_value, typeof(float));

                case TrendLogValueType.TL_TYPE_ENUM:
                    return (uint)Convert.ChangeType(any_value, typeof(uint));

                case TrendLogValueType.TL_TYPE_ERROR:
                    if (any_value != null)
                        return (Error)Convert.ChangeType(any_value, typeof(Error));
                    else
                        return new Error(ErrorClasses.ERROR_CLASS_DEVICE, ErrorCodes.ERROR_CODE_ABORT_OTHER);

                case TrendLogValueType.TL_TYPE_NULL:
                    return null;

                case TrendLogValueType.TL_TYPE_REAL:
                    return (float)Convert.ChangeType(any_value, typeof(float));

                case TrendLogValueType.TL_TYPE_SIGN:
                    return (int)Convert.ChangeType(any_value, typeof(int));

                case TrendLogValueType.TL_TYPE_STATUS:
                    return (Asn1BitString)Convert.ChangeType(any_value, typeof(Asn1BitString));

                case TrendLogValueType.TL_TYPE_UNSIGN:
                    return (uint)Convert.ChangeType(any_value, typeof(uint));

                default:
                    throw new NotSupportedException();
            }
        }
        set
        {
            switch (type)
            {
                case TrendLogValueType.TL_TYPE_ANY:
                    any_value = value;
                    break;

                case TrendLogValueType.TL_TYPE_BITS:
                    if (value == null) value = new Asn1BitString();
                    if (value.GetType() != typeof(Asn1BitString))
                        value = Asn1BitString.ConvertFromInt((uint)Convert.ChangeType(value, typeof(uint)));
                    any_value = (Asn1BitString)value;
                    break;

                case TrendLogValueType.TL_TYPE_BOOL:
                    if (value == null) value = false;
                    if (value.GetType() != typeof(bool))
                        value = (bool)Convert.ChangeType(value, typeof(bool));
                    any_value = (bool)value;
                    break;

                case TrendLogValueType.TL_TYPE_DELTA:
                    if (value == null) value = (float)0;
                    if (value.GetType() != typeof(float))
                        value = (float)Convert.ChangeType(value, typeof(float));
                    any_value = (float)value;
                    break;

                case TrendLogValueType.TL_TYPE_ENUM:
                    if (value == null) value = (uint)0;
                    if (value.GetType() != typeof(uint))
                        value = (uint)Convert.ChangeType(value, typeof(uint));
                    any_value = (uint)value;
                    break;

                case TrendLogValueType.TL_TYPE_ERROR:
                    value ??= new Error();
                    if (value.GetType() != typeof(Error))
                        throw new ArgumentException();
                    any_value = (Error)value;
                    break;

                case TrendLogValueType.TL_TYPE_NULL:
                    if (value != null) throw new ArgumentException();
                    any_value = value;
                    break;

                case TrendLogValueType.TL_TYPE_REAL:
                    if (value == null) value = (float)0;
                    if (value.GetType() != typeof(float))
                        value = (float)Convert.ChangeType(value, typeof(float));
                    any_value = (float)value;
                    break;

                case TrendLogValueType.TL_TYPE_SIGN:
                    if (value == null) value = 0;
                    if (value.GetType() != typeof(int))
                        value = (int)Convert.ChangeType(value, typeof(int));
                    any_value = (int)value;
                    break;

                case TrendLogValueType.TL_TYPE_STATUS:
                    if (value == null) value = new Asn1BitString();
                    if (value.GetType() != typeof(Asn1BitString))
                        value = Asn1BitString.ConvertFromInt((uint)Convert.ChangeType(value, typeof(uint)));
                    any_value = (Asn1BitString)value;
                    break;

                case TrendLogValueType.TL_TYPE_UNSIGN:
                    if (value == null) value = (uint)0;
                    if (value.GetType() != typeof(uint))
                        value = (uint)Convert.ChangeType(value, typeof(uint));
                    any_value = (uint)value;
                    break;

                default:
                    throw new NotSupportedException();
            }
        }
    }

    public T GetValue<T>()
    {
        return (T)Convert.ChangeType(Value, typeof(T));
    }
}