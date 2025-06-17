namespace BACnet.Stack.Net;

[Flags]
public enum NpduControlFlags : byte
{
    PriorityNormalMessage = 0,
    PriorityUrgentMessage = 1,
    PriorityCriticalMessage = 2,
    PriorityLifeSafetyMessage = 3,
    ExpectingReply = 4,
    SourceSpecified = 8,
    DestinationSpecified = 32,
    NetworkLayerMessage = 128
}