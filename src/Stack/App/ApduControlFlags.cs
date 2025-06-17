namespace BACnet.Stack.App;

[Flags]
internal enum ApduControlFlags : int
{
    PDU_TYPE_CONFIRMED_SERVICE_REQUEST = 0,
    SERVER = 1,
    NEGATIVE_ACK = 2,
#pragma warning disable CA1069 // 不同场景下意义不同。
    SEGMENTED_RESPONSE_ACCEPTED = 2,
#pragma warning restore CA1069 // 不同场景下意义不同。
    MORE_FOLLOWS = 4,
    SEGMENTED_MESSAGE = 8,
    PDU_TYPE_UNCONFIRMED_SERVICE_REQUEST = 0x10,
    PDU_TYPE_SIMPLE_ACK = 0x20,
    PDU_TYPE_COMPLEX_ACK = 0x30,
    PDU_TYPE_SEGMENT_ACK = 0x40,
    PDU_TYPE_ERROR = 0x50,
    PDU_TYPE_REJECT = 0x60,
    PDU_TYPE_ABORT = 0x70,
    PDU_TYPE_MASK = 0xF0,
}