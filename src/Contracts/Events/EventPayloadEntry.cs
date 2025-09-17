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
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;

public enum EventPayloadEntryType
{
    Created,
    Updated,
    Deleted,
    NotDeleted
}


public class EventPayloadEntry : IComparable<EventPayloadEntry>
{
    public const string SCHEMA_URL = "https://api.swaggerhub.com/domains/Plattform_i40/Part1-MetaModel-Schemas/V3.1.0#/components/schemas/";

    public string time { get; set; } //latest timeStamp

    [JsonIgnore]
    public EventPayloadEntryType eventPayloadEntryType { get; private set; } // eventPayloadEntryType

    public string type { get; private set; } // Created, Updated, Deleted, NotDeleted
    public string id { get; set; }
    public string source { get; set; } // link to source

    public JsonObject data { get; set; } // JSON Serialization
    public string dataschema { get; set; } // SCHEMA_URL + model type
    public List<string> notDeletedIdShortList { get; set; } // for DELETE only, remaining idShort
    public string semanticid { get; set; }

    public EventPayloadEntry()
    {
        type = "";
        time = "";
        dataschema = "";
        id = "";

        data = new JsonObject();
    }

    public void SetType(EventPayloadEntryType type)
    {
        this.type = type.ToString();
        this.eventPayloadEntryType = type;
    }

    public string GetSubdmodelId()
    {
        var splittedSourceString = source.Split("/");

        if (GetModelType()?.ToLower() == "basiceventelement")
        {
            return Base64UrlEncoder.Decode(splittedSourceString[^3]);
        }
        else if (GetModelType()?.ToLower() == "submodel")
        {
            return Base64UrlEncoder.Decode(splittedSourceString?.Last());
        }
        else if (!GetModelType().IsNullOrEmpty())
        {
            return Base64UrlEncoder.Decode(splittedSourceString[^2]);
        }
        else
        {
            return "undefined";
        }
    }

    public string GetIdShortPath()
    {
        var splittedSourceString = source.Split("/");

        if (GetModelType()?.ToLower() == "basiceventelement")
        {
            return splittedSourceString.Last();
        }
        else if (GetModelType()?.ToLower() == "submodel")
        {
            return String.Empty;
        }
        else if (!GetModelType().IsNullOrEmpty())
        {
            return splittedSourceString.Last();
        }
        else
        {
            return "undefined";
        }
    }


    public string GetModelType()
    {
        return dataschema.Split("/")?.Last();
    }

    public int CompareTo(EventPayloadEntry other)
    {
        var result = string.Compare(this.GetSubdmodelId(), other.GetSubdmodelId());

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
            result = string.Compare(this.GetIdShortPath(), other.GetIdShortPath());
        }

        return result;

        return 0;
    }
}
