using BACnet.Contracts;

namespace BACnet.Stack.App;

internal class BacnetApplication
{
    private readonly INpdu _npdu;
    private readonly IApdu? _apdu;

    private readonly ILogger<BacnetApplication> _logger;

    public BacnetApplication(ILoggerFactory loggerFactory, INpdu npdu, IApdu? apdu = null)
    {
        _apdu = apdu;
        _npdu = npdu ?? throw new ArgumentNullException(nameof(npdu));
        _logger = loggerFactory?.CreateLogger<BacnetApplication>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }
}