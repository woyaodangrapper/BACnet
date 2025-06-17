namespace BACnet.Contracts;

public interface IChannel

{
    /// <summary>
    /// 尝试启动一个 BACnet，监听指定的端口。
    /// </summary>
    Task TryExecuteAsync();

    Task<bool> TryWriteAsync(byte[] bytes, IPEndPoint? endpoint = null, CancellationToken cancellationToken = default);

    ValueTask<bool> TryWriteAsync(ReadOnlyMemory<byte> bytes, EndPoint? remoteEndpoint = null, CancellationToken cancellationToken = default);
}