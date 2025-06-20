// Ignore Spelling: npdu

using BACnet.Contracts;
using BACnet.Extensions;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace BACnet.Stack.Bvlc;

/// <summary>
/// 封装 BVLC Function、Forward、Broadcast
/// </summary>
internal class Bvlc : IBvlc
{
    private readonly ILogger<Bvlc> _logger;
    private readonly Dispatcher _dispatcher;

    private readonly DeviceType _deviceType;
    private readonly BvlcStateMachine _bvlcState;

    public DeviceType DeviceType => _deviceType;
    public BvlcStateMachine BvlcState => _bvlcState;

    public Bvlc(IChannel channel, ILoggerFactory loggerFactory, int defaultPort = 47808, DeviceType deviceType = DeviceType.Non)
    {
        _logger = loggerFactory.CreateLogger<Bvlc>();

        _deviceType = deviceType;
        _bvlcState = new(DeviceState.Initial);
        _dispatcher = new(channel, defaultPort);
    }

    internal async ValueTask<BvlcDecodeResult> DecodeAsync(byte[] buffer, int offset, IPEndPoint sender, CancellationToken token)
    {
        scoped Span<byte> slice = buffer.AsSpan(offset);
        // 尝试解析 BVLC 响应头
        if (!Assembler.TryParseResponseHeader(slice, out BvlcDecodeResult result))
        {
            return result;
        }

        BvlcFunction function = result.Function;
        int headerLength = result.MsgLength;

        try
        {
            // 处理需要分发的函数
            await HandleFunctionAsync(function, headerLength, buffer, offset, sender, token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling BVLC function {Function} from {Sender}", function, sender);
            throw;
        }
        // 如果是 NAK 响应，则直接返回
        Dictionary<BvlcFunction, BvlcResult> nackMap = new()
        {
            [BvlcFunction.BVLC_READ_FOREIGN_DEVICE_TABLE] = BvlcResult.BVLC_RESULT_READ_FOREIGN_DEVICE_TABLE_NAK,
            [BvlcFunction.BVLC_DELETE_FOREIGN_DEVICE_TABLE_ENTRY] = BvlcResult.BVLC_RESULT_DELETE_FOREIGN_DEVICE_TABLE_ENTRY_NAK,
            [BvlcFunction.BVLC_READ_BROADCAST_DIST_TABLE] = BvlcResult.BVLC_RESULT_READ_BROADCAST_DISTRIBUTION_TABLE_NAK,
            [BvlcFunction.BVLC_WRITE_BROADCAST_DISTRIBUTION_TABLE] = BvlcResult.BVLC_RESULT_WRITE_BROADCAST_DISTRIBUTION_TABLE_NAK,
        };

        // 如果是 NAK 响应，则发送对应的结果
        if (nackMap.TryGetValue(function, out BvlcResult bvlcResult))
        {
            await _dispatcher.SendToResultAsync(sender, bvlcResult).ConfigureAwait(false);
        }
        // 剥离分发处理和响应处理
        return function switch
        {
            BvlcFunction.BVLC_RESULT => new BvlcDecodeResult(0, function, headerLength),// not for the upper layers
            BvlcFunction.BVLC_ORIGINAL_UNICAST_NPDU => new BvlcDecodeResult(4, function, headerLength),// only for the upper layers
            BvlcFunction.BVLC_ORIGINAL_BROADCAST_NPDU => new BvlcDecodeResult(4, function, headerLength),// also for the upper layers
            BvlcFunction.BVLC_FORWARDED_NPDU => new BvlcDecodeResult(10, function, headerLength),// also for the upper layers
            BvlcFunction.BVLC_DISTRIBUTE_BROADCAST_TO_NETWORK => new BvlcDecodeResult(0, function, headerLength),// not for the upper layers
            BvlcFunction.BVLC_REGISTER_FOREIGN_DEVICE => new BvlcDecodeResult(0, function, headerLength),// not for the upper layers
            BvlcFunction.BVLC_READ_FOREIGN_DEVICE_TABLE => new BvlcDecodeResult(0, function, headerLength),
            BvlcFunction.BVLC_DELETE_FOREIGN_DEVICE_TABLE_ENTRY => new BvlcDecodeResult(0, function, headerLength),
            BvlcFunction.BVLC_READ_BROADCAST_DIST_TABLE => new BvlcDecodeResult(0, function, headerLength),
            BvlcFunction.BVLC_WRITE_BROADCAST_DISTRIBUTION_TABLE or BvlcFunction.BVLC_READ_BROADCAST_DIST_TABLE_ACK
                => new BvlcDecodeResult(0, function, headerLength),
            BvlcFunction.BVLC_READ_FOREIGN_DEVICE_TABLE_ACK => new BvlcDecodeResult(0, function, headerLength),
            _ => new BvlcDecodeResult(-1, function, headerLength),
        };
    }

    private async Task HandleFunctionAsync(BvlcFunction function, int headerLength, byte[] buffer, int offset, IPEndPoint sender
        , CancellationToken cancellationToken = default)
    {
        await function.IfOk(BvlcFunction.BVLC_ORIGINAL_BROADCAST_NPDU, ()
          => ForwardAsync(buffer, headerLength, broadcast: false, sender), _bvlcState.Ok, cancellationToken).ConfigureAwait(false);

        await function.IfOk(BvlcFunction.BVLC_FORWARDED_NPDU, async () =>
        {
            Memory<byte> forwardedMemory = buffer.AsMemory(offset, headerLength);
            await _dispatcher.SendToRoutersAsync(forwardedMemory).ConfigureAwait(false);
            await _dispatcher.SendBroadcastAsync(forwardedMemory).ConfigureAwait(false);
        }, _bvlcState.Ok && headerLength >= 10, cancellationToken).ConfigureAwait(false);

        await function.IfOk(BvlcFunction.BVLC_DISTRIBUTE_BROADCAST_TO_NETWORK, async () =>
        {
            if (_dispatcher.ForeignContainsKey(sender))
            {
                await ForwardAsync(buffer, headerLength, broadcast: true, sender).ConfigureAwait(false);
                return;
            }

            await _dispatcher.SendToResultAsync(sender, BvlcResult.BVLC_RESULT_DISTRIBUTE_BROADCAST_TO_NETWORK_NAK)
            .ConfigureAwait(false);
        }, _bvlcState.Ok, cancellationToken).ConfigureAwait(false); ;

        await function.IfOk(BvlcFunction.BVLC_REGISTER_FOREIGN_DEVICE, async () =>
        {
            _dispatcher.AddOrUpdateForeignFromBuffer(sender, buffer);
            await _dispatcher.SendToResultAsync(sender, BvlcResult.BVLC_RESULT_SUCCESSFUL_COMPLETION).ConfigureAwait(false);
        }, _bvlcState.Ok && headerLength == 6, cancellationToken).ConfigureAwait(false);
    }

    private async Task ForwardAsync(byte[] buffer, int msgLength, bool broadcast, IPEndPoint sender)
    {
        if (Assembler.TryAssemble(buffer, msgLength, sender, out ReadOnlyMemory<byte> forwardedPacket))
        {
            await _dispatcher.TryDispatchAsync(forwardedPacket, broadcast, sender).ConfigureAwait(false);
        }
    }

    //private static (IPEndPoint Endpoint, IPAddress Mask)[] ParseBvlcBroadcastTable(ReadOnlySpan<byte> buffer, int msgLength)
    //{
    //    int nbEntries = (msgLength - 4) / 10;
    //    (IPEndPoint Endpoint, IPAddress Mask)[] entries = new (IPEndPoint Endpoint, IPAddress Mask)[nbEntries];

    //    for (int i = 0; i < nbEntries; i++)
    //    {
    //        int offset = 4 + i * 10;
    //        ReadOnlySpan<byte> span = buffer.Slice(offset, 10);

    //        uint ipInt = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(0, 4));
    //        IPAddress ipAddress = new IPAddress(ipInt);

    //        ushort port = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(4, 2));

    //        byte[] maskBytes = span.Slice(6, 4).ToArray();
    //        IPAddress mask = new IPAddress(maskBytes);

    //        entries[i] = (new IPEndPoint(ipAddress, port), mask);
    //    }

    //    return entries;
    //}

    //private static (IPEndPoint Endpoint, ushort Ttl, ushort RemainTtl)[] ParseBvlcForeignDeviceTable(ReadOnlySpan<byte> buffer, int msgLength)
    //{
    //    int nbEntries = (msgLength - 4) / 10;
    //    (IPEndPoint Endpoint, ushort Ttl, ushort RemainTtl)[] entries = new (IPEndPoint Endpoint, ushort Ttl, ushort RemainTtl)[nbEntries];

    //    for (int i = 0; i < nbEntries; i++)
    //    {
    //        int offset = 4 + i * 10;
    //        ReadOnlySpan<byte> span = buffer.Slice(offset, 10);

    //        uint ipInt = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(0, 4));
    //        IPAddress ipAddress = new IPAddress(ipInt);

    //        ushort port = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(4, 2));

    //        ushort ttl = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(6, 2));

    //        ushort remainTtl = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(8, 2));

    //        entries[i] = (new IPEndPoint(ipAddress, port), ttl, remainTtl);
    //    }

    //    return entries;
    //}

    private sealed class Assembler
    {
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

        // TryNpduAssemble
        public static bool TryAssemble(byte[] buffer, int msgLength, IPEndPoint sender, out ReadOnlyMemory<byte> forwardedMemory)
        {
            forwardedMemory = default;
            byte[] forwardedBuffer;
            int headerSize;

            if (sender.AddressFamily == AddressFamily.InterNetwork)
            {
                headerSize = 10;
                forwardedBuffer = new byte[msgLength + headerSize];
                Array.Copy(buffer, 0, forwardedBuffer, headerSize, msgLength);

                ForwardHeaderIPv4 header = new()
                {
                    Header = new BvlcHeader(BvlcHeader.TypeIPv4, (byte)BvlcFunction.BVLC_FORWARDED_NPDU, (ushort)(msgLength + headerSize)),
                    Ip = BinaryPrimitives.ReadUInt32BigEndian(sender.Address.GetAddressBytes()),
                    Port = (ushort)IPAddress.HostToNetworkOrder((short)sender.Port)
                };

                MemoryMarshal.Write(forwardedBuffer.AsSpan(0, headerSize), in header);
            }
            else if (sender.AddressFamily == AddressFamily.InterNetworkV6)
            {
                headerSize = 22;
                forwardedBuffer = new byte[msgLength + headerSize];
                Array.Copy(buffer, 0, forwardedBuffer, headerSize, msgLength);

                ForwardHeaderIPv6 header = new()
                {
                    Header = new BvlcHeader(BvlcHeader.TypeIPv6, (byte)BvlcFunction.BVLC_FORWARDED_NPDU, (ushort)(msgLength + headerSize)),
                    Ip = new UInt128Like(sender.Address.GetAddressBytes()),
                    Port = (ushort)IPAddress.HostToNetworkOrder((short)sender.Port)
                };

                MemoryMarshal.Write(forwardedBuffer.AsSpan(0, headerSize), in header);
            }
            else
            {
                return false;
            }

            forwardedMemory = forwardedBuffer.AsMemory(0, forwardedBuffer.Length);
            return true;
        }

        public static BvlcHeader EncodeHeader(int msgLength, IPEndPoint sender, BvlcFunction function)
        {
            BvlcHeader bvlcHeader;
            if (sender.AddressFamily == AddressFamily.InterNetwork)
            {
                bvlcHeader = new BvlcHeader(BvlcHeader.TypeIPv4, (byte)function, (ushort)(msgLength));
            }
            else if (sender.AddressFamily == AddressFamily.InterNetworkV6)
            {
                bvlcHeader = new BvlcHeader(BvlcHeader.TypeIPv6, (byte)function, (ushort)(msgLength));
            }
            else
            {
                throw new NotSupportedException("Unsupported AddressFamily for BVLC header encoding.");
            }

            return bvlcHeader;
        }

        public static bool TryParseResponseHeader(Span<byte> slice, out BvlcDecodeResult result)
        {
            result = BvlcDecodeResult.Invalid();

            if (!BvlcHeaderExtensions.TryParse(slice, out BvlcHeader header))
            {
                return false;
            }

            if (slice.Length < header.Length)
            {
                return false;
            }

            if (header.Type != BvlcHeader.TypeIPv4 && header.Type != BvlcHeader.TypeIPv6)
            {
                return false;
            }

            if (!TryGetFunction(header.Function, out BvlcFunction function))
            {
                result = BvlcDecodeResult.Invalid(func: function);
                return false;
            }

            result = new BvlcDecodeResult(0, function, header.Length);
            return true;
        }
    }

    private sealed class Dispatcher(IChannel channel, int port)
    {
        private readonly IChannel _channel = channel;
        private readonly int _defaultPort = port;

        // 存储路由器设备的 IP 地址和掩码（BBMD）。
        private readonly ConcurrentDictionary<IPEndPoint, IPAddress> _routerDevices = [];

        // 存储 BACnet 网络中的远程注册的外部设备及时间。
        private readonly ConcurrentDictionary<IPEndPoint, DateTime> _foreignDevices = [];

        private readonly string _broadcast = NetworkExtension.GetBroadcastAddress()?.ToString() ?? "";

        public async Task<bool> TryDispatchAsync(ReadOnlyMemory<byte> packet, bool isToAll, IPEndPoint sender)
        {
            try
            {
                await SendToRoutersAsync(packet).ConfigureAwait(false);
                await SendToForeignAsync(packet, sender).ConfigureAwait(false);
                if (isToAll)
                {
                    await SendBroadcastAsync(packet).ConfigureAwait(false);
                }
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> SendBroadcastAsync(ReadOnlyMemory<byte> packet)
        {
            IPEndPoint broadcastEndpoint = new(IPAddress.Parse(_broadcast), _defaultPort);
            return await _channel.TryWriteAsync(packet, broadcastEndpoint).ConfigureAwait(false);
        }

        public async Task SendToForeignAsync(ReadOnlyMemory<byte> packet, IPEndPoint? sender = null)
        {
            DateTime now = DateTime.UtcNow;
            foreach (IPEndPoint key in _foreignDevices.Where(kv => now > kv.Value).Select(kv => kv.Key))
            {
                _foreignDevices.Remove(key, out _);
            }

            List<Task> tasks = [];
            foreach (KeyValuePair<IPEndPoint, DateTime> kv in _foreignDevices)
            {
                if (sender != null && kv.Key.Equals(sender))
                {
                    continue;
                }

                tasks.Add(_channel.TryWriteAsync(packet, kv.Key).AsTask());
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        public async Task SendToRoutersAsync(ReadOnlyMemory<byte> packet)
        {
            List<Task> tasks = [];
            foreach (KeyValuePair<IPEndPoint, IPAddress> kv in _routerDevices)
            {
                IPEndPoint endpoint = NetworkExtension.ComputeResolveAddress(kv.Key, kv.Value);
                tasks.Add(_channel.TryWriteAsync(packet, endpoint).AsTask());
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        public async Task SendToResultAsync(IPEndPoint sender, BvlcResult resultCode)
        {
            BvlcHeader header = Assembler.EncodeHeader(6, sender, BvlcFunction.BVLC_RESULT);
            header.ToBytes(out byte[] buffer);

            buffer[4] = (byte)(((ushort)resultCode & 0xFF00) >> 8);
            buffer[5] = (byte)((ushort)resultCode & 0xFF);

            await _channel.TryWriteAsync(buffer.AsMemory(), sender).ConfigureAwait(false);
        }

        public bool ForeignContainsKey(IPEndPoint sender)
            => _foreignDevices.ContainsKey(sender);

        public bool RoutersContainsKey(IPEndPoint sender)
          => _routerDevices.ContainsKey(sender);

        public void AddRouterDevice(IPEndPoint point, IPAddress mask)
            => _routerDevices.TryAdd(point, mask);

        public void AddForeignDevice(IPEndPoint point, DateTime date)
            => _foreignDevices.TryAdd(point, date);

        public void AddOrUpdateForeignFromBuffer(IPEndPoint sender, ReadOnlySpan<byte> buffer)
        {
            ushort ttlSeconds = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(4, 2));
            DateTime expiration = DateTime.Now.AddSeconds(ttlSeconds + 30);
            _foreignDevices.AddOrUpdate(sender, expiration, (key, old) => expiration);
        }
    }
}

public readonly record struct BvlcDecodeResult(int Result, BvlcFunction Function, int MsgLength)
{
    public static BvlcDecodeResult Invalid(int result = -1, BvlcFunction func = default, int length = 0)
        => new(result, func, length);

    public bool IsSuccess => Result >= 0;
}

/**
 *
 * +--------+----------+-------------------+
 * | Byte 0 | Byte 1   | Bytes 2 and 3     |
 * +--------+----------+-------------------+
 * | Type   | Function | Total Length      |
 * +--------+----------+-------------------+
 *
 * **/