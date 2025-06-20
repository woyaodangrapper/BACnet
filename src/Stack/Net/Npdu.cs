using BACnet.Contracts;
using BACnet.Serializer;
using BACnet.Types.Common;

namespace BACnet.Stack.Net;

/// <summary>
///	编码 NPDU，管理路由、Hop Count、Destination
/// </summary>
internal class Npdu : INpdu
{
    private readonly IBvlc _bvlc;
    private readonly ILogger<Npdu> _logger;

    public Npdu(IBvlc bvlc, ILoggerFactory loggerFactory)
    {
        _bvlc = bvlc ?? throw new ArgumentNullException(nameof(bvlc));
        _logger = loggerFactory?.CreateLogger<Npdu>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public static NpduDecodeResult Decode(ReadOnlySpan<byte> buffer, ref int offset)
    {
        int startOffset = offset;

        // 协议版本
        if (buffer.Length - offset < 2)
        {
            return NpduDecodeResult.Invalid();
        }

        byte protocolVersion = buffer[offset++];
        if (protocolVersion != 0x01)
        {
            return NpduDecodeResult.Invalid(-1);
        }

        // 控制字段
        NpduControlFlags function = (NpduControlFlags)buffer[offset++];

        // Destination
        Address destination;
        if ((function & NpduControlFlags.DestinationSpecified) == NpduControlFlags.DestinationSpecified)
        {
            if (buffer.Length - offset < 3)
            {
                return NpduDecodeResult.Invalid(-1);
            }

            ushort destNet = (ushort)((buffer[offset] << 8) | buffer[offset + 1]);
            offset += 2;

            int destAdrLen = buffer[offset++];
            if (buffer.Length - offset < destAdrLen)
            {
                return NpduDecodeResult.Invalid(-1);
            }

            byte[] destAdr = new byte[destAdrLen];
            if (destAdrLen > 0)
            {
                buffer.Slice(offset, destAdrLen).CopyTo(destAdr);
                offset += destAdrLen;
            }

            destination = CreateAddress(destNet, destAdr);
        }
        else
        {
            destination = CreateAddress(0, []);
        }

        // source
        Address source;
        if ((function & NpduControlFlags.SourceSpecified) == NpduControlFlags.SourceSpecified)
        {
            if (buffer.Length - offset < 3)
            {
                return NpduDecodeResult.Invalid(-1);
            }

            ushort srcNet = (ushort)((buffer[offset] << 8) | buffer[offset + 1]);
            offset += 2;

            int srcAdrLen = buffer[offset++];
            if (buffer.Length - offset < srcAdrLen)
            {
                return NpduDecodeResult.Invalid(-1);
            }

            byte[] srcAdr = new byte[srcAdrLen];
            if (srcAdrLen > 0)
            {
                buffer.Slice(offset, srcAdrLen).CopyTo(srcAdr);
                offset += srcAdrLen;
            }

            source = CreateAddress(srcNet, srcAdr);
        }
        else
        {
            source = CreateAddress(0, []);
        }

        byte hopCount = 0;
        if ((function & NpduControlFlags.DestinationSpecified) == NpduControlFlags.DestinationSpecified)
        {
            if (buffer.Length - offset < 1)
            {
                return NpduDecodeResult.Invalid(-1);
            }
            hopCount = buffer[offset++];
        }

        NpduMessageTypes networkMsgType = NpduMessageTypes.NETWORK_MESSAGE_WHO_IS_ROUTER_TO_NETWORK;
        ushort vendorId = 0;

        if (function.HasFlag(NpduControlFlags.NetworkLayerMessage))
        {
            if (buffer.Length - offset < 1)
            {
                return NpduDecodeResult.Invalid(-1);
            }

            networkMsgType = (NpduMessageTypes)buffer[offset++];

            if ((byte)networkMsgType >= 0x80)
            {
                if (buffer.Length - offset < 2)
                {
                    return NpduDecodeResult.Invalid(-1);
                }

                vendorId = (ushort)((buffer[offset] << 8) | buffer[offset + 1]);
                offset += 2;
            }
        }

        int bytesRead = offset - startOffset;

        return new NpduDecodeResult(
            Result: bytesRead,
            Function: function,
            Destination: destination,
            Source: source,
            HopCount: hopCount,
            NetworkMsgType: networkMsgType,
            VendorId: vendorId);
    }

    public static void Encode(EncodeBuffer buffer, NpduControlFlags flag, Address destination, Address source,
        byte hopCount, NpduMessageTypes type, ushort vendorId)
    {
        Encode(buffer, flag, destination, source, hopCount);

        if (flag.HasFlag(NpduControlFlags.NetworkLayerMessage))
        {
            buffer.Add((byte)type);

            if ((byte)type >= 0x80)
            {
                buffer.Add((byte)(vendorId >> 8));
                buffer.Add((byte)(vendorId & 0x00FF));
            }
        }
    }

    public static void Encode(EncodeBuffer buffer, NpduControlFlags function, Address destination, Address? source = null, byte hopCount = 0xFF)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        bool hasDestination = destination is { Net: > 0 };
        bool hasSource = source is { Net: > 0 and not 0xFFFF };// .NET 8 属性模式写法

        buffer.Add(0x01);

        NpduControlFlags flags =
            function
            | (hasDestination ? NpduControlFlags.DestinationSpecified : 0)
            | (hasSource ? NpduControlFlags.SourceSpecified : 0);

        buffer.Add((byte)flags);

        if (hasSource)
        {
            EncodeAddress(buffer, source!);
        }

        if (hasDestination)
        {
            EncodeAddress(buffer, destination);
        }

        // 写入跳数
        if (hasDestination)
        {
            buffer.Add(hopCount);
        }
    }

    private static void EncodeAddress(EncodeBuffer buf, Address addr)
    {
        // 网络号高、低字节
        buf.Add((byte)(addr.Net >> 8));
        buf.Add((byte)(addr.Net & 0x00FF));

        // 0xFFFF 表示广播网络，地址长度固定为 0
        if (addr.Net == 0xFFFF)
        {
            buf.Add(0x00);
            return;
        }

        // 写入地址长度
        int addressLength = addr.Adr.Length;
        buf.Add((byte)addressLength);

        // 写入地址本体
        foreach (byte octet in addr.Adr)
        {
            buf.Add(octet);
        }
    }

    private static Address CreateAddress(ushort net, byte[] adr)
    {
        AddressTypes type = adr.Length switch
        {
            6 => AddressTypes.IP,
            18 => AddressTypes.IPV6,
            0 => AddressTypes.None,
            _ => AddressTypes.None
        };

        return new Address(type, net, adr);
    }
}

internal readonly record struct NpduDecodeResult(
    int Result,
    NpduControlFlags Function,
    Address Destination,
    Address Source,
    byte HopCount,
    NpduMessageTypes NetworkMsgType,
    ushort VendorId)
{
    public static NpduDecodeResult Invalid(int result = -1)
        => new(result, default, default!, default!, 0, default, 0);

    public bool IsSuccess => Result >= 0;
}

/**
 *
 * Byte Offset | 含义
 * ------------|------------------------
 * 0           | 协议版本号（必须是 0x01）
 * 1           | 控制字段（Function Flags）
 * 2+          | 可选：DNET/DADR/SNET/SADR/HopCount...
 *
 * **/