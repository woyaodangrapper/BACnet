using BACnet.Contracts;

namespace BACnet.Channels;

/// <summary>
///  BACnet MSTP (Serial)
/// </summary>
public class MstpChannel : BaseChannel, IChannel
{
    public MstpChannel(ChannelOptions options, ILoggerFactory loggerFactory)
        : base(options, loggerFactory)
    {
    }

    public Task TryExecuteAsync()
    {
        throw new NotImplementedException();
    }

    public Task<bool> TryWriteAsync(byte[] bytes, IPEndPoint? endpoint = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    protected override Socket CreateSocket(ChannelOptions options)
    {
        throw new NotImplementedException();
    }
}

/*
 * BACnet MS/TP 电气物理层标准（基于 ANSI/TIA-485-A & ASHRAE 135-2024 Annex G）
 *
 * ▸ 总线类型：         半双工差分总线（RS-485）
 * ▸ 最大传输距离：     1200 米（4000 英尺）
 * ▸ 最大节点数：       32 个设备（每段总线，未使用中继器时）
 * ▸ 差分电压要求：     ≥ 1.5 V （驱动器输出 |V_A - V_B|）
 * ▸ 共模电压范围：     -7 V 到 +12 V
 * ▸ 最大负载单位（LU）：32 LU（每个设备通常为 1 LU）
 * ▸ 接口终端电阻：     两端需有 120 欧姆终端电阻
 * ▸ 连接方式：         建议采用 daisy-chain（菊花链）拓扑结构，避免星形
 * ▸ 通信速率：         支持的波特率包括 9600、19200、38400、57600、76800、115200（取决于实现）
 *
 *  标准文献参考：
 * - ASHRAE 135-2024: BACnet - Annex G (Master-Slave/Token-Passing)
 * - ANSI/TIA-485-A: Electrical Characteristics of Generators and Receivers for Use in Balanced Digital Multipoint Systems
 * - BTL Test Plan for MS/TP Devices: https://www.bacnetinternational.net/btl/
 */