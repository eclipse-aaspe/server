namespace Contracts.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class EventPayload
{
    public static object EventLock = new object();
    public EventStatus status { get; set; }
    public string statusData { get; set; } // application status data, continuously sent, can be used for specific reconnect
    public List<EventPayloadEntry> eventEntries { get; set; }

    public EventPayload()
    {
        status = new EventStatus();
        statusData = "";
        eventEntries = new List<EventPayloadEntry>();
    }
}