namespace Contracts.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class EventStatus
{
    // public string Mode { get; set; } // PULL or PUSH must be the same in publisher and consumer
    public string Transmitted { get; set; } // timestamp of GET or PUT
    public string LastUpdate { get; set; } // latest timeStamp for all entries
    public int CountSM { get; set; }
    public int CountSME { get; set; }
    public string Cursor { get; set; }
    public EventStatus()
    {
        // Mode = "";
        Transmitted = "";
        LastUpdate = "";
        CountSM = 0;
        CountSME = 0;
        Cursor = "";
    }
}