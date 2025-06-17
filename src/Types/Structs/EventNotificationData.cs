using BACnet.Stack;
using BACnet.Types.Common;
using BACnet.Types.Object;
using BACnet.Types.Values;

namespace BACnet.Types.Structs;

internal struct EventNotificationData
{
    public uint processIdentifier;
    public ObjectId initiatingObjectIdentifier;
    public ObjectId eventObjectIdentifier;
    public Asn1GenericTime timeStamp;
    public uint notificationClass;
    public byte priority;
    public EventTypes eventType;
    public string messageText;       /* OPTIONAL - Set to NULL if not being used */
    public NotifyTypes notifyType;
    public bool ackRequired;
    public EventStates fromState;
    public EventStates toState;

    /*
     ** Each of these structures in the union maps to a particular eventtype
     ** Based on BACnetNotificationParameters
     */

    /*
     ** EVENT_CHANGE_OF_BITSTRING
     */
    public Asn1BitString changeOfBitstring_referencedBitString;
    public Asn1BitString changeOfBitstring_statusFlags;
    /*
     ** EVENT_CHANGE_OF_STATE
     */
    public PropertyState changeOfState_newState;
    public Asn1BitString changeOfState_statusFlags;
    /*
     ** EVENT_CHANGE_OF_VALUE
     */
    public Asn1BitString changeOfValue_changedBits;
    public float changeOfValue_changeValue;
    public COVTypes? changeOfValue_tag;
    public Asn1BitString changeOfValue_statusFlags;
    /*
     ** EVENT_COMMAND_FAILURE
     */
    public uint commandFailure_commandValue;
    public Asn1BitString commandFailure_statusFlags;
    public uint commandFailure_feedbackValue;
    /*
     ** EVENT_FLOATING_LIMIT
     */
    public float floatingLimit_referenceValue;
    public Asn1BitString floatingLimit_statusFlags;
    public float floatingLimit_setPointValue;
    public float floatingLimit_errorLimit;
    /*
     ** EVENT_OUT_OF_RANGE
     */
    public float outOfRange_exceedingValue;
    public Asn1BitString outOfRange_statusFlags;
    public float outOfRange_deadband;
    public float outOfRange_exceededLimit;
    /*
     ** EVENT_CHANGE_OF_LIFE_SAFETY
     */
    public LifeSafetyStates? changeOfLifeSafety_newState;
    public LifeSafetyModes? changeOfLifeSafety_newMode;
    public Asn1BitString changeOfLifeSafety_statusFlags;
    public LifeSafetyOperations? changeOfLifeSafety_operationExpected;
    /*
     ** EVENT_EXTENDED
     **
     ** Not Supported!
     */
    /*
     ** EVENT_BUFFER_READY
     */
    public Asn1DeviceObjectPropertyReference bufferReady_bufferProperty;
    public uint bufferReady_previousNotification;
    public uint bufferReady_currentNotification;
    /*
     ** EVENT_UNSIGNED_RANGE
     */
    public uint unsignedRange_exceedingValue;
    public Asn1BitString unsignedRange_statusFlags;
    public uint unsignedRange_exceededLimit;
    /*
     ** EVENT_EXTENDED
     */
    public uint extended_vendorId;
    public uint extended_eventType;
    public object[] extended_parameters;
    /*
     ** EVENT_CHANGE_OF_RELIABILITY
     */
    public Reliability changeOfReliability_reliability;
    public Asn1BitString changeOfReliability_statusFlags;
    public PropertyValue[] changeOfReliability_propertyValues;

    public override string ToString()
    {
        return $"initiatingObject: {initiatingObjectIdentifier}, eventObject: {eventObjectIdentifier}, "
             + $"eventType: {eventType}, notifyType: {notifyType}, timeStamp: {timeStamp}, "
             + $"fromState: {fromState}, toState: {toState}"
             + (notifyType != NotifyTypes.NOTIFY_ACK_NOTIFICATION ? $", {GetEventDetails()}" : "");
    }

    private string GetEventDetails()
    {
        switch (eventType)
        {
            case EventTypes.EVENT_CHANGE_OF_BITSTRING:
                return $"referencedBitString: {changeOfBitstring_referencedBitString}, statusFlags: {changeOfBitstring_statusFlags}";

            case EventTypes.EVENT_CHANGE_OF_STATE:
                return $"newState: {changeOfState_newState}, statusFlags: {changeOfState_statusFlags}";

            case EventTypes.EVENT_CHANGE_OF_VALUE:
                return $"changedBits: {changeOfValue_changedBits}, changeValue: {changeOfValue_changeValue}, "
                       + $"tag: {changeOfValue_tag}, statusFlags: {changeOfValue_statusFlags}";

            case EventTypes.EVENT_FLOATING_LIMIT:
                return $"referenceValue: {floatingLimit_referenceValue}, statusFlags: {floatingLimit_statusFlags}, "
                       + $"setPointValue: {floatingLimit_setPointValue}, errorLimit: {floatingLimit_errorLimit}";

            case EventTypes.EVENT_OUT_OF_RANGE:
                return $"exceedingValue: {outOfRange_exceedingValue}, statusFlags: {outOfRange_statusFlags}, "
                       + $"deadband: {outOfRange_deadband}, exceededLimit: {outOfRange_exceededLimit}";

            case EventTypes.EVENT_CHANGE_OF_LIFE_SAFETY:
                return $"newState: {changeOfLifeSafety_newState}, newMode: {changeOfLifeSafety_newMode}, "
                       +
                       $"statusFlags: {changeOfLifeSafety_statusFlags}, operationExpected: {changeOfLifeSafety_operationExpected}";

            case EventTypes.EVENT_BUFFER_READY:
                return $"bufferProperty: {bufferReady_bufferProperty}, previousNotification: {bufferReady_previousNotification}, "
                       + $"currentNotification: {bufferReady_currentNotification}";

            case EventTypes.EVENT_UNSIGNED_RANGE:
                return $"exceedingValue: {unsignedRange_exceedingValue}, statusFlags: {unsignedRange_statusFlags}, "
                       + $"exceededLimit: {unsignedRange_exceededLimit}";

            case EventTypes.EVENT_EXTENDED:
                return $"vendorId: {extended_vendorId}, extendedEventType: {extended_eventType}, parameters: [{extended_parameters?.Length ?? 0}]";

            case EventTypes.EVENT_CHANGE_OF_RELIABILITY:
                var properties = string.Join(", ", changeOfReliability_propertyValues?.Select(p => $"{p.property}"));
                return $"reliability: {changeOfReliability_reliability}, statusFlags: {changeOfReliability_statusFlags}, properties: [{properties}]";

            default:
                return "no details";
        }
    }
};