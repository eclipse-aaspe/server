namespace Contracts.DbRequests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contracts.Events;

public class DbEventRequest
{
    public EventDto EventData { get; set; } = new EventDto();

    public bool IsWithPayload { get; set; }

    public int LimitSm { get; set; }

    public int LimitSme { get; set; }

    public int OffsetSm { get; set; }

    public int OffsetSme { get; set; }

    public string Diff { get; set; } = string.Empty;
}
