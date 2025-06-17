using BACnet.Stack.Bvlc;
using System.Buffers.Binary;

namespace BACnet.Extensions;

internal static class BvlcHeaderExtensions
{
    /// <summary>
    /// 包头长度常量，表示 BVLC 包头的固定长度为 4 字节。
    /// </summary>
    private const int BVLC_HEADER_SIZE = 4;

    public static bool TryParse(ReadOnlySpan<byte> buffer, out BvlcHeader header)
    {
        header = default;

        if (buffer.Length < BVLC_HEADER_SIZE)
            return false;

        header = new BvlcHeader
        {
            Type = buffer[0],
            Function = buffer[1],
            Length = BinaryPrimitives.ReadUInt16BigEndian(buffer[2..4]) // (b2<<8)|(b3<<0)
        };

        return true;
    }

    public static bool TryWrite(this BvlcHeader header, Span<byte> buffer)
    {
        if (buffer.Length < BVLC_HEADER_SIZE)
            return false;

        buffer[0] = header.Type;
        buffer[1] = header.Function;
        BinaryPrimitives.WriteUInt16BigEndian(buffer[2..4], header.Length);
        return true;
    }
}