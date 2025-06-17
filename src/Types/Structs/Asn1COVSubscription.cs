using BACnet.Types.Common;
using BACnet.Types.Object;

namespace BACnet.Types.Structs;

internal struct Asn1COVSubscription
{
    /* BACnetRecipientProcess */
    public Address Recipient;
    public uint subscriptionProcessIdentifier;
    /* BACnetObjectPropertyReference */
    internal ObjectId monitoredObjectIdentifier;
    internal PropertyReference monitoredProperty;
    /* BACnetCOVSubscription */
    public bool IssueConfirmedNotifications;
    public uint TimeRemaining;
    public float COVIncrement;
}