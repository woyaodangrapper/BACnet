namespace BACnet.Contracts;

public interface IDecode
{
    int Decode(byte[] buffer, int offset, uint count);
}