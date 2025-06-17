using BACnet.Types.Common;
using BACnet.Types.Values;

namespace BACnet.Types.Object;

internal struct GetEventInformationData
{
    public ObjectId objectIdentifier;
    public EventStates eventState;
    public Asn1BitString acknowledgedTransitions;
    public Asn1GenericTime[] eventTimeStamps;    //3
    public NotifyTypes notifyType;
    public Asn1BitString eventEnable;
    public uint[] eventPriorities;     //3
}