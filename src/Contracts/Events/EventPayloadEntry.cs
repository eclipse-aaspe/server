namespace Contracts.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class EventPayloadEntry : IComparable<EventPayloadEntry>
{
    public string entryType { get; set; } // CREATE, UPDATE, DELETE
    public string lastUpdate { get; set; } // timeStamp for this entry
    public string payloadType { get; set; } // Submodel, SME, AAS
    public string payload { get; set; } // JSON Serialization
    public string submodelId { get; set; } // ID of related Submodel
    public string idShortPath { get; set; } // for SMEs only
    public List<string> notDeletedIdShortList { get; set; } // for DELETE only, remaining idShort

    public EventPayloadEntry()
    {
        entryType = "";
        lastUpdate = "";
        payloadType = "";
        payload = "";
        submodelId = "";
        idShortPath = "";
        notDeletedIdShortList = new List<string>();
    }

    public int CompareTo(EventPayloadEntry other)
    {
        var result = string.Compare(this.submodelId, other.submodelId);

        if (result == 0)
        {
            if (this.payloadType == other.payloadType)
            {
                result = 0;
            }
            else
            {
                if (this.payloadType == "sm")
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
            result = string.Compare(this.idShortPath, other.idShortPath);
        }

        return result;
    }
}