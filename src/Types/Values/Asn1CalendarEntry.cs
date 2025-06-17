using BACnet.Contracts;
using BACnet.Serializer;

namespace BACnet.Types.Values;

public struct Asn1CalendarEntry : IEncode, IDecode
{
    public List<object> Entries; // Date or DateRange or WeekNDay

    public void Encode(EncodeBuffer buffer)
    {
        if (Entries == null)
            return;

        foreach (IEncode entry in Entries)
        {
            if (entry is Asn1Date)
            {
                TlvEncoder.encode_tag(buffer, 0, true, 4);
                entry.Encode(buffer);
            }

            if (entry is Asn1DateRange)
            {
                TlvEncoder.encode_opening_tag(buffer, 1);
                entry.Encode(buffer);
                TlvEncoder.encode_closing_tag(buffer, 1);
            }

            if (entry is Asn1WeekNDay)
            {
                TlvEncoder.encode_tag(buffer, 2, true, 3);
                entry.Encode(buffer);
            }
        }
    }

    public int Decode(byte[] buffer, int offset, uint count)
    {
        var len = 0;

        Entries = new List<object>();

        while (true)
        {
            len += TlvEncoder.decode_tag_number(buffer, offset + len, out byte tagNumber);

            switch (tagNumber)
            {
                case 0:
                    var bdt = new Asn1Date();
                    len += bdt.Decode(buffer, offset + len, count);
                    Entries.Add(bdt);
                    break;

                case 1:
                    var bdr = new Asn1DateRange();
                    len += bdr.Decode(buffer, offset + len, count);
                    Entries.Add(bdr);
                    len++; // closing tag
                    break;

                case 2:
                    var bwd = new Asn1WeekNDay();
                    len += bwd.Decode(buffer, offset + len, count);
                    Entries.Add(bwd);
                    break;

                default:
                    return len - 1; // closing Tag
            }
        }
    }
}