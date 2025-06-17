using BACnet.Serializer;

namespace BACnet.Contracts;

public interface IEncode
{
    void Encode(EncodeBuffer buffer);
}