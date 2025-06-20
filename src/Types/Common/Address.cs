using BACnet.Contracts;
using BACnet.Serializer;
using System.Net.NetworkInformation;

namespace BACnet.Types.Common;

internal class Address : IEncode, IEquatable<Address>
{
    private readonly ushort _net;
    private readonly byte[] _adr;
    private readonly byte[] _vmac;
    private readonly AddressTypes _type;

    private readonly Address? _routedSource;
    private readonly Address? _routedDestination;

    // —— 公共只读属性 ——————————————————

    /// <summary>网络号（DNET）。</summary>
    public ushort Net => _net;

    /// <summary>设备地址（DADR，长度 ≤ 7）。</summary>
    public byte[] Adr => _adr;

    /// <summary>MAC（IPv6 扩展）</summary>
    public byte[] VMac => _vmac;

    /// <summary>地址类型。</summary>
    public AddressTypes Type => _type;

    /// <summary>路由源地址。</summary>
    public Address? RoutedSource => _routedSource;

    /// <summary>路由目标地址。</summary>
    public Address? RoutedDestination => _routedDestination;

    public Address(AddressTypes addressType, ushort network, byte[] address, byte[]? mac = default)
    {
        _net = network;
        _adr = address;
        _type = addressType;
        _vmac = mac ?? new byte[3];
    }

    public Address(AddressTypes addressType, ushort network, string address, byte[]? mac = default)
      : this(addressType, network, CreateAddress(addressType, address), mac)
    {
    }

    private static byte[] CreateAddress(AddressTypes type, string address)
    {
        switch (type)
        {
            case AddressTypes.IP:
                {
                    string[] parts = address.Split(':');
                    byte[] ipBytes = IPAddress.Parse(parts[0]).GetAddressBytes();

                    byte[] adr = new byte[6]; // 4 bytes IP + 2 bytes port
                    Array.Copy(ipBytes, adr, ipBytes.Length);

                    ushort port = parts.Length > 1 ? ushort.Parse(parts[1]) : (ushort)0xBAC0;
                    byte[] portBytes = BitConverter.GetBytes(port);
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(portBytes);
                    }

                    Array.Copy(portBytes, 0, adr, ipBytes.Length, portBytes.Length);
                    return adr;
                }
            case AddressTypes.Ethernet:
                {
                    return PhysicalAddress.Parse(address).GetAddressBytes();
                }
            default:
                throw new NotSupportedException($"Address type {type} is not supported for string parsing.");
        }
    }

    public void Encode(EncodeBuffer buffer)
    {
        TlvEncoder.encode_opening_tag(buffer, 1);
        TlvEncoder.encode_application_unsigned(buffer, _net);
        TlvEncoder.encode_application_octet_string(buffer, _adr, 0, _adr.Length);
        TlvEncoder.encode_closing_tag(buffer, 1);
    }

    public override string ToString() => ToString(_type);

    private string ToString(AddressTypes addressType)
    {
        while (true)
        {
            switch (addressType)
            {
                case AddressTypes.IP:
                    return Adr != null && Adr.Length >= 6
                        ? $"{Adr[0]}.{Adr[1]}.{Adr[2]}.{Adr[3]}:{(Adr[4] << 8) | Adr[5]}"
                        : "0.0.0.0";

                case AddressTypes.MSTP:
                    return Adr != null && Adr.Length >= 1
                        ? $"{Adr[0]}"
                        : "-1";

                case AddressTypes.PTP:
                    return "x";

                case AddressTypes.Ethernet:
                    return $"{new PhysicalAddress(Adr)}";

                case AddressTypes.IPV6:
                    return Adr != null && Adr.Length == 18
                        ? $"{new IPAddress([.. Adr.Take(16)])}:{Adr[16] << 8 | Adr[17]}"
                        : "[::]";

                default: // Routed @ are always like this, NPDU do not contains the MAC type, only the lenght
                    if (Adr == null || Adr.Length == 0)
                    {
                        return "?";
                    }

                    switch (Adr.Length)
                    {
                        case 6: // certainly IP, but not sure (Newron System send it for internal usage with 4*0 bytes)
                            addressType = AddressTypes.IP;
                            continue;

                        case 18: // Not sure it could appears, since NPDU may contains Vmac ?
                            addressType = AddressTypes.IPV6;
                            continue;

                        case 3:
                            return $"IPv6 VMac : {(Adr[0] << 16) | (Adr[1] << 8) | Adr[2]}";

                        default:
                            return string.Join(" ", Adr);
                    }
            }
        }
    }

    //public override string ToString() => ToString(Type);

    public string ToString(bool sourceOnly)
    {
        return _routedSource == null
            ? ToString()
            : sourceOnly
            ? _routedSource.ToString()
            : $"{_routedSource} via {ToString()}";
    }

    public bool HasAddress(IPAddress ipAddress)
        => Type == AddressTypes.IP
        && Adr != null
        && ipAddress != null
        && Adr.Take(4).SequenceEqual(ipAddress.GetAddressBytes());

    // checked if device is routed by curent equipement
    public bool IsMyRouter(Address device)
    {
        if (device.RoutedSource == null || RoutedSource != null)
        {
            return false;
        }

        return Adr.Length == device.Adr.Length
            && !Adr.Where((t, i) => t != device.Adr[i]).Any();
    }

    public string FullHashString()
    {
        string hash = $"{(uint)Type}.{Net}.{string.Concat(Adr.Select(a => a.ToString("X2")))}";

        if (_routedSource != null)
        {
            hash += $":{_routedSource.FullHashString()}";
        }

        if (_routedDestination != null)
        {
            hash += $":{_routedDestination.FullHashString()}";
        }

        return hash;
    }

    public override bool Equals(object? obj) => obj is Address other && Equals(other);

    public bool Equals(Address? other)
    {
        if (other == null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (_net != other.Net)
        {
            return false;
        }

        if (_type != other.Type)
        {
            return false;
        }

        if (_adr == null && other.Adr == null)
        {
            // 都为空，继续比较路由
        }
        else
        {
            if (_adr == null || other.Adr == null)
            {
                return false;
            }

            if (_adr.Length != other.Adr.Length)
            {
                return false;
            }

            if (!_adr.SequenceEqual(other.Adr))
            {
                return false;
            }
        }

        if (_vmac == null && other.VMac == null)
        {
            // 都为空，继续
        }
        else
        {
            if (_vmac == null || other.VMac == null)
            {
                return false;
            }

            if (!_vmac.SequenceEqual(other.VMac))
            {
                return false;
            }
        }

        bool routedSourceEqual = false;

        if (_routedSource == null && other._routedSource == null)
        {
            routedSourceEqual = true;
        }
        else if (_routedSource != null)
        {
            routedSourceEqual = _routedSource.Equals(other.RoutedSource);
        }
        // else routedSourceEqual remains false if other._routedSource != null

        bool routedDestinationEqual = false;

        if (_routedDestination == null && other.RoutedDestination == null)
        {
            routedDestinationEqual = true;
        }
        else if (_routedDestination != null)
        {
            routedDestinationEqual = _routedDestination.Equals(other.RoutedDestination);
        }

        return routedSourceEqual && routedDestinationEqual;
    }

    public override int GetHashCode() => HashCode.Combine(_net, _type, _adr, _vmac, _routedSource, _routedDestination);
}