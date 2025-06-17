using BACnet.Extensions;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BACnet.Stack.Bvlc;

/**
 *
 * +--------+----------+-------------------+--------------------------+------------------+
 * | Byte 0 | Byte 1   | Bytes 2 and 3     | Bytes 4 ~ 7              | Bytes 8 ~ 9      |
 * +--------+----------+-------------------+--------------------------+------------------+
 * | Type   | Function | Total Length      | Original IPv4 Address (4) | Original UDP Port |
 * +--------+----------+-------------------+--------------------------+------------------+
 *
 * **/

[StructLayout(LayoutKind.Explicit, Size = 10)]
public struct ForwardHeaderIPv4 : IEquatable<ForwardHeaderIPv4>
{
    [FieldOffset(0)] public BvlcHeader Header;
    [FieldOffset(4)] public uint Ip;
    [FieldOffset(8)] public ushort Port;

    public ForwardHeaderIPv4(ReadOnlySpan<byte> bytes)
    {
        this = default;
        if (!BvlcHeaderExtensions.TryParse(bytes, out Header))
        {
            throw new ArgumentException("Buffer too small for ForwardHeaderIPv4.");
        }
        Ip = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(4, 4));
        Port = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(8, 2));
    }

    public readonly bool Equals(ForwardHeaderIPv4 other) =>
           Header.Equals(other.Header) && Ip == other.Ip && Port == other.Port;

    public override readonly bool Equals([NotNullWhen(true)] object? obj) =>
        obj is ForwardHeaderIPv4 other && Equals(other);

    public override readonly int GetHashCode() => HashCode.Combine(Header, Ip, Port);

    public static bool operator ==(ForwardHeaderIPv4 left, ForwardHeaderIPv4 right) => left.Equals(right);

    public static bool operator !=(ForwardHeaderIPv4 left, ForwardHeaderIPv4 right) => !(left == right);
}

/**
 *
 * +--------+----------+-------------------+--------------------------------------+------------------+
 * | Byte 0 | Byte 1   | Bytes 2 and 3     | Bytes 4 ~ 19                        | Bytes 20 ~ 21    |
 * +--------+----------+-------------------+--------------------------------------+------------------+
 * | Type   | Function | Total Length      | Original IPv6 Address (16 bytes)     | Original UDP Port |
 * +--------+----------+-------------------+--------------------------------------+------------------+
 *
 * **/

[StructLayout(LayoutKind.Explicit, Size = 22)]
public struct ForwardHeaderIPv6 : IEquatable<ForwardHeaderIPv6>
{
    [FieldOffset(0)] public BvlcHeader Header;
    [FieldOffset(4)] public UInt128Like Ip;
    [FieldOffset(20)] public ushort Port;

    public ForwardHeaderIPv6(ReadOnlySpan<byte> bytes)
    {
        this = default;
        if (!BvlcHeaderExtensions.TryParse(bytes, out Header))
        {
            throw new ArgumentException("Buffer too small for ForwardHeaderIPv4.");
        }
        Ip = new UInt128Like(bytes.Slice(4, 16));
        Port = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(20, 2));
    }

    public override readonly bool Equals([NotNullWhen(true)] object? obj)
        => obj is ForwardHeaderIPv6 other && Header.Equals(other.Header) && Ip.Equals(other.Ip) && Port == other.Port;

    public override readonly int GetHashCode() => HashCode.Combine(Header, Ip, Port);

    public static bool operator ==(ForwardHeaderIPv6 left, ForwardHeaderIPv6 right) => left.Equals(right);

    public static bool operator !=(ForwardHeaderIPv6 left, ForwardHeaderIPv6 right) => !(left == right);

    public bool Equals(ForwardHeaderIPv6 other) => Header.Equals(other.Header) && Ip.Equals(other.Ip) && Port == other.Port;
}

[StructLayout(LayoutKind.Explicit, Size = 16)]
public readonly struct UInt128Like : IEquatable<UInt128Like>
{
    [FieldOffset(0)] public readonly ulong A;
    [FieldOffset(8)] public readonly ulong B;

    public void ToBytes(out byte[] buffer)
    {
        Span<byte> span = MemoryMarshal.AsBytes(
            MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in this), 1)
        );
        buffer = span.ToArray();
    }

    public UInt128Like(byte[] bytes) => this = MemoryMarshal.Read<UInt128Like>(bytes.AsSpan()[..16]);

    public UInt128Like(ReadOnlySpan<byte> bytes) => this = MemoryMarshal.Read<UInt128Like>(bytes[..16]);

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is UInt128Like other && A == other.A && B == other.B;

    public override int GetHashCode() => HashCode.Combine(A, B);

    public static bool operator ==(UInt128Like left, UInt128Like right) => left.Equals(right);

    public static bool operator !=(UInt128Like left, UInt128Like right) => !(left == right);

    public bool Equals(UInt128Like other) => A == other.A && B == other.B;
}