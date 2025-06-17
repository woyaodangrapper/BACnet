namespace BACnet.Channels;

public abstract class BaseChannel : IDisposable
{
    private readonly ILogger<BaseChannel> _logger;

    protected Socket Listener { get; }

    protected IPEndPoint LocalEndpoint;
    protected CancellationTokenSource CancellationToken { get; } = new();

    protected BaseChannel(ChannelOptions options, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        _logger = loggerFactory.CreateLogger<BaseChannel>();

        Listener = CreateSocket(options);
        LocalEndpoint = new IPEndPoint(options.IPAddress, options.Port);
    }

    public virtual async ValueTask<bool> TryWriteAsync(ReadOnlyMemory<byte> bytes, EndPoint? remoteEndpoint = null, CancellationToken cancellationToken = default)
    {
        try
        {
            int sent = await Listener.SendToAsync(bytes, SocketFlags.None, remoteEndpoint ?? LocalEndpoint, cancellationToken)
                .ConfigureAwait(false);
            return sent == bytes.Length;
        }
        catch (SocketException)
        {
            return false;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    protected virtual bool IsConnected()
        => !(Listener.Poll(1, SelectMode.SelectRead) && Listener.Available == 0);

    private bool _disposed;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                Listener.Dispose();
                CancellationToken.Dispose();
            }

            // Dispose unmanaged resources if any
            _disposed = true;
        }
    }

    protected abstract Socket CreateSocket(ChannelOptions options);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~BaseChannel()
    {
        Dispose(false);
    }
}