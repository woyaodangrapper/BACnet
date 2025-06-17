using BACnet.Channels;
using BACnet.Contracts;
using BACnet.Stack.App;
using BACnet.Stack.Bvlc;
using BACnet.Stack.Net;

namespace BACnet;

public class Bacnet
{
    private readonly ChannelOptions _options;
    private readonly ILogger<Bacnet> _logger;
    private readonly IChannel _channel;

    private readonly Bvlc bvlc;
    private readonly Npdu npdu;
    private readonly Apdu apdu;

    private readonly BacnetApplication application;

    public Bacnet(ChannelOptions options, ILoggerFactory loggerFactory)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = loggerFactory?.CreateLogger<Bacnet>() ?? throw new ArgumentNullException(nameof(loggerFactory));

        _channel = CreateChannel(_options, loggerFactory);

        bvlc = new Bvlc(_channel, loggerFactory);
        npdu = new Npdu(bvlc, loggerFactory);
        apdu = new Apdu(npdu, loggerFactory);
        application = new(loggerFactory, npdu, apdu);
    }

    public static IChannel CreateChannel([NotNull] ChannelOptions options, [NotNull] ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        ILogger logger = loggerFactory.CreateLogger("ChannelFactory");

        return options.ChannelTypes switch
        {
            ChannelTypes.Udp => CreateUdpChannel(options, loggerFactory, logger),
            ChannelTypes.Mstp => throw new NotSupportedException("MSTP channel not implemented."),
            ChannelTypes.WebSocket => throw new NotSupportedException("WebSocket channel not implemented."),

            _ => throw new ArgumentOutOfRangeException(nameof(options.ChannelTypes), options.ChannelTypes, "Unsupported channel type.")
        };
    }

    private static UdpChannel CreateUdpChannel(ChannelOptions options, ILoggerFactory loggerFactory, ILogger logger)
    {
        logger.LogInformation("Creating UDP channel with IP: {IP}, Port: {Port}", options.IPAddress, options.Port);

        return new UdpChannel(options, loggerFactory);
    }
}

/**
 *  Bacnet 协议结构简述
 *
 * |-----------------------------|
 * |        Application         | ← Services, APDU
 * |-----------------------------|
 * |     Network Layer (NPDU)   |
 * |-----------------------------|
 * |  BVLL (Bacnet Virtual Link)| ← B/IP 封装层
 * |-----------------------------|
 * |         UDP/IP             | ← 物理传输（用 UdpChannel 实现即可）
 * |-----------------------------|
 *
 * +--------------------+
 * |    Application     |
 * +--------------------+
 * |       NPDU         |
 * +--------------------+
 * |       BVLC         |
 * +--------------------+
 * |    IChannel (UDP)  |
 * +--------------------+
 * **/