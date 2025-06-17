using BACnet.Contracts;

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
}