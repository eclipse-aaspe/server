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
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public class EventPayload
{
    //ToDo: Delete, when db request handler is used for the events?
    public static object EventLock = new object();

    private const string SPEC_VERSION = "1.0";
    private const string DATA_CONTENT_TYPE = "application/json";

    public string specversion { get; set; } //current spec version, to be changed in static variable
    public string time { get; set; } //latest timeStamp for all entries
    public string transmitted { get; set; } // timestamp of GET or PUT
    public string domain { get; set; } // domain (like phoenixcontact.com)
    public string id { get; set; } // message id
    public string datacontenttype { get; set; } // content type of transmitted data

    public int? countSM { get; set; }
    public string cursor { get; set; }

    public JsonObject statusData { get; set; } // application status data, continuously sent, can be used for specific reconnect
    public List<EventPayloadEntry> elements { get; set; }

    public EventPayload(bool isREST)
    {
        specversion = SPEC_VERSION;
        datacontenttype = DATA_CONTENT_TYPE;
        id = $"{Guid.NewGuid()}";

        time = "";
        transmitted = "";
        domain = "";

        if (isREST)
        {
            cursor = "";
            countSM = 0;
            statusData = new JsonObject();
        }
        else
        {
            elements = new List<EventPayloadEntry>();
        }
    }
}
