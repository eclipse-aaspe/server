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
using System.Text.Json.Nodes;
using System.Threading.Tasks;

public class EventPayload
{
    //To be deleted, when db request handler is used for events?
    public static object EventLock = new object();

    private const string SPEC_VERSION = "1.0";

    public string specVersion { get; set; } //current spec version, to be changed in static variable
    public string time { get; set; } //latest timeStamp for all entries
    public string transmitted { get; set; } // timestamp of GET or PUT


    public int countSM { get; set; }
    public string cursor { get; set; }

    public JsonObject statusData { get; set; } // application status data, continuously sent, can be used for specific reconnect
    public List<EventPayloadEntry> elements { get; set; }

    public EventPayload()
    {
        specVersion = SPEC_VERSION;
        time = "";
        transmitted = "";

        countSM = 0;

        statusData = new JsonObject();
        elements = new List<EventPayloadEntry>();
    }
}
