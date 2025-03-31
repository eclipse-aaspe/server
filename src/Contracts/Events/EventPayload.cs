namespace Contracts.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class EventPayload
{
    public static object EventLock = new object();
    public EventStatus Status { get; set; }
    public string StatusData { get; set; } // application status data, continuously sent, can be used for specific reconnect
    public List<EventPayloadEntry> EventEntries { get; set; }

    public EventPayload()
    {
        Status = new EventStatus();
        StatusData = "";
        EventEntries = new List<EventPayloadEntry>();
    }
}