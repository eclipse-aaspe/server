/********************************************************************************
* Copyright (c) {2019 - 2025} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

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
