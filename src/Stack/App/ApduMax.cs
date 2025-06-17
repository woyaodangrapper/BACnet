namespace BACnet.Stack.App;

internal enum ApduMax : byte
{
    MAX_APDU50 = 0,
    MAX_APDU128 = 1,
    MAX_APDU206 = 2,
    MAX_APDU480 = 3,
    MAX_APDU1024 = 4,
    MAX_APDU1476 = 5
}