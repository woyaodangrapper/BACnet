using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace BACnet.Stack.Bvlc;

// ASHRAE BACnet/IP 标准（ISO 16484-5）https://www.iso.org/standard/60641.html 默认全部使用大端字节序（Big Endian）
/**
 * +--------+----------+-------------------+
 * | Byte 0 | Byte 1   | Bytes 2 and 3     |
 * +--------+----------+-------------------+
 * | Type   | Function | Total Length      |
 * +--------+----------+-------------------+
 * **/

/// <summary>
/// Represents the header of a BACnet/IP BVLC (BACnet Virtual Link Control) message.
/// </summary>
/// <remarks>The <see cref="BvlcHeader"/> structure is used to define the fixed-length header of a BACnet/IP BVLC
/// message. It includes fields for the message type, function code, and the total length of the packet. This header is
/// essential for identifying and processing BACnet/IP messages according to the ASHRAE BACnet/IP standard.</remarks>
[StructLayout(LayoutKind.Explicit, Size = 4)]
public struct BvlcHeader : IEquatable<BvlcHeader>
{
    public const byte TypeIPv4 = 0x81;   // BACnet/IP  (Annex J)
    public const byte TypeIPv6 = 0x82;   // BACnet/IPv6 (Addendum aj)

    /// <summary>
    /// Represents the type of the message. This field is fixed to <see langword="0x81"/>.
    /// </summary>
    /// <remarks>The value of this field is always <see langword="0x81"/> and does not change. It is used to
    /// identify the specific type of message in the protocol.</remarks>
    [FieldOffset(0)]
    public byte Type;

    /// <summary>
    /// Represents the function code associated with the operation.
    /// </summary>
    /// <remarks>The function code specifies the type of operation to be performed. For example, a value of
    /// <c>0x0A</c> indicates raw forwarding. The meaning of other values depends on the specific protocol or
    /// context.</remarks>
    [FieldOffset(1)]
    public byte Function;

    /// <summary>
    /// Represents the total length of the packet in bytes, stored in big-endian byte order.
    /// </summary>
    /// <remarks>The value of this field is stored in big-endian format, which may require conversion
    /// depending on the system's endianness when reading or writing.</remarks>
    [FieldOffset(2)]
    public ushort Length;

    public BvlcHeader([NotNull] byte[] bytes)
    {
        if (bytes.Length < 4)
        {
            throw new ArgumentException("BVLC header must be at least 4 bytes.", nameof(bytes));
        }
        this = default;
        Type = bytes[0];
        Function = bytes[1];
        Length = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(2, 2));
    }

    public BvlcHeader(byte type, byte function, ushort length)
    {
        Type = type;
        Function = function;
        Length = length;
    }

    public readonly void ToBytes(out byte[] buffer)
    {
        buffer = new byte[4];
        buffer[0] = Type;
        buffer[1] = Function;
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(2), Length);
    }

    public override readonly bool Equals(object? obj) => obj is BvlcHeader other && Equals(other);

    public readonly bool Equals(BvlcHeader other) =>
        Type == other.Type &&
        Function == other.Function &&
        Length == other.Length;

    public override readonly int GetHashCode() => HashCode.Combine(Type, Function, Length);

    public static bool operator ==(BvlcHeader left, BvlcHeader right) => left.Equals(right);

    public static bool operator !=(BvlcHeader left, BvlcHeader right) => !(left == right);
}