using System.Net.NetworkInformation;

namespace BACnet.Extensions;

internal static class NetworkExtension
{
    /// <summary>
    /// 获取系统默认路由接口对应的 IPv4 广播地址。
    /// </summary>
    public static IPAddress? GetBroadcastAddress()
    {
        var defaultIf = NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic =>
                nic.OperationalStatus == OperationalStatus.Up &&
                nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .Select(nic => new
            {
                Nic = nic,
                Props = nic.GetIPProperties()
            })
            .FirstOrDefault(x => x.Props.GatewayAddresses
                .Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork));

        if (defaultIf is null)
            return null;

        UnicastIPAddressInformation? unicast = defaultIf.Props.UnicastAddresses
            .FirstOrDefault(u => u.Address.AddressFamily == AddressFamily.InterNetwork);

        if (unicast is null || unicast.IPv4Mask is null)
            return null;

        byte[] ipBytes = unicast.Address.GetAddressBytes();
        byte[] maskBytes = unicast.IPv4Mask.GetAddressBytes();

        byte[] broadcastBytes = new byte[4];
        for (int i = 0; i < 4; i++)
        {
            broadcastBytes[i] = (byte)(ipBytes[i] | (byte)~maskBytes[i]);
        }

        return new IPAddress(broadcastBytes);
    }

    /// <summary>
    /// 根据 BACnet BBMD 转发规则，计算 Forwarded-NPDU 的目标地址：
    /// 目标 = BBMD 地址 OR (~广播分发掩码)。
    /// 同时支持 IPv4 和 IPv6（保留 IPv6 的 ScopeId）。
    /// </summary>
    /// <param name="bbmd">源 BBMD 的 IP 端点（IPv4 或 IPv6）。</param>
    /// <param name="broadcastMask">广播分发掩码，应与 bbmd 地址族一致。</param>
    /// <returns>计算后的目标 IPEndPoint。</returns>
    public static IPEndPoint ComputeResolveForwardAddress(IPEndPoint bbmd, IPAddress broadcastMask)
    {
        // 只支持 IPv4 和 IPv6，且掩码地址族必须与 BBMD 地址一致
        AddressFamily family = bbmd.Address.AddressFamily;
        if ((family != AddressFamily.InterNetwork && family != AddressFamily.InterNetworkV6)
            || broadcastMask.AddressFamily != family)
        {
            throw new ArgumentException("BBMD address and broadcastMask must both be IPv4 or both IPv6.");
        }

        // 原始地址和掩码字节
        byte[] addrBytes = bbmd.Address.GetAddressBytes();
        byte[] maskBytes = broadcastMask.GetAddressBytes();
        int len = addrBytes.Length;

        // 结果缓冲区：Stackalloc 当长度 <= 16 时，否则退回到堆分配
        Span<byte> result = len <= 16
            ? stackalloc byte[16][..len]
            : new byte[len];

        // 计算：result[i] = addrBytes[i] OR (~maskBytes[i])
        for (int i = 0; i < len; i++)
        {
            result[i] = (byte)(addrBytes[i] | ~maskBytes[i]);
        }

        // 构造目标 IPAddress，IPv6 时保留 ScopeId
        IPAddress targetIp = family switch
        {
            AddressFamily.InterNetwork => new IPAddress(result),
            AddressFamily.InterNetworkV6 => new IPAddress(result, bbmd.Address.ScopeId),
            _ => throw new InvalidOperationException("Unsupported address family.")
        };

        return new IPEndPoint(targetIp, bbmd.Port);
    }

    /// <summary>
    /// 获取带指定端口的广播端点（IPEndPoint）。
    /// </summary>
    public static IPEndPoint? GetBroadcastEndpoint(int port)
    {
        IPAddress? addr = GetBroadcastAddress();
        return addr != null ? new IPEndPoint(addr, port) : null;
    }
}