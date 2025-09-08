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
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

public enum EventPayloadEntryType
{
    Created,
    Updated,
    Deleted
}


public class EventPayloadEntry : IComparable<EventPayloadEntry>
{
    public const string SCHEMA_URL = "https://api.swaggerhub.com/domains/Plattform_i40/Part1-MetaModel-Schemas/V3.1.0#/components/schemas/";

    public EventPayloadEntrySubject subject { get; set; }
    public string time { get; set; } //latest timeStamp

    [JsonIgnore]
    public EventPayloadEntryType eventPayloadEntryType { get; private set; } // eventPayloadEntryType

    public string type { get; private set; } // Created, Updated, Deleted
    public string id { get; set; }
    public string source { get; set; } // link to source
    public JsonObject data { get; set; } // JSON Serialization
    public string dataschema { get; set; } // SCHEMA_URL + model type

    public List<string> notDeletedIdShortList { get; set; } // for DELETE only, remaining idShort

    public EventPayloadEntry()
    {
        type = "";
        time = "";
        dataschema = "";
        id = "";

        data = new JsonObject();
        subject = new EventPayloadEntrySubject();
    }

    public void SetType(EventPayloadEntryType type)
    {
        this.type = type.ToString();
        this.eventPayloadEntryType = type;
    }

    public int CompareTo(EventPayloadEntry other)
    {
        var result = string.Compare(this.subject.id, other.subject.id);

        if (result == 0)
        {
            var typeInSchema = this.dataschema.Split('/')?.Last().ToLower();
            var otherTypeInSchema = other.dataschema.Split('/')?.Last().ToLower();

            bool isBothSubmodel = typeInSchema == "submodel" && otherTypeInSchema == "submodel";
            bool isBothSubmodelElement = typeInSchema != "submodel" && otherTypeInSchema != "submodel";

            if (isBothSubmodel || isBothSubmodelElement)
            {
                result = 0;
            }
            else
            {
                if (typeInSchema == "submodel")
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
