namespace Contracts.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class EventPayloadEntry : IComparable<EventPayloadEntry>
{
    public string EntryType { get; set; } // CREATE, UPDATE, DELETE
    public string LastUpdate { get; set; } // timeStamp for this entry
    public string PayloadType { get; set; } // Submodel, SME, AAS
    public string Payload { get; set; } // JSON Serialization
    public string SubmodelId { get; set; } // ID of related Submodel
    public string IdShortPath { get; set; } // for SMEs only
    public List<string> NotDeletedIdShortList { get; set; } // for DELETE only, remaining idShort

    public EventPayloadEntry()
    {
        EntryType = "";
        LastUpdate = "";
        PayloadType = "";
        Payload = "";
        SubmodelId = "";
        IdShortPath = "";
        NotDeletedIdShortList = new List<string>();
    }

    public int CompareTo(EventPayloadEntry other)
    {
        var result = string.Compare(this.SubmodelId, other.SubmodelId);

        if (result == 0)
        {
            if (this.PayloadType == other.PayloadType)
            {
                result = 0;
            }
            else
            {
                if (this.PayloadType == "sm")
                {
                    result = -1;
                }
                else
                {
                    result = 1;
                }
            }
        }

        if (result == 0)
        {
            result = string.Compare(this.IdShortPath, other.IdShortPath);
        }

        return result;
    }
}