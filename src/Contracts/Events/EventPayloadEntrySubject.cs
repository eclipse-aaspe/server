namespace Contracts.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class EventPayloadEntrySubject
{
    public string id { get; set; } // ID of related Submodel
    public string semanticId { get; set; }
    public string idShortPath { get; set; } // for SMEs only

    public EventPayloadEntrySubject()
    {
        semanticId = "";
        id = "";
    }
}
