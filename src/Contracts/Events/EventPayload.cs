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
using System.ComponentModel;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;


public enum EventPayloadType
{
    Created,
    Updated,
    Deleted,
    NotDeleted
}


public class EventPayload : IComparable<EventPayload>
{
    public const string SCHEMA_URL = "https://api.swaggerhub.com/domains/Plattform_i40/Part1-MetaModel-Schemas/V3.1.0#/components/schemas/";

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

    //public int? countSM { get; set; }
    public string cursor { get; set; }

    public JsonObject statusData { get; set; } // application status data, continuously sent, can be used for specific reconnect

    //Event payload entry
    [JsonIgnore]
    public EventPayloadType eventPayloadEntryType { get; private set; } // eventPayloadEntryType
    public string type { get; set; } // Created, Updated, Deleted
    public string source { get; set; } // link to source
    public JsonObject data { get; set; } // JSON Serialization
    public string dataschema { get; set; } // SCHEMA_URL + model type
    public List<string> notDeletedIdShortList { get; set; } // for DELETE only, remaining idShort
    public string semanticid { get; set; }


    public EventPayload(bool isREST)
    {
        specversion = SPEC_VERSION;
        datacontenttype = DATA_CONTENT_TYPE;
        id = null;

        time = "";
        domain = "";
    }

    public void SetType(EventPayloadType type)
    {
        this.eventPayloadEntryType = type;
        this.type = $"io.admin-shell.events.v1.{type.ToString()?.ToLower()}";

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

    public int CompareTo(EventPayload other)
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
    }
}
