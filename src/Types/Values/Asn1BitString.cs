namespace BACnet.Types.Values;

internal struct Asn1BitString
{
    public byte bits_used;
    public byte[] value;

    public byte Length => bits_used;
    public bool this[byte bitNumber] => GetBit(bitNumber);

    public override string ToString()
    {
        var ret = "";
        for (var i = 0; i < bits_used; i++)
        {
            ret += (value[i / 8] & 1 << i % 8) > 0 ? "1" : "0";
        }
        return ret;
    }

    public void SetBit(byte bitNumber, bool v)
    {
        var byteNumber = (byte)(bitNumber / 8);
        byte bitMask = 1;

        if (value == null)
            value = new byte[Serializer.TlvEncoder.MAX_BITSTRING_BYTES];

        if (byteNumber < Serializer.TlvEncoder.MAX_BITSTRING_BYTES)
        {
            /* set max bits used */
            if (bits_used < bitNumber + 1)
                bits_used = (byte)(bitNumber + 1);
            bitMask = (byte)(bitMask << bitNumber - byteNumber * 8);
            if (v)
                value[byteNumber] |= bitMask;
            else
                value[byteNumber] &= (byte)~bitMask;
        }
    }

    public bool GetBit(byte bitNumber)
    {
        var byteNumber = (byte)(bitNumber / 8);

        if (byteNumber >= Serializer.TlvEncoder.MAX_BITSTRING_BYTES || bitNumber >= bits_used)
            throw new ArgumentOutOfRangeException(nameof(bitNumber));

        if (value == null)
            return false;

        var bitMask = (byte)(1 << bitNumber - byteNumber * 8);
        return (value[byteNumber] & bitMask) > 0;
    }

    public static Asn1BitString Parse(string str)
    {
        var ret = new Asn1BitString
        {
            value = new byte[Serializer.TlvEncoder.MAX_BITSTRING_BYTES]
        };

        if (string.IsNullOrEmpty(str))
            return ret;

        ret.bits_used = (byte)str.Length;
        for (var i = 0; i < ret.bits_used; i++)
        {
            var isSet = str[i] == '1';
            if (isSet) ret.value[i / 8] |= (byte)(1 << i % 8);
        }

        return ret;
    }

    public uint ConvertToInt()
    {
        return value != null
            ? BitConverter.ToUInt32(value, 0)
            : 0;
    }

    public static Asn1BitString ConvertFromInt(uint value)
    {
        return new Asn1BitString
        {
            value = BitConverter.GetBytes(value),
            bits_used = (byte)Math.Ceiling(Math.Log(value, 2))
        };
    }
};