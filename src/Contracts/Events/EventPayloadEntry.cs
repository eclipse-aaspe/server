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


public class EventPayloadEntry : IComparable<EventPayloadEntry>
{
    public const string SCHEMA = "https://api.swaggerhub.com/domains/Plattform_i40/Part1-MetaModel-Schemas/V3.1.0#/components/schemas/";
    public string? idShortPath;

    public EventPayloadEntrySubject subject { get; set; }


    public string time { get; set; } //latest timeStamp
    public string type { get; set; } // CREATE, UPDATE, DELETE
    //public string source { get; set; } // link to source
    public string payloadType { get; set; } // Submodel, SME, AAS
    public string source { get; set; } // link to source

    public JsonObject data { get; set; } // JSON Serialization

    public List<string> notDeletedIdShortList { get; set; } // for DELETE only, remaining idShort


    public EventPayloadEntry()
    {
        type = "";
        time = "";

        data = new JsonObject();
        subject = new EventPayloadEntrySubject();
        //notDeletedIdShortList = new List<string>();
    }

    public int CompareTo(EventPayloadEntry other)
    {
        var result = string.Compare(this.subject.id, other.subject.id);

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
            result = string.Compare(this.subject.idShortPath, other.subject.idShortPath);
        }

        return result;
    }
}
