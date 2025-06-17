namespace BACnet;

/// <summary> The options to create a queue. </summary>
public sealed class ChannelOptions
{
    /// <summary>
    /// 初始化 <see cref="ChannelOptions"/> 类的新实例。
    /// </summary>
    /// <param name="channelName">通道名称。</param>
    public ChannelOptions(string channelName)
    {
        ChannelName = channelName;
        IPAddress = new IPAddress([0, 0, 0, 0]);
        Port = 47808;
        ChannelTypes = ChannelTypes.Udp; // 默认通道类型为客户端
    }

    /// <summary>
    /// 初始化 <see cref="ChannelOptions"/> 类的新实例。
    /// </summary>
    /// <param name="channelName">通道名称。</param>
    /// <param name="ip">绑定的 IP 地址。</param>
    /// <param name="port">绑定的端口号。</param>
    public ChannelOptions(string channelName, string ip, int port)
    {
        ChannelName = channelName;
        IPAddress = IPAddress.Parse(ip);
        Port = port;
    }

    public ChannelOptions(string channelName, IPAddress ip, int port)
    {
        ChannelName = channelName;
        IPAddress = ip;
        Port = port;
    }

    /// <summary>
    /// 获取通道唯一名称。
    /// </summary>
    public string ChannelName { get; }

    /// <summary>
    /// 通道类型。
    /// </summary>
    public ChannelTypes ChannelTypes { get; }

    /// <summary>
    ///绑定的端口号。
    /// </summary>
    public int Port { get; }

    /// <summary>
    /// 绑定的 IP 地址。
    /// </summary>
    public IPAddress IPAddress { get; }
}

public class CreateBuilder(string channelName)
{
    private readonly string _channelName = channelName;
    private IPAddress _ipAddress = new([0, 0, 0, 0]);
    private int _port = 47808;

    public CreateBuilder SetAddress(IPAddress ip)
    {
        _ipAddress = ip;
        return this;
    }

    public CreateBuilder SetPort(int port)
    {
        _port = port;
        return this;
    }

    public ChannelOptions Build() => new(_channelName, _ipAddress, _port);
}

public enum ChannelTypes
{
    Udp,        // 纯 UDP 通道，支持无状态收发
    Mstp,       // MSTP 通道，带状态机和 Token 机制
    WebSocket,  // WebSocket 通道（如未来扩展）
}