using BACnet.Contracts;
using System.Text;

namespace BACnet.Channels;

/// <summary>
/// BACnet/IP (UDP)
/// </summary>
public class UdpChannel : BaseChannel, IChannel
{
    private readonly UdpClient _udpClient;
    private readonly ChannelOptions _options;
    private readonly ILogger<UdpChannel> _logger;

    public UdpClient Client => _udpClient;

    public UdpChannel([NotNull] ChannelOptions options, ILoggerFactory loggerFactory)
        : base(options, loggerFactory)
    {
        _options = options;

        _logger = loggerFactory.CreateLogger<UdpChannel>();
        _udpClient = new UdpClient { Client = Listener };
        if (LocalEndpoint.AddressFamily == AddressFamily.InterNetworkV6)
        {
            JoinMulticastGroup(_udpClient);
            _udpClient.MulticastLoopback = false;  // 关闭多播回环
        }
    }

    private static void JoinMulticastGroup(UdpClient multicastListener)
    {
        multicastListener.JoinMulticastGroup(IPAddress.Parse("[FF02::BAC0]"));
        multicastListener.JoinMulticastGroup(IPAddress.Parse("[FF04::BAC0]"));
        multicastListener.JoinMulticastGroup(IPAddress.Parse("[FF05::BAC0]"));
        multicastListener.JoinMulticastGroup(IPAddress.Parse("[FF08::BAC0]"));
        multicastListener.JoinMulticastGroup(IPAddress.Parse("[FF0E::BAC0]"));
    }

    public async Task<bool> TryWriteAsync(byte[] bytes, IPEndPoint? endpoint = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var sendTask = _udpClient.SendAsync(bytes, bytes.Length, endpoint);
            var completedTask = await Task.WhenAny(sendTask, Task.Delay(Timeout.Infinite, cts.Token));
            if (completedTask == sendTask)
            {
                return sendTask.Result == bytes.Length;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException)
        {
            return false;
        }
    }

    //public bool TryWriteAsync(byte[] bytes, int msgLength, IPEndPoint? ep = null)
    //{
    //    ArgumentNullException.ThrowIfNull(bytes);

    //    try
    //    {
    //        int sent = _udpClient.Send(bytes, msgLength, ep ?? new IPEndPoint(_options.IPAddress, _options.Port));
    //        return sent == bytes.Length;
    //    }
    //    catch (SocketException)
    //    {
    //        return false;
    //    }
    //    catch (ObjectDisposedException)
    //    {
    //        return false;
    //    }
    //}

    public async Task TryExecuteAsync()
    {
        try
        {
            while (!CancellationToken.Token.IsCancellationRequested)
            {
                await ReadLoopAsync(_udpClient, CancellationToken.Token)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.BeginScope("UdpClient already disposed.");
        }
        catch (SocketException ex)
        {
            _logger.LogError(ex, "SocketException occurred in the receive loop.");
            throw; // Re-throw the exception to comply with CA1031
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "InvalidOperationException occurred in the receive loop.");
            throw; // Re-throw the exception to comply with CA1031
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception occurred in the receive loop.");
            throw; // Re-throw the exception to comply with CA1031
        }
    }

    private async Task ReadLoopAsync(UdpClient client, CancellationToken stoppingToken)
    {
        try
        {
            UdpReceiveResult res = await _udpClient
                    .ReceiveAsync(CancellationToken.Token)
                    .ConfigureAwait(false);

            string message = Encoding.UTF8.GetString(res.Buffer);
            Console.WriteLine($"Received from {res.RemoteEndPoint}: {message}");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Error  UdpClient.");
        }
        finally
        {
            client.Close();
        }
    }

    /// <summary>
    /// UdpClient vs Socket：协议行为一致
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    protected override Socket CreateSocket(ChannelOptions options)
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
        socket.Bind(new IPEndPoint(options.IPAddress, options.Port));
        return socket;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_udpClient != null)
            {
                try
                {
                    _udpClient.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    _logger.BeginScope("UdpClient already disposed.");
                }
                catch (Exception ex) when (ex is SocketException || ex is InvalidOperationException)
                {
                    _logger.LogError(ex, "Error disposing UdpClient.");
                    throw; // Re-throw the exception to comply with CA1031
                }
            }

            // 基类释放 Listener Socket
            base.Dispose(disposing);
        }
    }
}