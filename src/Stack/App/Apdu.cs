using BACnet.Contracts;

namespace BACnet.Stack.App;

/// <summary>
/// 编解码服务请求头（PDU）
/// </summary>
internal class Apdu : IApdu
{
    private readonly ILogger<Apdu> _logger;
    private readonly INpdu _npdu;

    public Apdu(INpdu npdu, ILoggerFactory loggerFactory)
    {
        _npdu = npdu ?? throw new ArgumentNullException(nameof(npdu));
        _logger = loggerFactory?.CreateLogger<Apdu>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }
}