// Ignore Spelling: npdu

using BACnet.Contracts;
using BACnet.Extensions;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;

namespace BACnet.Stack.Bvlc;

/// <summary>
/// 封装 BVLC Function、Forward、Broadcast
/// </summary>
internal class Bvlc : IBvlc
{
    private readonly IChannel _channel;
    private readonly ILogger<Bvlc> _logger;

    private readonly string _broadcast = NetworkExtension.GetBroadcastAddress()?.ToString() ?? "";

    private readonly Subject<BvlcMessage> _messageSubject = new();

    public IObservable<BvlcMessage> MessageReceived => _messageSubject;

    private readonly int _defaultPort;
    private readonly DeviceType _deviceType;
    private readonly BvlcStateMachine _bvlcState;

    /// <summary>
    /// 存储 BACnet 网络中的广播管理设备（BBMD）。
    /// </summary>
    private readonly ConcurrentDictionary<IPEndPoint, IPAddress> _routerDevices = [];

    /// <summary>
    /// 存储 BACnet 网络中的远程注册的外部设备及时间。
    /// </summary>
    private readonly ConcurrentDictionary<IPEndPoint, DateTime> _foreignDevices = new();

    public Bvlc(IChannel channel, ILoggerFactory loggerFactory, BvlcStateMachine? bvlcState = null, int defaultPort = 47808, DeviceType deviceType = DeviceType.Non)
    {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        _logger = loggerFactory?.CreateLogger<Bvlc>() ?? throw new ArgumentNullException(nameof(loggerFactory));

        _bvlcState = bvlcState ?? new();
        _defaultPort = defaultPort;
        _deviceType = deviceType;
    }

    public async ValueTask<BvlcDecodeResult> DecodeAsync(
        byte[] buffer, int offset, IPEndPoint sender, CancellationToken cancellationToken = default)
    {
        // 校验参数
        scoped Span<byte> slice = buffer.AsSpan(offset);
        if (!BvlcHeaderExtensions.TryParse(slice, out BvlcHeader header))
            return BvlcDecodeResult.Invalid();

        // 校验包长度
        if (slice.Length < header.Length)
            return BvlcDecodeResult.Invalid();

        // 校验Type是否合法
        if (header.Type != BvlcHeader.TypeIPv4 && header.Type != BvlcHeader.TypeIPv6)
            return BvlcDecodeResult.Invalid();

        if (!TryGetFunction(header.Function, out BvlcFunction function))
        {
            return BvlcDecodeResult.Invalid(func: function);
        }

        switch (function)
        {
            case BvlcFunction.BVLC_RESULT:
                ushort resultCode = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(4, 2));
                OnMessageReceived(sender, function, resultCode, null);
                return new BvlcDecodeResult(0, function, header.Length);   // not for the upper layers

            case BvlcFunction.BVLC_ORIGINAL_UNICAST_NPDU:
                return new BvlcDecodeResult(4, function, header.Length);   // only for the upper layers

            case BvlcFunction.BVLC_ORIGINAL_BROADCAST_NPDU: // Normaly received in an IP local or global broadcast packet
                                                            // Send to FDs & BBMDs, not broadcast or it will be made twice !
                await _bvlcState.ExecuteIfStateAsync(() => ForwardNpduAsync(buffer, header.Length, false, sender)
                , DeviceState.Running).ConfigureAwait(false);

                return new BvlcDecodeResult(4, function, header.Length);   // also for the upper layers

            case BvlcFunction.BVLC_FORWARDED_NPDU:   // Sent only by a BBMD, broadcast on it network, or broadcast demand by one of it's FDs

                bool ret = _routerDevices.Any(items => items.Key.Address.Equals(sender.Address));

                await _bvlcState.ExecuteIfAsync(() => ret && header.Length >= 10 && _bvlcState.Current == DeviceState.Running, async () =>
                {
                    Memory<byte> forwardedMemory = buffer.AsMemory(offset, header.Length);
                    IPEndPoint broadcastEndpoint = new(IPAddress.Parse(_broadcast), _defaultPort);
                    // Forward to all BBMDs and FDs, but not the original sender
                    await SendToForeignAsync(forwardedMemory).ConfigureAwait(false);
                    // Forward to all BBMDs, but not the original sender
                    await _channel.TryWriteAsync(forwardedMemory, broadcastEndpoint).ConfigureAwait(false);
                }).ConfigureAwait(false);

                return new BvlcDecodeResult(10, function, header.Length);  // also for the upper layers

            case BvlcFunction.BVLC_DISTRIBUTE_BROADCAST_TO_NETWORK:  // Sent by a Foreign Device, not a BBMD

                return new BvlcDecodeResult(0, function, header.Length);   // not for the upper layers

            case BvlcFunction.BVLC_REGISTER_FOREIGN_DEVICE:

                return new BvlcDecodeResult(0, function, header.Length);  // not for the upper layers

            // We don't care about Read/Write operation in the BBMD/FDR tables (who realy use it ?)
            case BvlcFunction.BVLC_READ_FOREIGN_DEVICE_TABLE:
                return new BvlcDecodeResult(0, function, header.Length);

            case BvlcFunction.BVLC_DELETE_FOREIGN_DEVICE_TABLE_ENTRY:
                return new BvlcDecodeResult(0, function, header.Length);

            case BvlcFunction.BVLC_READ_BROADCAST_DIST_TABLE:
                return new BvlcDecodeResult(0, function, header.Length);

            case BvlcFunction.BVLC_WRITE_BROADCAST_DISTRIBUTION_TABLE:
            case BvlcFunction.BVLC_READ_BROADCAST_DIST_TABLE_ACK:
                {
                    return new BvlcDecodeResult(0, function, header.Length);
                }

            case BvlcFunction.BVLC_READ_FOREIGN_DEVICE_TABLE_ACK:
                {
                    return new BvlcDecodeResult(0, function, header.Length);
                }

            // error encoding function or experimental one
            default:
                return new BvlcDecodeResult(-1, function, header.Length);
        }
    }

    public readonly record struct BvlcDecodeResult(int Result, BvlcFunction Function, int MsgLength)
    {
        public static BvlcDecodeResult Invalid(int result = -1, BvlcFunction func = default, int length = 0)
            => new(result, func, length);

        public bool IsSuccess => Result >= 0;
    }

    /// <summary>
    /// 添加一个“点对点的广播转发伙伴”
    /// Add a "point-to-point broadcast forwarding partner" (BBMD).
    /// </summary>
    public void Add(IPEndPoint point, IPAddress mask)
        => _routerDevices.TryAdd(point, mask);

    protected void OnMessageReceived(IPEndPoint sender, BvlcFunction function, ushort result, object? data)
    {
        if (!TryGetBvlcResult(result, out BvlcResult bvlcResult))
        {
            _logger.LogWarning("Received unknown BVLC result: {Result}", result);
            return;
        }
        BvlcMessage message = new(sender, function, bvlcResult, data);
        _messageSubject.OnNext(message);
    }

    private static bool TryNpduAssemble(byte[] buffer, int msgLength, IPEndPoint senderEndpoint, out ReadOnlyMemory<byte> forwardedMemory)
    {
        byte[] forwardedBuffer;
        forwardedMemory = default;
        int headerSize;
        if (senderEndpoint.AddressFamily == AddressFamily.InterNetwork) // IPv4
        {
            headerSize = 10;
            forwardedBuffer = new byte[msgLength + headerSize];

            // 先拷贝原始数据
            Array.Copy(buffer, 0, forwardedBuffer, headerSize, msgLength);

            ForwardHeaderIPv4 header = new();
            header.Header.Type = BvlcHeader.TypeIPv4; // 0x81
            header.Header.Function = (byte)BvlcFunction.BVLC_FORWARDED_NPDU; // 0x04
            header.Header.Length = (ushort)(msgLength + headerSize);

            header.Ip = BinaryPrimitives.ReadUInt32BigEndian(senderEndpoint.Address.GetAddressBytes()); // 转成大端
            header.Port = (ushort)IPAddress.HostToNetworkOrder((short)senderEndpoint.Port);

            MemoryMarshal.Write(forwardedBuffer.AsSpan(0, headerSize), in header);
        }
        else if (senderEndpoint.AddressFamily == AddressFamily.InterNetworkV6) // IPv6
        {
            headerSize = 22;
            forwardedBuffer = new byte[msgLength + headerSize];
            Array.Copy(buffer, 0, forwardedBuffer, headerSize, msgLength);

            ForwardHeaderIPv6 header = new();
            header.Header.Type = BvlcHeader.TypeIPv6;
            header.Header.Function = (byte)BvlcFunction.BVLC_FORWARDED_NPDU;
            header.Header.Length = (ushort)(msgLength + headerSize);

            header.Ip = new UInt128Like(senderEndpoint.Address.GetAddressBytes());
            header.Port = (ushort)IPAddress.HostToNetworkOrder((short)senderEndpoint.Port);

            MemoryMarshal.Write(forwardedBuffer.AsSpan(0, headerSize), in header);
        }
        else
        {
            return false;
        }
        forwardedMemory = forwardedBuffer.AsMemory(0, forwardedBuffer.Length);
        return true;
    }

    private async Task<bool> TryNpduDispatcAsync(ReadOnlyMemory<byte> forwardedMemory, bool isToAll, IPEndPoint originalSender)
    {
        try
        {
            await SendsToRouterAsync(forwardedMemory).ConfigureAwait(false);
            await SendToForeignAsync(forwardedMemory, originalSender).ConfigureAwait(false);
            if (isToAll)
            {
                IPEndPoint broadcastEndpoint = new(IPAddress.Parse(_broadcast), _defaultPort);
                await _channel.TryWriteAsync(forwardedMemory, broadcastEndpoint)
                     .ConfigureAwait(false);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Send a Frame to each registered foreign devices, except the original sender
    /// </summary>
    private async Task SendToForeignAsync(ReadOnlyMemory<byte> forwardedPacket, IPEndPoint? sender = null)
    {
        DateTime now = DateTime.UtcNow;
        // 清理过期设备（TTL + 30s）
        IEnumerable<IPEndPoint> expired = _foreignDevices.Where(kv => now > kv.Value).Select(kv => kv.Key);
        foreach (IPEndPoint key in expired)
        {
            _foreignDevices.Remove(key, out _);
        }

        // 并发发送
        List<Task> tasks = [];
        foreach (KeyValuePair<IPEndPoint, DateTime> kv in _foreignDevices)
        {
            if (sender != null && kv.Key.Equals(sender)) continue;
            tasks.Add(_channel.TryWriteAsync(forwardedPacket, kv.Key).AsTask());
        }

        await Task.WhenAll(tasks)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a frame to each registered BBMD except the original sender, concurrently.
    /// </summary>
    private async Task SendsToRouterAsync(ReadOnlyMemory<byte> forwardedPacket)
    {
        List<Task> tasks = [];

        foreach (KeyValuePair<IPEndPoint, IPAddress> e in _routerDevices)
        {
            IPEndPoint endpoint = NetworkExtension.ComputeResolveForwardAddress(e.Key, e.Value);
            tasks.Add(_channel.TryWriteAsync(forwardedPacket, endpoint).AsTask());
        }

        await Task.WhenAll(tasks)
            .ConfigureAwait(false);
    }

    private async Task ForwardNpduAsync(byte[] buffer, int msgLength, bool isToAll, IPEndPoint sender)
    {
        if (TryNpduAssemble(buffer, msgLength, sender, out ReadOnlyMemory<byte> forwardedPacket))
        {
            await TryNpduDispatcAsync(forwardedPacket, isToAll, sender).ConfigureAwait(false);
        }
    }

    private static bool TryGetFunction(byte value, out BvlcFunction function)
    {
        if (Enum.IsDefined(typeof(BvlcFunction), value))
        {
            function = (BvlcFunction)value;
            return true;
        }
        function = default;
        return false;
    }

    private static bool TryGetBvlcResult(ushort value, out BvlcResult result)
    {
        if (Enum.IsDefined(typeof(BvlcResult), value))
        {
            result = (BvlcResult)value;
            return true;
        }
        result = default;
        return false;
    }

    public void Dispose()
    {
        _messageSubject.OnCompleted();
        _messageSubject.Dispose();
    }
}

/**
 * +--------+----------+-------------------+
 * | Byte 0 | Byte 1   | Bytes 2 and 3     |
 * +--------+----------+-------------------+
 * | Type   | Function | Total Length      |
 * +--------+----------+-------------------+
 *
 * **/