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

namespace AasxServerStandardBib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text;
using System.Text.Json.Nodes;
using AasxServerDB.Entities;
using AasxServerDB;
using AdminShellNS;
using Contracts;
using Contracts.Events;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using Extensions;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using AasxServer;
using System.Text.Json.Serialization;

public class EventService : IEventService
{
    public EventService(MqttClientService mqttClientService)
    {
        _mqttClientService = mqttClientService;
        var envValue = Environment.GetEnvironmentVariable("AASX_MQTT");
        _enableMqtt = envValue == "1";

        EventDtos = new List<EventDto>();
    }

    public List<EventDto> EventDtos { get; set; }

    public static object EventLock = new object();

    private bool _enableMqtt;
    private readonly MqttClientService _mqttClientService;

    public event EventHandler? CalculateCfpRequestReceived;

    public EventDto TryAddDto(EventDto eventDto)
    {
        var eventDtoIdentfier = $"{eventDto.SubmodelId}.{eventDto.IdShortPath}.{eventDto.IdShort}";
        var dto = EventDtos.FirstOrDefault(e => $"{e.SubmodelId}.{e.IdShortPath}.{e.IdShort}" == eventDtoIdentfier);

        if (dto == null)
        {
            EventDtos.Add(eventDto);
            return eventDto;
        }
        else
        {
            return dto;
        }
    }

    public async void RegisterMqttMessage(EventDto eventData, string submodelId, string idShortPath)
    {
        if (!_enableMqtt
            || eventData.MessageBroker == null
            || eventData.MessageBroker.Value.IsNullOrEmpty())
        {
            //ToDo: logging?
            return;
        }

        var clientId = Program.externalBlazor + "/submodels/" + Base64UrlEncoder.Encode(submodelId);

        if (!idShortPath.IsNullOrEmpty())
        {
            clientId += $"/submodel-elements/{idShortPath}.{eventData.IdShort}";
        }

        _mqttClientService.MessageReceived += _mqttClientService_MessageReceived;

        var result = await _mqttClientService.SubscribeAsync(clientId, eventData.MessageBroker.Value, eventData.MessageTopicType.Value,
                                eventData.UserName.Value, eventData.PassWord.Value);
    }

    private void _mqttClientService_MessageReceived(object? sender, MqttClientServiceMessageReceivedEventArgs e)
    {
        var eventDtosWithMessageCondition = EventDtos.Where(ev => ev.MessageCondition != null
                    && ev.MessageCondition.Value != null);

        foreach (var item in e.Message)
        {
            EventDto eventDto = null;

            var source = item["source"];

            if (source != null)
            {
                var search = $"(source == \"{source.ToString()}\")";
                eventDto = eventDtosWithMessageCondition.FirstOrDefault(ev
                    => ev.MessageCondition.Value == search);
            }

            if (eventDto == null)
            {
                var subject = item["subject"];

                if (subject != null)
                {
                    var idShortPath = subject["idShortPath"];

                    if (idShortPath != null)
                    {
                        eventDto = eventDtosWithMessageCondition.FirstOrDefault(ev
                            => ev.MessageCondition.Value == $"(subject.idShortPath == \"{idShortPath.ToString()}\")");
                    }

                    if (eventDto == null)
                    {
                        var id = subject["id"];

                        if (id != null)
                        {
                            eventDto = eventDtosWithMessageCondition.FirstOrDefault(ev
                                => ev.MessageCondition.Value == $"(subject.id == \"{id.ToString()}\")");
                        }
                    }

                    //var schemaType = subject["schema"];
                    //if (schemaType != null
                    //    && schemaType.ToString().Split("/").Last() != "BasicEventElement")
                    //{

                    //}
                }
            }

            if (eventDto != null)
            {
                if (eventDto.Action != null && eventDto.Action.Value != null)
                {
                    var nextUpdate = DateTime.UtcNow;
                    var now = nextUpdate;

                    if (eventDto.LastUpdate != null)
                    {
                        var executeAction = false;

                        executeAction = string.IsNullOrEmpty(eventDto.LastUpdate.Value);
                        if (!executeAction)
                        {
                            if (eventDto.MinInterval != null
                                && eventDto.MinInterval.Value != null)
                            {
                                nextUpdate = DateTime.Parse(eventDto.LastUpdate.Value)
                                    .Add(TimeSpan.FromSeconds(Int32.Parse(eventDto.MinInterval.Value)));
                            }

                            if (now >= nextUpdate)
                            {
                                executeAction = true;
                            }
                        }

                        if (executeAction)
                        {
                            if (eventDto.Action.Value == "calculatecfp")
                            {
                                OnCalculateCfpRequestReceived();
                            }
                            else if (eventDto.Action.Value == "updateDatabase")
                            {

                            }
                            eventDto.LastUpdate.Value = now.ToString();
                        }
                    }
                }
            }
        }
    }

    protected virtual void OnCalculateCfpRequestReceived()
    {
        CalculateCfpRequestReceived?.Invoke(this, EventArgs.Empty);
    }

    public async void CheckMqttMessages(EventDto eventData, string submodelId, string idShortPath)
    {
        if (!_enableMqtt
            || eventData.MessageBroker == null
            || eventData.MessageBroker.Value.IsNullOrEmpty()
            || eventData.PassWord == null
            || eventData.PassWord.Value == null
            || eventData.UserName == null
            || eventData.UserName.Value == null)
        {
            //ToDo: logging?
            //ToDo: allow empty username or password?
            return;
        }

        if (eventData.Transmitted != null)
        {
            var nextUpdate = DateTime.UtcNow;
            var now = nextUpdate;
            var executeAction = false;

            executeAction = string.IsNullOrEmpty(eventData.Transmitted.Value);
            if (!executeAction)
            {
                if (eventData.MinInterval != null
                    && eventData.MinInterval.Value != null)
                {
                    nextUpdate = DateTime.Parse(eventData.Transmitted.Value)
                        .Add(TimeSpan.FromSeconds(Int32.Parse(eventData.MinInterval.Value)));
                }

                if (now >= nextUpdate)
                {
                    executeAction = true;
                }
            }

            if (executeAction)
            {
                if (eventData.Action != null && eventData.Action.Value != null)
                {
                    if (eventData.Action.Value == "calculatecfp")
                    {
                        OnCalculateCfpRequestReceived();
                    }
                    else if (eventData.Action.Value == "updateDatabase")
                    {
                        //toDo
                        //eventData.LastUpdate.Value = TimeStamp.TimeStamp.DateTimeToString(now);

                    }

                    eventData.Transmitted.Value = TimeStamp.TimeStamp.DateTimeToString(now);
                }
            }
            else
            {
                //ToDo: Do we need max interval for MQTT in event elements?
                if (eventData.MaxInterval != null
                        && eventData.MaxInterval.Value != null
                            && Int32.TryParse(eventData.MaxInterval.Value, out int result))
                {
                    var nextMaxActionUpdate = DateTime.Parse(eventData.Transmitted.Value)
                        .Add(TimeSpan.FromSeconds(result));

                    if (now > nextMaxActionUpdate)
                    {
                        //ToDo: Do we need max interval for MQTT in event elements?
                    }
                }
            }

        }
    }

    public async void PublishMqttMessage(EventDto eventData, string submodelId, string idShortPath)
    {
        if (!_enableMqtt
            || eventData.MessageBroker == null
            || eventData.MessageBroker.Value.IsNullOrEmpty()
            || eventData.PassWord == null
            || eventData.PassWord.Value == null
            || eventData.UserName == null
            || eventData.UserName.Value == null)
        {
            //ToDo: logging?
            //ToDo: allow empty username or password?
            return;
        }

        IReferable source = null;
        if (eventData.DataCollection != null)
        {
            source = eventData.DataCollection;
        }
        else
        {
            source = eventData.DataSubmodel;
        }

        TimeSpan minInterval = TimeSpan.Zero;
        TimeSpan maxInterval = TimeSpan.Zero;

        var d = "";
        if (eventData.LastUpdate != null && eventData.LastUpdate.Value != null && eventData.LastUpdate.Value == "init")
        {
            d = "init";
        }
        else
        {
            if (eventData.LastUpdate == null || eventData.LastUpdate.Value.IsNullOrEmpty())
            {
                d = "init";
            }
            else
            {
                d = eventData.LastUpdate.Value;
            }
        }

        List<String> diffEntry = new List<String>();

        bool wp = false;

        if (eventData.Include != null
            && eventData.Include.Value != null)
        {
            wp = eventData.Include.Value.ToLower() == "true";
        }
        bool smOnly = false;

        if (eventData.SubmodelsOnly != null
                && eventData.SubmodelsOnly.Value != null)
        {
            smOnly = eventData.SubmodelsOnly.Value.ToLower() == "true";
        }

        bool pbee = false;

        if (eventData.PublishBasicEventElement != null
                && eventData.PublishBasicEventElement.Value != null)
        {
            pbee = eventData.PublishBasicEventElement.Value.ToLower() == "true";

            if (pbee)
            {
                wp = false;
            }
        }

        string domain = "";
        if (eventData.Domain != null)
        {
            domain = eventData.Domain.Value;
        }

        DateTime transmitted = DateTime.MinValue;
        if (eventData.Transmitted != null)
        {
            if (!eventData.Transmitted.Value.IsNullOrEmpty())
            {
                transmitted = DateTime.Parse(eventData.Transmitted.Value);
            }

            if (eventData.MinInterval != null
                && eventData.MinInterval.Value != null
                && Int32.TryParse(eventData.MinInterval.Value, out int minResult))
            {

                minInterval = TimeSpan.FromSeconds(minResult);
            }

            if (eventData.MaxInterval != null
                && eventData.MaxInterval.Value != null
                && Int32.TryParse(eventData.MaxInterval.Value, out int maxResult))
            {

                maxInterval = TimeSpan.FromSeconds(maxResult);
            }
        }

        var clientId = Program.externalBlazor + "/submodels/" + Base64UrlEncoder.Encode(submodelId);

        if (!idShortPath.IsNullOrEmpty())
        {
            clientId += $"/submodel-elements/{idShortPath}.{eventData.IdShort}";
        }

        var sourceString = "";
        var semanticId = "";
        if (pbee)
        {
            if (eventData.IdShort != null)
            {
                sourceString = $"{Program.externalBlazor}/submodels/{Base64UrlEncoder.Encode(submodelId)}/events/{idShortPath}.{eventData.IdShort}";
            }

            semanticId = (eventData.SemanticId != null && eventData.SemanticId?.Keys != null) ? eventData.SemanticId?.Keys[0].Value : "";
        }

        var e = CollectPayload(null, false, sourceString, semanticId, domain, null, eventData.ConditionSM, eventData.ConditionSME,
            d, diffEntry, transmitted, minInterval, maxInterval, wp, smOnly, 1000, 1000, 0, 0);

        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        if (e.elements.Count > 0)
        {
            var elements = e.elements.Copy();

            foreach (var element in elements)
            {
                e.type = element.type;
                e.source = element.source;
                e.data = element.data;
                e.dataschema = element.dataschema;
                e.id = element.id;
                e.semanticid = element.semanticid;

                if (!element.time.IsNullOrEmpty())
                {
                    e.time = element.time;
                }

                e.elements = null;

                var payloadObjString = JsonSerializer.Serialize(e, options);

                try
                {
                    var result = await _mqttClientService.PublishAsync(clientId, eventData.MessageBroker.Value, eventData.MessageTopicType.Value,
                        eventData.UserName.Value, eventData.PassWord.Value, payloadObjString);

                    var now = DateTime.UtcNow;

                    bool isSucceeded = false;

                    if (result != null && result.IsSuccess)
                    {
                        isSucceeded = true;
                        Console.WriteLine("MQTT message sent.");
                    }

                    if (isSucceeded)
                    {
                        if (eventData.Transmitted != null)
                        {
                            eventData.Transmitted.Value = e.transmitted;
                            eventData.Transmitted.SetTimeStamp(now);
                        }
                        var dt = DateTime.Parse(e.time);
                        if (eventData.LastUpdate != null)
                        {
                            eventData.LastUpdate.Value = e.time;
                            eventData.LastUpdate.SetTimeStamp(dt);
                        }
                        if (eventData.Status != null)
                        {
                            if (eventData.Message != null)
                            {
                                //ToDo: Is message really correct? 
                                eventData.Message.Value = "on";
                            }
                            eventData.Status.SetTimeStamp(now);
                        }
                        if (eventData.Diff != null && diffEntry.Count > 0)
                        {
                            eventData.Diff.Value = new List<ISubmodelElement>();
                            int i = 0;
                            foreach (var dif in diffEntry)
                            {
                                var p = new Property(DataTypeDefXsd.String);
                                p.IdShort = "diff" + i;
                                p.Value = dif;
                                p.SetTimeStamp(dt);
                                eventData.Diff.Value.Add(p);
                                p.SetAllParentsAndTimestamps(eventData.Diff, dt, dt, DateTime.MinValue);
                                i++;
                            }
                            eventData.Diff.SetTimeStamp(dt);
                        }
                        Program.signalNewData(2);
                    }
                    else
                    {
                        var statusCode = "";

                        if (result != null)
                        {
                            statusCode = result.ReasonCode.ToString();
                        }

                        if (eventData.Status != null
                            && eventData.Message != null)
                        {
                            eventData.Message.Value = "ERROR: " +
                                statusCode + " ; " +
                                " ; PUT " + eventData.MessageBroker.Value;
                            eventData.Status.SetTimeStamp(now);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (eventData.Message != null)
                    {
                        eventData.Message.Value = "ERROR: " +
                            ex.Message +
                            " ; PUT " + eventData.MessageBroker.Value;
                    }
                    var now = DateTime.UtcNow;
                    eventData.Status.SetTimeStamp(now);
                    // d = eventData.LastUpdate.Value = "reconnect";
                }
            }

            eventData.env.setWrite(true);
        }
    }

    //private int CollectSubmodelElements(List<ISubmodelElement> submodelElements, DateTime diffTime, EventPayloadEntryType entryType,
    //    string submodelId, string idShortPath, List<EventPayloadEntry> entries, List<String> diffEntry, bool withPayload)
    //{
    //    int count = 0;
    //    foreach (var sme in submodelElements)
    //    {
    //        bool tree = false;
    //        bool copy = false;
    //        bool delete = false;
    //        DateTime timeStamp = new DateTime();

    //        List<ISubmodelElement> children = new List<ISubmodelElement>();
    //        switch (sme)
    //        {
    //            case ISubmodelElementCollection smc:
    //                children = smc.Value;
    //                break;
    //            case ISubmodelElementList sml:
    //                children = sml.Value;
    //                break;
    //        }

    //        switch (entryType)
    //        {
    //            case EventPayloadEntryType.Created:
    //                timeStamp = sme.TimeStampCreate;
    //                if ((sme.TimeStampCreate - sme.TimeStampTree).TotalMilliseconds >= 0
    //                    && (sme.TimeStampCreate - diffTime).TotalMilliseconds > 1)
    //                {
    //                    copy = true;
    //                }
    //                else
    //                {
    //                    if (sme.TimeStampCreate < sme.TimeStampTree && (sme.TimeStampTree - diffTime).TotalMilliseconds > 1)
    //                    {
    //                        tree = true;
    //                    }
    //                }
    //                break;
    //            case EventPayloadEntryType.Updated:
    //                timeStamp = sme.TimeStampTree;
    //                var a = (diffTime - sme.TimeStampCreate).TotalMilliseconds;
    //                var b = (sme.TimeStampTree - sme.TimeStampCreate).TotalMilliseconds;
    //                var c = (sme.TimeStampTree - diffTime).TotalMilliseconds;
    //                if ((diffTime - sme.TimeStampCreate).TotalMilliseconds >= -1
    //                    && (sme.TimeStampTree - sme.TimeStampCreate).TotalMilliseconds > 1
    //                    && (sme.TimeStampTree - diffTime).TotalMilliseconds > 1)
    //                {
    //                    if (children != null && children.Count != 0)
    //                    {
    //                        foreach (ISubmodelElement child in children)
    //                        {
    //                            if (child.TimeStampTree != sme.TimeStampTree || child.TimeStampCreate == sme.TimeStampTree)
    //                            {
    //                                tree = true;
    //                                break;
    //                            }
    //                        }
    //                    }
    //                    if (!tree)
    //                    {
    //                        copy = true;
    //                    }
    //                }
    //                break;
    //            case EventPayloadEntryType.Deleted:
    //                timeStamp = sme.TimeStampDelete;
    //                if ((diffTime - sme.TimeStampCreate).TotalMilliseconds >= 0
    //                    && (sme.TimeStampDelete - diffTime).TotalMilliseconds > 1)
    //                {
    //                    delete = true;
    //                }
    //                if (children != null && children.Count != 0)
    //                {
    //                    foreach (ISubmodelElement child in children)
    //                    {
    //                        if (child.TimeStampTree != sme.TimeStampTree)
    //                        {
    //                            tree = true;
    //                            break;
    //                        }
    //                    }
    //                }
    //                break;
    //        }
    //        if (copy)
    //        {
    //            diffEntry.Add(entryType + " " + idShortPath + sme.IdShort);
    //            var j = Jsonization.Serialize.ToJsonObject(sme);
    //            var e = new EventPayloadEntry();
    //            e.SetType(entryType);
    //            e.time = TimeStamp.TimeStamp.DateTimeToString(timeStamp);
    //            if (withPayload)
    //            {
    //                e.data = j;
    //            }
    //            e.source = 

    //            e.subject.id = submodelId;
    //            e.subject.idShortPath = idShortPath + sme.IdShort;

    //            //ToDo: Find correct Semantic Id
    //            //e.semanticId = sme.SemanticId.GetAsIdentifier();

    //            entries.Add(e);
    //            count++;
    //        }
    //        if (delete)
    //        {
    //            Console.WriteLine("DELETE SME " + idShortPath + sme.IdShort);
    //            diffEntry.Add(entryType + " " + idShortPath + sme.IdShort + ".*");
    //            var e = new EventPayloadEntry();
    //            e.SetType(entryType);
    //            e.time = TimeStamp.TimeStamp.DateTimeToString(timeStamp);
    //            e.subject.id = submodelId;
    //            e.subject.idShortPath = idShortPath + sme.IdShort;
    //            if (children != null || children.Count != 0)
    //            {
    //                foreach (var child in children)
    //                {
    //                    if (e.notDeletedIdShortList == null)
    //                    {
    //                        e.notDeletedIdShortList = new List<string>();
    //                    }

    //                    e.notDeletedIdShortList.Add(child.IdShort);
    //                }
    //            }
    //            entries.Add(e);
    //            count++;
    //        }
    //        if (tree)
    //        {
    //            if (tree && children.Count != 0)
    //            {
    //                count += CollectSubmodelElements(children, diffTime, entryType, submodelId, idShortPath + sme.IdShort + ".", entries, diffEntry, withPayload);
    //            }
    //        }
    //    }
    //    return count;
    //}

    public EventPayload CollectPayload(Dictionary<string, string> securityCondition, bool isREST, string basicEventElementSourceString,
        string basicEventElementSemanticId, string domain, SubmodelElementCollection statusData, AasCore.Aas3_0.Property conditionSM, AasCore.Aas3_0.Property conditionSME,
        string diff, List<String> diffEntry, DateTime transmitted, TimeSpan minInterval, TimeSpan maxInterval,
        bool withPayload, bool smOnly, int limitSm, int limitSme, int offsetSm, int offsetSme)
    {
        var eventPayload = new EventPayload(isREST);

        var diffTime = new DateTime();

        if (!diff.IsNullOrEmpty()
            && diff != "status"
            && diff != "init")
        {
            var nextUpdate = transmitted
                .Add(minInterval);

            var now = DateTime.UtcNow;

            if (now < nextUpdate)
            {
                return eventPayload;
            }

            diffTime = DateTime.Parse(diff);
            diffTime = diffTime.AddMilliseconds(1);
        }

        eventPayload.transmitted = TimeStamp.TimeStamp.DateTimeToString(DateTime.UtcNow);

        eventPayload.time = "";
        eventPayload.domain = domain;

        if (statusData != null && statusData.Value != null)
        {
            var j = Jsonization.Serialize.ToJsonObject(statusData);
            eventPayload.statusData = j;
            /*
            if (depth == 0)
            {
                var j = Jsonization.Serialize.ToJsonObject(statusData);
                e.statusData = j.ToJsonString();
            }
            if (depth == 1 && statusData.Value.Count == 1 && statusData.Value[0] is SubmodelElementCollection smc)
            {
                var j = Jsonization.Serialize.ToJsonObject(smc);
                e.statusData = j.ToJsonString();
            }
            */
        }

        lock (EventLock)
        {
            int countSM = 0;
            int countSME = 0;
            string searchSM = string.Empty;
            string searchSME = string.Empty;
            if (conditionSM != null && conditionSM.Value != null)
            {
                searchSM = conditionSM.Value;
            }
            if (securityCondition != null && securityCondition.TryGetValue("sm.", out _))
            {
                if (searchSM == string.Empty)
                {
                    searchSM = securityCondition["sm."];
                }
                else
                {
                    searchSM = $"({securityCondition["sm."]})&&({searchSM})";
                }
            }
            if (conditionSME != null && conditionSME.Value != null)
            {
                searchSME = conditionSME.Value;
            }
            if (securityCondition != null && securityCondition.TryGetValue("sme.", out _))
            {
                if (searchSME == string.Empty)
                {
                    searchSME = securityCondition["sme."];
                }
                else
                {
                    searchSME = $"({securityCondition["sme."]})&&({searchSME})";
                }
            }

            using AasContext db = new();
            var timeStampMax = new DateTime();

            if (searchSM is "(*)" or "*" or "")
            {
                var s = db.SMSets.Select(sm => sm.TimeStampTree);
                if (s.Any())
                {
                    timeStampMax = s.Max();
                }
            }
            else
            {
                var s = db.SMSets.Where(searchSM).Select(sm => sm.TimeStampTree);
                if (s.Any())
                {
                    timeStampMax = s.Max();
                }
            }

            eventPayload.time = TimeStamp.TimeStamp.DateTimeToString(timeStampMax);
            if (diff == "status")
            {
                var statusEntry = new EventPayloadEntry();

                if (!basicEventElementSourceString.IsNullOrEmpty())
                {
                    statusEntry.dataschema = "https://api.swaggerhub.com/domains/Plattform_i40/Part1-MetaModel-Schemas/V3.1.0#/components/schemas/BasicEventElement";
                    statusEntry.id = $"{basicEventElementSourceString}-{eventPayload.time}";
                    eventPayload.id = statusEntry.id;

                    statusEntry.SetType(EventPayloadEntryType.Updated);
                    statusEntry.source = basicEventElementSourceString;
                    eventPayload.semanticid = basicEventElementSemanticId;

                };
                eventPayload.elements =
                [
                    statusEntry,
                ];
                return eventPayload;
            }

            if (timeStampMax <= diffTime)
            {
                if (maxInterval != TimeSpan.Zero)
                {
                    var nextTransmit = transmitted
                         .Add(maxInterval);

                    var now = DateTime.UtcNow;

                    if (now > nextTransmit)
                    {
                        var statusEntry = new EventPayloadEntry();

                        if (!basicEventElementSourceString.IsNullOrEmpty())
                        {
                            statusEntry.dataschema = "https://api.swaggerhub.com/domains/Plattform_i40/Part1-MetaModel-Schemas/V3.1.0#/components/schemas/BasicEventElement";
                            statusEntry.id = $"{basicEventElementSourceString}-{eventPayload.time}";
                            eventPayload.id = statusEntry.id;

                            statusEntry.SetType(EventPayloadEntryType.Updated);
                            statusEntry.source = basicEventElementSourceString;
                            eventPayload.semanticid = basicEventElementSemanticId;

                            eventPayload.elements =
                        [
                            statusEntry,
                        ];

                            return eventPayload;
                        }
                        else
                        {
                            //ToDo: Add element for non-basiceventelement keep alive message
                        }
                    }
                    else
                    {
                        return eventPayload;
                    }
                }
                else
                {
                    return eventPayload;
                }
            }
            else if (!basicEventElementSourceString.IsNullOrEmpty())
            {
                var statusEntry = new EventPayloadEntry();

                if (!basicEventElementSourceString.IsNullOrEmpty())
                {
                    statusEntry.dataschema = "https://api.swaggerhub.com/domains/Plattform_i40/Part1-MetaModel-Schemas/V3.1.0#/components/schemas/BasicEventElement";
                    statusEntry.id = $"{basicEventElementSourceString}-{eventPayload.time}";
                    eventPayload.id = statusEntry.id;

                    statusEntry.SetType(EventPayloadEntryType.Updated);
                    statusEntry.source = basicEventElementSourceString;
                    eventPayload.semanticid = basicEventElementSemanticId;

                };
                eventPayload.elements =
                [
                    statusEntry,
                        ];

                return eventPayload;
            }

            eventPayload.elements = new List<EventPayloadEntry>();

            IQueryable<SMSet> smSearchSet = db.SMSets;
            if (!searchSM.IsNullOrEmpty() && searchSM != "*")
            {
                smSearchSet = smSearchSet.Where(searchSM);
            }
            // smSearchSet = smSearchSet.Where(timeStampExpression[i]);
            smSearchSet = smSearchSet.Where(sm => (sm.TimeStampCreate > diffTime) ||
                ((sm.TimeStampTree != sm.TimeStampCreate) && (sm.TimeStampTree > diffTime) && (sm.TimeStampCreate <= diffTime)));
            smSearchSet = smSearchSet.OrderBy(sm => sm.TimeStampTree).Skip(offsetSm).Take(limitSm);
            var smSearchList = smSearchSet.ToList();

            foreach (var sm in smSearchList)
            {
                var entryType = EventPayloadEntryType.Updated;
                if (sm.TimeStampCreate > diffTime)
                {
                    entryType = EventPayloadEntryType.Created;
                }

                bool completeSM = true;

                if (!smOnly)
                {
                    if (sm.TimeStampCreate <= diffTime)
                    {
                        var smeSearchSM = db.SMESets.Where(sme => sme.SMId == sm.Id);
                        var smeSearchTimeStamp = smeSearchSM.Where(sme =>
                                sme.TimeStampCreate > diffTime
                                || sme.TimeStampDelete > diffTime
                                || (sme.TimeStampCreate <= diffTime && sme.TimeStampDelete <= diffTime
                                    && sme.TimeStampTree != sme.TimeStampCreate && sme.TimeStampTree > diffTime)
                            )
                            .OrderBy(sme => sme.TimeStampTree).Skip(offsetSme).Take(limitSme).ToList();
                        if (smeSearchTimeStamp.Count != 0)
                        {
                            // smeSearchTimeStamp = smeSearchSM.Where(sme => sme.ParentSMEId == null).ToList();
                            var tree = CrudOperator.GetTree(db, sm, smeSearchTimeStamp);
                            var treeMerged = CrudOperator.GetSmeMerged(db, null, tree, sm);
                            // var lookupChildren = treeMerged?.ToLookup(m => m.smeSet.ParentSMEId);

                            completeSM = false;
                            List<int> skip = [];
                            foreach (var sme in smeSearchTimeStamp)
                            {
                                if (skip.Contains(sme.Id))
                                {
                                    var children = smeSearchSM.Where(c => sme.Id == c.ParentSMEId).Select(s => s.Id).ToList();
                                    skip.AddRange(children);
                                    continue;
                                }
                                var notDeletedIdShortList = new List<string>();
                                if (sme.TimeStampDelete > diffTime)
                                {
                                    entryType = EventPayloadEntryType.Deleted;
                                    var children = smeSearchSM.Where(c => sme.Id == c.ParentSMEId).ToList();
                                    foreach (var c in children)
                                    {
                                        notDeletedIdShortList.Add(c.IdShort);
                                    }
                                }
                                else
                                {
                                    var totalChildren = db.SMESets.Where(s => s.ParentSMEId == sme.Id).ToList();
                                    var allChildren = smeSearchTimeStamp.Where(s => s.ParentSMEId == sme.Id).ToList();
                                    var createChildren = allChildren.Where(s => s.ParentSMEId == sme.Id && s.TimeStampCreate > diffTime).ToList();
                                    var updateChildren = allChildren.Where(s => s.ParentSMEId == sme.Id && s.TimeStampTree > diffTime).ToList();
                                    var deleteChildren = allChildren.Where(s => s.ParentSMEId == sme.Id && s.TimeStampDelete > diffTime).ToList();
                                    if (sme.TimeStampCreate > diffTime)
                                    {
                                        if (allChildren.Count == 0 || totalChildren.Count == createChildren.Count)
                                        {
                                            entryType = EventPayloadEntryType.Created;
                                            skip.AddRange(createChildren.Select(s => s.Id).ToList());
                                        }
                                        else // SKIP and use children instead
                                        {
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        if (allChildren.Count == 0 ||
                                            (createChildren.Count == 0 && deleteChildren.Count == 0
                                                && totalChildren.Count != 1 && totalChildren.Count == updateChildren.Count))
                                        {
                                            entryType = EventPayloadEntryType.Updated;
                                            skip.AddRange(updateChildren.Select(s => s.Id).ToList());
                                        }
                                        else // SKIP and use children instead
                                        {
                                            continue;
                                        }
                                    }
                                }
                                var parentId = sme.ParentSMEId;
                                var idShortPath = sme.IdShortPath;

                                if (sme.TimeStampTree > timeStampMax)
                                {
                                    timeStampMax = sme.TimeStampTree;
                                }
                                /*
                                var idShortPath = sme.IdShort;
                                while (parentId != null && parentId != 0)
                                {
                                    var parentSME = smeSearchSM.Where(sme => sme.Id == parentId).FirstOrDefault();
                                    idShortPath = parentSME.IdShort + "." + idShortPath;
                                    parentId = parentSME.ParentSMEId;
                                }
                                */

                                var sourceString = Program.externalBlazor + "/submodels/" + Base64UrlEncoder.Encode(sm.Identifier);
                                sourceString += "/submodel-elements/" + idShortPath;

                                var entry = new EventPayloadEntry();
                                entry.SetType(entryType);
                                entry.time = TimeStamp.TimeStamp.DateTimeToString(sme.TimeStampTree);

                                entry.source = sourceString;

                                entry.dataschema = EventPayloadEntry.SCHEMA_URL + CrudOperator.GetModelType(sme.SMEType);

                                if (notDeletedIdShortList != null && notDeletedIdShortList.Count > 0)
                                {
                                    entry.notDeletedIdShortList = notDeletedIdShortList;
                                }

                                if (sm.SemanticId != null)
                                {
                                    entry.semanticid = sme.SemanticId;
                                }

                                if (entryType != EventPayloadEntryType.Deleted && withPayload)
                                {
                                    // var s = Converter.GetSubmodelElement(sme);
                                    var s = CrudOperator.ReadSubmodelElement(sme, treeMerged);
                                    if (s != null)
                                    {
                                        var j = Jsonization.Serialize.ToJsonObject(s);
                                        if (j != null)
                                        {
                                            entry.data = j;
                                        }
                                    }
                                }

                                entry.id = $"{entry.source}-{entry.time}";

                                eventPayload.elements.Add(entry);

                                diffEntry.Add(entry.eventPayloadEntryType.ToString() + " " + entry.GetIdShortPath());
                                Console.WriteLine($"Event {entry.eventPayloadEntryType.ToString()} Schema: {entry.dataschema} idShortPath: {entry.GetIdShortPath()}");
                                countSME++;
                            }
                        }
                    }
                }

                if (completeSM)
                {
                    if (sm.TimeStampTree > timeStampMax)
                    {
                        timeStampMax = sm.TimeStampTree;
                    }

                    var sourceString = Program.externalBlazor + "/submodels/" + Base64UrlEncoder.Encode(sm.Identifier);

                    var entry = new EventPayloadEntry();
                    entry.source = sourceString;
                    entry.SetType(entryType);
                    entry.time = TimeStamp.TimeStamp.DateTimeToString(sm.TimeStampTree);

                    entry.dataschema = EventPayloadEntry.SCHEMA_URL + "submodel";

                    if (sm.SemanticId != null)
                    {
                        entry.semanticid = sm.SemanticId;
                    }

                    if (withPayload)
                    {
                        var s = CrudOperator.ReadSubmodel(db, sm);
                        if (s != null)
                        {
                            var j = Jsonization.Serialize.ToJsonObject(s);
                            if (j != null)
                            {
                                entry.data = j;
                            }
                        }
                    }

                    entry.id = $"{entry.source}-{entry.time}";

                    diffEntry.Add(entry.eventPayloadEntryType.ToString() + " " + entry.GetIdShortPath());
                    Console.WriteLine($"Event {entry.eventPayloadEntryType.ToString()} Type: {entry.dataschema} idShortPath: {entry.GetIdShortPath()}");

                    eventPayload.elements.Add(entry);
                    countSM++;
                }
            }
            if (countSM == 0 && countSME == 0)
            {
                if (searchSM is "(*)" or "*" or "")
                {
                    timeStampMax = db.SMSets.Select(sm => sm.TimeStampTree).DefaultIfEmpty().Max();
                }
                else
                {
                    timeStampMax = db.SMSets.Where(searchSM).Select(sm => sm.TimeStampTree).DefaultIfEmpty().Max();
                }
            }
            eventPayload.time = TimeStamp.TimeStamp.DateTimeToString(timeStampMax);

            if (basicEventElementSourceString.IsNullOrEmpty())
            {
                //eventPayload.countSM = countSM;
                //e.status.countSME = countSME;
                if (countSM == limitSm)
                {
                    eventPayload.cursor = $"offsetSM={offsetSm + limitSm}";
                }
                else if (countSM == limitSm)
                {
                    eventPayload.cursor = $"offsetSME={offsetSme + limitSme}";
                }
            }
        }

        return eventPayload;
    }

    public int ChangeData(string json, EventDto eventData, AdminShellPackageEnv[] env, IReferable referable, out string transmit, out string lastDiffValue, out string statusValue, List<String> diffEntry, int packageIndex = -1)
    {
        transmit = "";
        lastDiffValue = "";
        statusValue = "ERROR";
        int count = 0;

        var statusData = eventData.StatusData;

        EventPayload eventPayload = null;
        try
        {
            eventPayload = JsonSerializer.Deserialize<EventPayload>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        transmit = eventPayload.transmitted;
        var dt = TimeStamp.TimeStamp.StringToDateTime(eventPayload.time);
        dt = DateTime.Parse(eventPayload.time);
        var dtTransmit = DateTime.Parse(eventPayload.transmitted);
        lastDiffValue = TimeStamp.TimeStamp.DateTimeToString(dt);

        ISubmodelElementCollection statusDataCollection = null;
        if (eventPayload.statusData != null && statusData != null)
        {
            ISubmodelElement receiveSme = null;
            MemoryStream mStrm = new MemoryStream(Encoding.UTF8.GetBytes(eventPayload.statusData.ToJsonString()));
            JsonNode node = System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(mStrm).Result;
            receiveSme = Jsonization.Deserialize.ISubmodelElementFrom(node);
            if (receiveSme is AasCore.Aas3_0.SubmodelElementCollection smc)
            {
                statusData.Value = new List<ISubmodelElement>();
                statusData.Add(smc);
                receiveSme.TimeStampCreate = dtTransmit;
                receiveSme.TimeStampDelete = new DateTime();
                receiveSme.SetAllParentsAndTimestamps(statusData, dtTransmit, receiveSme.TimeStampCreate, receiveSme.TimeStampDelete);
                receiveSme.SetTimeStamp(dtTransmit);
            }
        }

        AasCore.Aas3_0.Environment aasEnv = null;
        int index = -1;
        ISubmodelElementCollection dataCollection = null;
        List<ISubmodelElement> data = new List<ISubmodelElement>();
        SubmodelElementCollection status = null;
        AasCore.Aas3_0.Property message = null;
        AasCore.Aas3_0.Property transmitted = null;
        AasCore.Aas3_0.Property lastUpdate = null;
        SubmodelElementCollection diff = null;

        // sort by submodelID + entryType + idShortPath by CompareTo(EventPayloadEntry)
        eventPayload.elements.Sort();
        var entriesSubmodel = new List<EventPayloadEntry>();
        foreach (var entry in eventPayload.elements)
        {
            Console.WriteLine($"Event {entry.eventPayloadEntryType.ToString()} Type: {entry.dataschema} idShortPath: {entry.GetIdShortPath()}");
            Submodel receiveSM = null;
            if (entry.dataschema.Split("/")?.Last().ToLower() == "submodel")
            {
                if (entry.data != null)
                {
                    //ToDo: Test whether we need ToJsonString or whether we can desirialize another way
                    MemoryStream mStrm = new MemoryStream(Encoding.UTF8.GetBytes(entry.data.ToString()));
                    JsonNode node = System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(mStrm).Result;
                    receiveSM = Jsonization.Deserialize.SubmodelFrom(node);
                }
                if (receiveSM != null)
                {
                    using (var db = new AasContext())
                    {
                        if (entry.eventPayloadEntryType == EventPayloadEntryType.Created)
                        {
                            var smDBQuery = db.SMSets.Where(sm => sm.Identifier == receiveSM.Id);
                            var smDB = smDBQuery.ToList();
                            if (smDB != null && smDB.Count > 0)
                            {
                                smDBQuery.ExecuteDeleteAsync().Wait();
                                db.SaveChanges();
                            }
                        }
                        var visitor = new VisitorAASX(db);
                        visitor.update = entry.eventPayloadEntryType == EventPayloadEntryType.Updated;
                        visitor.currentDataTime = dt;
                        visitor.VisitSubmodel(receiveSM);
                        db.Add(visitor._smDB);
                        db.SaveChanges();
                        count++;
                    }
                }
            }
            if (entry.dataschema.Split("/")?.Last().ToLower() != "submodel")
            {
                bool changeSubmodel = false;
                bool addEntry = false;
                if (entriesSubmodel.Count == 0)
                {
                    addEntry = true;
                }
                else
                {
                    if (entry.GetSubdmodelId() != entriesSubmodel.Last().GetSubdmodelId())
                    {
                        changeSubmodel = true;
                    }
                }
                if (entry == eventPayload.elements.Last())
                {
                    addEntry = true;
                    changeSubmodel = true;
                }
                if (addEntry)
                {
                    entriesSubmodel.Add(entry);
                }
                if (changeSubmodel)
                {
                    var submodelIdentifier = entriesSubmodel.Last().GetSubdmodelId();
                    List<int> smeDelete = [];
                    using (var db = new AasContext())
                    {
                        var smDBQuery = db.SMSets.Where(sm => sm.Identifier == submodelIdentifier);
                        var smDB = smDBQuery.FirstOrDefault();
                        if (smDB == null)
                        {
                            smDB = new SMSet();
                            smDB.Identifier = submodelIdentifier;
                            var idShort = Regex.Replace(submodelIdentifier, @"[^\w\d]", "_");
                            smDB.IdShort = idShort;
                            smDB.Kind = ModellingKind.Instance.ToString();
                            CrudOperator.setTimeStamp(smDB, dt);
                            db.Add(smDB);
                            db.SaveChanges();
                        }
                        if (smDB != null)
                        {
                            var visitor = new VisitorAASX(db);
                            visitor._smDB = smDB;
                            visitor.currentDataTime = dt;
                            var smDBId = smDB.Id;
                            var smeSmList = db.SMESets.Where(sme => sme.SMId == smDBId).ToList();
                            CrudOperator.CreateIdShortPath(db, smeSmList);
                            var smeSmMerged = CrudOperator.GetSmeMerged(db, null, smeSmList, smDB);
                            visitor.smSmeMerged = smeSmMerged;

                            foreach (var e in entriesSubmodel)
                            {
                                bool change = false;
                                ISubmodelElement receiveSme = null;
                                if (e.data != null)
                                {
                                    //ToDo: Test whether we need ToJsonString or whether we can desirialize another way
                                    MemoryStream mStrm = new MemoryStream(Encoding.UTF8.GetBytes(entry.data.ToString()));
                                    JsonNode node = System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(mStrm).Result;
                                    receiveSme = Jsonization.Deserialize.ISubmodelElementFrom(node);
                                }
                                if (receiveSme != null)
                                {
                                    visitor.idShortPath = e.GetIdShortPath();
                                    visitor.update = e.eventPayloadEntryType == EventPayloadEntryType.Updated;
                                    var receiveSmeDB = visitor.VisitSMESet(receiveSme);
                                    if (receiveSmeDB != null)
                                    {
                                        receiveSmeDB.SMId = smDBId;

                                        var parentPath = "";
                                        if (e.GetIdShortPath().Contains("."))
                                        {
                                            int lastDotIndex = e.GetIdShortPath().LastIndexOf('.');
                                            if (lastDotIndex != -1)
                                            {
                                                parentPath = e.GetIdShortPath().Substring(0, lastDotIndex);
                                            }
                                        }
                                        else
                                        {
                                            change = true;
                                        }
                                        switch (e.eventPayloadEntryType)
                                        {
                                            case EventPayloadEntryType.Created:
                                            case EventPayloadEntryType.Updated:
                                                if (parentPath != "")
                                                {
                                                    var parentDB = smeSmMerged.Where(sme => sme.smeSet.IdShortPath == parentPath).FirstOrDefault();
                                                    if (parentDB != null)
                                                    {
                                                        receiveSmeDB.ParentSMEId = parentDB.smeSet.Id;
                                                        receiveSmeDB.IdShortPath = e.GetIdShortPath();
                                                        change = true;
                                                    }
                                                }
                                                if (change)
                                                {
                                                    CrudOperator.setTimeStampTree(db, smDB, receiveSmeDB, receiveSmeDB.TimeStamp);
                                                    try
                                                    {
                                                        db.SMESets.Add(receiveSmeDB);
                                                        db.SaveChanges();
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                    }
                                                    var smeDB = smeSmMerged.Where(sme =>
                                                            !visitor.keepSme.Contains(sme.smeSet.Id) &&
                                                            visitor.deleteSme.Contains(sme.smeSet.Id)
                                                        ).ToList();
                                                    var smeDBList = smeDB.Select(sme => sme.smeSet.Id).Distinct().ToList();
                                                    smeDelete.AddRange(smeDBList);
                                                    count++;
                                                }
                                                break;
                                        }
                                    }
                                }
                                else
                                {
                                    if (e.eventPayloadEntryType == EventPayloadEntryType.Deleted)
                                    {
                                        var notDeleted = e.notDeletedIdShortList;
                                        var parentPath = e.GetIdShortPath();
                                        db.SaveChanges();
                                        var deletePathList = smeSmMerged.Where(
                                            sme => sme.smeSet.IdShort != null
                                            && sme.smeSet.IdShortPath == parentPath + "." + sme.smeSet.IdShort
                                            && !notDeleted.Contains(sme.smeSet.IdShort))
                                            .Select(sme => sme.smeSet.IdShortPath).ToList();
                                        var smeDBList = new List<int>();
                                        foreach (var deletePath in deletePathList)
                                        {
                                            smeDBList.AddRange(
                                                smeSmMerged.Where(sme =>
                                                    sme.smeSet.IdShortPath != null &&
                                                    (sme.smeSet.IdShortPath == deletePath ||
                                                    sme.smeSet.IdShortPath.StartsWith(deletePath + ".")))
                                                .Select(sme => sme.smeSet.Id).ToList());
                                        }
                                        smeDelete.AddRange(smeDBList);
                                        count++;
                                    }
                                }
                            }
                        }
                        if (smeDelete.Count > 0)
                        {
                            db.SMESets.Where(sme => smeDelete.Contains(sme.Id)).ExecuteDeleteAsync().Wait();
                            db.SaveChanges();
                        }
                    }
                    entriesSubmodel.Clear();
                }
            }
        }

        statusValue = "Updated: " + count;

        return count;
    }

    private int ChangeSubmodelElement(EventDto eventData, EventPayloadEntry entry, IReferable parent, List<ISubmodelElement> submodelElements, string idShortPath, List<String> diffEntry)
    {
        int count = 0;
        var dt = DateTime.Parse(entry.time);

        int maxCount = 0;
        if (eventData.DataMaxSize != null && eventData.DataMaxSize.Value != null)
        {
            maxCount = Convert.ToInt32(eventData.DataMaxSize.Value);
        }

        ISubmodelElement receiveSme = null;
        if (entry.data != null)
        {
            //ToDo: Test whether we need ToJsonString or whether we can desirialize another way
            MemoryStream mStrm = new MemoryStream(Encoding.UTF8.GetBytes(entry.data.ToString()));
            JsonNode node = System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(mStrm).Result;
            receiveSme = Jsonization.Deserialize.ISubmodelElementFrom(node);
            receiveSme.Parent = parent;
        }

        if (entry.eventPayloadEntryType == EventPayloadEntryType.Created)
        {
            if (entry.GetIdShortPath().StartsWith(idShortPath))
            {
                var path = entry.GetIdShortPath();
                if (idShortPath != "")
                {
                    path = path.Replace(idShortPath, "");
                }
                if (!path.Contains("."))
                {
                    Console.WriteLine("Event CREATE SME: " + entry.GetIdShortPath());
                    receiveSme.TimeStampCreate = dt;
                    receiveSme.TimeStampDelete = new DateTime();
                    int i = 0;
                    bool found = false;
                    for (; i < submodelElements.Count; i++)
                    {
                        var sme = submodelElements[i];
                        if (sme.IdShort == path)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found)
                    {
                        submodelElements[i] = receiveSme;
                    }
                    else
                    {
                        submodelElements.Add(receiveSme);
                    }
                    receiveSme.SetAllParentsAndTimestamps(parent, dt, receiveSme.TimeStampCreate, receiveSme.TimeStampDelete);
                    receiveSme.SetTimeStamp(dt);
                    diffEntry.Add(entry.eventPayloadEntryType.ToString() + " " + entry.GetIdShortPath());
                    count++;
                    return count;
                }
                else
                {
                    for (int i = 0; i < submodelElements.Count; i++)
                    {
                        var sme = submodelElements[i];
                        path = idShortPath + sme.IdShort + ".";
                        switch (sme)
                        {
                            case ISubmodelElementCollection smc:
                                count += ChangeSubmodelElement(eventData, entry, smc, smc.Value, path, diffEntry);
                                break;
                            case ISubmodelElementList sml:
                                count += ChangeSubmodelElement(eventData, entry, sml, sml.Value, path, diffEntry);
                                break;
                        }
                    }
                }
            }
        }
        if (entry.eventPayloadEntryType == EventPayloadEntryType.Updated)
        {
            for (int i = 0; i < submodelElements.Count; i++)
            {
                var sme = submodelElements[i];
                if (entry.GetIdShortPath() == idShortPath + sme.IdShort)
                {
                    Console.WriteLine("Event UPDATE SME: " + entry.GetIdShortPath());
                    receiveSme.TimeStampCreate = submodelElements[i].TimeStampCreate;
                    receiveSme.TimeStampDelete = submodelElements[i].TimeStampDelete;
                    submodelElements[i] = receiveSme;
                    receiveSme.SetAllParentsAndTimestamps(parent, dt, receiveSme.TimeStampCreate, receiveSme.TimeStampDelete);
                    receiveSme.SetTimeStamp(dt);
                    diffEntry.Add(entry.eventPayloadEntryType.ToString() + " " + entry.GetIdShortPath());
                    count++;
                    return count;
                }
                var path = idShortPath + sme.IdShort + ".";
                if (entry.GetIdShortPath().StartsWith(path))
                {
                    switch (sme)
                    {
                        case ISubmodelElementCollection smc:
                            count += ChangeSubmodelElement(eventData, entry, smc, smc.Value, path, diffEntry);
                            break;
                        case ISubmodelElementList sml:
                            count += ChangeSubmodelElement(eventData, entry, sml, sml.Value, path, diffEntry);
                            break;
                    }
                }
            }
        }
        if (entry.eventPayloadEntryType == EventPayloadEntryType.Deleted)
        {
            if (maxCount == 0 || eventData.DataCollection != parent)
            {
                for (int i = 0; i < submodelElements.Count; i++)
                {
                    var sme = submodelElements[i];
                    List<ISubmodelElement> children = new List<ISubmodelElement>();
                    switch (sme)
                    {
                        case ISubmodelElementCollection smc:
                            children = smc.Value;
                            break;
                        case ISubmodelElementList sml:
                            children = sml.Value;
                            break;
                    }
                    if (entry.GetIdShortPath() == idShortPath + sme.IdShort)
                    {
                        Console.WriteLine("Event DELETE SME: " + entry.GetIdShortPath());
                        if (children.Count != 0)
                        {
                            int c = 0;
                            while (c < children.Count)
                            {
                                if (!entry.notDeletedIdShortList.Contains(children[c].IdShort))
                                {
                                    children.RemoveAt(c);
                                    sme.SetTimeStampDelete(dt);
                                    count++;
                                }
                                else
                                {
                                    c++;
                                }
                            }
                        }
                        diffEntry.Add(entry.eventPayloadEntryType.ToString() + " " + entry.GetIdShortPath() + ".*");
                        count++;
                        break;
                    }
                    var path = idShortPath + sme.IdShort + ".";
                    if (entry.GetIdShortPath().StartsWith(path))
                    {
                        count += ChangeSubmodelElement(eventData, entry, sme, children, path, diffEntry);
                    }
                }
            }
        }

        if (maxCount != 0 && eventData.DataCollection == parent)
        {
            SubmodelElementCollection data = null;
            if (eventData.DataCollection is SubmodelElementCollection dataSmc)
            {
                data = dataSmc;
            }
            // if (eventData.direction != null && eventData.direction.Value == "IN" && eventData.mode != null && (eventData.mode.Value == "PUSH" || eventData.mode.Value == "PUT"))
            if (data != null && eventData.Direction != null && eventData.Direction.Value == "IN" && eventData.Mode != null)
            {
                if (data.Value != null && data.Value.Count == 1 && data.Value[0] is SubmodelElementCollection smc)
                {
                    data = smc;

                    while (data.Value != null && data.Value.Count > maxCount)
                    {
                        data.Value.RemoveAt(0);
                        data.SetTimeStampDelete(dt);
                    }
                }
            }

        }

        return count;
    }


    /***********************           Event data           ****************************************/

    public Operation FindEvent(ISubmodel submodel, string eventPath)
    {
        return FindEvent(submodel, null, eventPath);
    }

    private Operation FindEvent(ISubmodel submodel, ISubmodelElement sme, string eventPath)
    {
        var children = new List<ISubmodelElement>();

        if (submodel != null)
        {
            children = submodel.SubmodelElements;
        }
        if (sme != null && sme is SubmodelElementCollection smc)
        {
            children = smc.Value;
        }
        if (sme != null && sme is SubmodelElementList sml)
        {
            children = sml.Value;
        }

        foreach (var c in children)
        {
            if (c is Operation op && c.IdShort == eventPath)
            {
                return op;
            }
            if (c is SubmodelElementCollection || c is SubmodelElementCollection)
            {
                if (eventPath.StartsWith(c.IdShort + "."))
                {
                    var result = FindEvent(null, c, eventPath.Substring(c.IdShort.Length + 1));
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
        }

        return null;
    }

    public EventDto ParseData(Operation op, AdminShellPackageEnv env)
    {
        SubmodelElementCollection smec = null;
        SubmodelElementList smel = null;
        Submodel sm = null;
        AasCore.Aas3_0.Property p = null;
        EventDto eventDto = new EventDto();

        eventDto.env = env;
        eventDto.IdShort = op.IdShort;
        eventDto.SemanticId = op.SemanticId;

        foreach (var input in op.InputVariables)
        {
            smec = null;
            smel = null;
            sm = null;
            p = null;
            var inputRef = input.Value;
            if (inputRef is AasCore.Aas3_0.Property)
            {
                p = (inputRef as AasCore.Aas3_0.Property);
                if (p.Value == null)
                {
                    p = null;
                }
            }

            if (inputRef is SubmodelElementCollection)
            {
                smec = (inputRef as SubmodelElementCollection);
            }
            if (inputRef is SubmodelElementList)
            {
                smel = (inputRef as SubmodelElementList);
            }

            if (inputRef is ReferenceElement)
            {
                var refElement = env.AasEnv.FindReferableByReference((inputRef as ReferenceElement).Value);
                if (refElement is SubmodelElementCollection)
                {
                    smec = refElement as SubmodelElementCollection;
                }
                if (refElement is SubmodelElementList)
                {
                    smel = refElement as SubmodelElementList;
                }
                if (refElement is Submodel)
                {
                    sm = refElement as Submodel;
                }
            }

            switch (inputRef.IdShort.ToLower())
            {
                case "direction":
                    if (p != null)
                        eventDto.Direction = p;
                    break;
                case "mode":
                    if (p != null)
                        eventDto.Mode = p;
                    break;
                case "authentication":
                    if (smec != null)
                        eventDto.Authentication = smec;
                    break;
                case "endpoint":
                    if (p != null)
                    {
                        if (p.Value != null && p.Value.StartsWith("$"))
                        {
                            var envVarName = p.Value.Substring(1);
                            var url = System.Environment.GetEnvironmentVariable(envVarName);
                            if (url != null)
                            {
                                Console.WriteLine($"{p.Value} = {url}");
                                p.Value = url;
                            }
                            else
                            {
                                Console.WriteLine($"Environment variable {envVarName} not found.");
                            }
                        }
                        eventDto.EndPoint = p;
                    }
                    break;
                case "data":
                case "observed":
                    if (inputRef is ReferenceElement r)
                    {
                        eventDto.DataReference = r;
                    }
                    if (sm != null)
                        eventDto.DataSubmodel = sm;
                    if (smec != null)
                        eventDto.DataCollection = smec;
                    if (smel != null)
                        eventDto.DataCollection = smel;
                    break;
                case "datamaxsize":
                    if (p != null)
                        eventDto.DataMaxSize = p;
                    break;
                case "statusdata":
                    if (smec != null)
                        eventDto.StatusData = smec;
                    break;
                case "persistence":
                    if (p != null)
                        eventDto.Persistence = p;
                    break;
                case "conditionsm":
                    if (p != null)
                        eventDto.ConditionSM = p;
                    break;
                case "conditionsme":
                    if (p != null)
                        eventDto.ConditionSME = p;
                    break;
                case "messagebroker":
                    if (p != null)
                        eventDto.MessageBroker = p;
                    break;
                case "messagetopictype":
                    if (p != null)
                        eventDto.MessageTopicType = p;
                    break;
                case "include":
                    if (p != null)
                        eventDto.Include = p;
                    break;
                case "submodelsonly":
                    if (p != null)
                        eventDto.SubmodelsOnly = p;
                    break;
                case "publishbasiceventelement":
                    if (p != null)
                        eventDto.PublishBasicEventElement = p;
                    break;
                case "mininterval":
                    if (p != null)
                        eventDto.MinInterval = p;
                    break;
                case "maxinterval":
                    if (p != null)
                        eventDto.MaxInterval = p;
                    break;
                case "action":
                    if (p != null)
                        eventDto.Action = p;
                    break;
                case "messagecondition":
                    if (p != null)
                        eventDto.MessageCondition = p;
                    break;
                case "domain":
                    if (p != null)
                        eventDto.Domain = p;
                    break;
            }
        }
        /*
        if (dataMaxSize == null)
        {
            var timeStamp = DateTime.UtcNow;
            dataMaxSize = new AasCore.Aas3_0.Property(DataTypeDefXsd.String, idShort: "dataMaxSize", value: "");
            var dataMaxSizeOp = new OperationVariable(dataMaxSize);
            dataMaxSize.SetAllParentsAndTimestamps(null, timeStamp, timeStamp, DateTime.MinValue);
            op.InputVariables.Add(dataMaxSizeOp);
        }
        */

        foreach (var output in op.OutputVariables)
        {
            smec = null;
            sm = null;
            p = null;
            var outputRef = output.Value;
            if (outputRef is AasCore.Aas3_0.Property)
            {
                p = (outputRef as AasCore.Aas3_0.Property);
            }

            if (outputRef is SubmodelElementCollection)
            {
                smec = outputRef as SubmodelElementCollection;
            }

            if (outputRef is ReferenceElement)
            {
                var refElement = env.AasEnv.FindReferableByReference((outputRef as ReferenceElement).Value);
                if (refElement is SubmodelElementCollection)
                    smec = refElement as SubmodelElementCollection;
                if (refElement is Submodel)
                    sm = refElement as Submodel;
            }

            switch (outputRef.IdShort.ToLower())
            {
                case "lastupdate":
                    if (p != null)
                        eventDto.LastUpdate = p;
                    break;
                case "status":
                    if (smec != null)
                        eventDto.Status = smec;
                    break;
                case "statusdata":
                    if (smec != null)
                        eventDto.StatusData = smec;
                    break;
                case "diff":
                    if (smec != null)
                        eventDto.Diff = smec;
                    break;
            }
        }

        if (eventDto.Status != null)
        {
            foreach (var sme in eventDto.Status.Value)
            {
                switch (sme.IdShort.ToLower())
                {
                    case "message":
                        if (sme is AasCore.Aas3_0.Property)
                        {
                            eventDto.Message = sme as AasCore.Aas3_0.Property;
                        }
                        break;
                    case "transmitted":
                        if (sme is AasCore.Aas3_0.Property)
                        {
                            eventDto.Transmitted = sme as AasCore.Aas3_0.Property;
                        }
                        break;
                    case "lastupdate":
                        if (sme is AasCore.Aas3_0.Property)
                        {
                            eventDto.LastUpdate = sme as AasCore.Aas3_0.Property;
                        }
                        break;
                    case "diff":
                        if (sme is SubmodelElementCollection)
                        {
                            eventDto.Diff = sme as SubmodelElementCollection;
                        }
                        break;
                }
            }
        }

        if (eventDto.Authentication != null)
        {
            smec = eventDto.Authentication;
            int countSmec = smec.Value.Count;
            for (int iSmec = 0; iSmec < countSmec; iSmec++)
            {
                var sme2 = smec.Value[iSmec];
                var idShort = sme2.IdShort.ToLower();

                switch (idShort)
                {
                    case "authtype":
                        if (sme2 is AasCore.Aas3_0.Property)
                        {
                            eventDto.AuthType = sme2 as AasCore.Aas3_0.Property;
                        }

                        break;

                    case "accesstoken":
                        if (sme2 is AasCore.Aas3_0.Property)
                        {
                            eventDto.AccessToken = sme2 as AasCore.Aas3_0.Property;
                        }

                        break;

                    case "clienttoken":
                        if (sme2 is AasCore.Aas3_0.Property)
                        {
                            eventDto.ClientToken = sme2 as AasCore.Aas3_0.Property;
                        }

                        break;

                    case "username":
                        if (sme2 is AasCore.Aas3_0.Property)
                        {
                            eventDto.UserName = sme2 as AasCore.Aas3_0.Property;
                        }

                        break;

                    case "password":
                        if (sme2 is AasCore.Aas3_0.Property)
                        {
                            eventDto.PassWord = sme2 as AasCore.Aas3_0.Property;
                        }

                        break;

                    case "authservercertificate":
                        if (sme2 is AasCore.Aas3_0.File)
                        {
                            eventDto.AuthServerCertificate = sme2 as AasCore.Aas3_0.File;
                        }

                        break;

                    case "authserverendpoint":
                        if (sme2 is AasCore.Aas3_0.Property)
                        {
                            eventDto.AuthServerEndPoint = sme2 as AasCore.Aas3_0.Property;
                        }

                        break;

                    case "clientcertificate":
                        if (sme2 is AasCore.Aas3_0.File)
                        {
                            eventDto.ClientCertificate = sme2 as AasCore.Aas3_0.File;
                        }

                        break;

                    case "clientcertificatepassword":
                        if (sme2 is AasCore.Aas3_0.Property)
                        {
                            eventDto.ClientCertificatePassWord = sme2 as AasCore.Aas3_0.Property;
                        }

                        break;
                }
            }
        }
        return eventDto;
    }

    public async void NotifySubmodelDeleted(AasCore.Aas3_0.Submodel submodel)
    {
        var mqtttOutEventDtos = EventDtos.Where(eventData => eventData.Direction.Value == "OUT" && eventData.Mode.Value == "MQTT");

        string searchSM = string.Empty;
        EventDto notificationEventDto = null;

        foreach (var mqttEventDto in mqtttOutEventDtos)
        {
            if (mqttEventDto.ConditionSM != null && mqttEventDto.ConditionSM.Value != null)
            {
                searchSM = mqttEventDto.ConditionSM.Value;

                //ToDo: Use correct syntax?
                if (searchSM.Contains("idShort") && searchSM.Contains("==") && searchSM.Contains(submodel.IdShort))
                {
                    notificationEventDto = mqttEventDto;
                }
                else if (searchSM.Contains("id") && searchSM.Contains("==") && searchSM.Contains(submodel.Id))
                {
                    notificationEventDto = mqttEventDto;
                }
                //ToDo: Security has allowed to delete the sm, do we need security check anyway?
                //if (securityCondition != null && securityCondition.TryGetValue("sm.", out _))
                //{
                //    if (searchSM == string.Empty)
                //    {
                //        searchSM = securityCondition["sm."];
                //    }
                //    else
                //    {
                //        searchSM = $"({securityCondition["sm."]})&&({searchSM})";
                //    }
                //}

            }
        }

        if (notificationEventDto != null
            && !_enableMqtt
            && notificationEventDto.MessageBroker != null
            && !notificationEventDto.MessageBroker.Value.IsNullOrEmpty()
            && notificationEventDto.PassWord != null
            && notificationEventDto.PassWord.Value != null
            && notificationEventDto.UserName != null
            && notificationEventDto.UserName.Value != null)
        {
            var eventPayload = new EventPayload(false);
            List<String> diffEntry = new List<String>();

            eventPayload.transmitted = TimeStamp.TimeStamp.DateTimeToString(DateTime.UtcNow);

            eventPayload.time = "";

            if (notificationEventDto.Domain != null)
            {
                eventPayload.domain = notificationEventDto.Domain.Value;
            }

            eventPayload.time = TimeStamp.TimeStamp.DateTimeToString(DateTime.UtcNow);

            bool isPublishEventElement = notificationEventDto.PublishBasicEventElement != null
                && notificationEventDto.PublishBasicEventElement.Value != null && notificationEventDto.PublishBasicEventElement.Value == "true";


            if (isPublishEventElement)
            {

                if (notificationEventDto.IdShort != null)
                {
                    eventPayload.source = $"{Program.externalBlazor}/submodels/{Base64UrlEncoder.Encode(notificationEventDto.SubmodelId)}/events/{notificationEventDto.IdShortPath}.{notificationEventDto.IdShort}";
                }
                eventPayload.semanticid = (notificationEventDto.SemanticId != null && notificationEventDto.SemanticId?.Keys != null) ? notificationEventDto.SemanticId?.Keys[0].Value : "";
                eventPayload.dataschema = "https://api.swaggerhub.com/domains/Plattform_i40/Part1-MetaModel-Schemas/V3.1.0#/components/schemas/BasicEventElement";

            }
            else
            {
                var sourceString = Program.externalBlazor + "/submodels/" + Base64UrlEncoder.Encode(submodel.Id);

                eventPayload.source = sourceString;
                eventPayload.dataschema = EventPayloadEntry.SCHEMA_URL + "submodel";

                if (submodel.SemanticId != null)
                {
                    eventPayload.semanticid = (submodel.SemanticId != null && submodel.SemanticId?.Keys != null) ? submodel.SemanticId?.Keys[0].Value : "";
                }

                bool withPayloadInclude = notificationEventDto.Include != null
                    && notificationEventDto.Include.Value != null && notificationEventDto.Include.Value == "true";

                if (withPayloadInclude)
                {
                    if (submodel != null)
                    {
                        var j = Jsonization.Serialize.ToJsonObject(submodel);
                        if (j != null)
                        {
                            eventPayload.data = j;
                        }
                    }
                }

                //ToDo: Check diff entry
                diffEntry.Add(eventPayload.type + " " + eventPayload.source);
            }
            eventPayload.id = $"{eventPayload.source}-{eventPayload.time}";
            eventPayload.type = EventPayloadEntryType.Deleted.ToString();

            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var payloadObjString = JsonSerializer.Serialize(eventPayload, options);

            try
            {
                var clientId = Program.externalBlazor + "/submodels/" + Base64UrlEncoder.Encode(notificationEventDto.SubmodelId);

                if (!notificationEventDto.IdShortPath.IsNullOrEmpty())
                {
                    clientId += $"/submodel-elements/{notificationEventDto.IdShortPath}.{notificationEventDto.IdShort}";
                }

                var result = await _mqttClientService.PublishAsync(clientId, notificationEventDto.MessageBroker.Value,
                    notificationEventDto.MessageTopicType.Value,
                    notificationEventDto.UserName.Value, notificationEventDto.PassWord.Value, payloadObjString);

                var now = DateTime.UtcNow;

                bool isSucceeded = false;

                if (result != null && result.IsSuccess)
                {
                    isSucceeded = true;
                    Console.WriteLine("MQTT message sent.");
                }

                if (isSucceeded)
                {
                    if (notificationEventDto.Transmitted != null)
                    {
                        notificationEventDto.Transmitted.Value = eventPayload.transmitted;
                        notificationEventDto.Transmitted.SetTimeStamp(now);
                    }
                    var dt = DateTime.Parse(eventPayload.time);
                    if (notificationEventDto.LastUpdate != null)
                    {
                        notificationEventDto.LastUpdate.Value = eventPayload.time;
                        notificationEventDto.LastUpdate.SetTimeStamp(dt);
                    }
                    if (notificationEventDto.Status != null)
                    {
                        if (notificationEventDto.Message != null)
                        {
                            //ToDo: Is message really correct? 
                            notificationEventDto.Message.Value = "on";
                        }
                        notificationEventDto.Status.SetTimeStamp(now);
                    }
                    if (notificationEventDto.Diff != null && diffEntry.Count > 0)
                    {
                        notificationEventDto.Diff.Value = new List<ISubmodelElement>();
                        int i = 0;
                        foreach (var dif in diffEntry)
                        {
                            var p = new Property(DataTypeDefXsd.String);
                            p.IdShort = "diff" + i;
                            p.Value = dif;
                            p.SetTimeStamp(dt);
                            notificationEventDto.Diff.Value.Add(p);
                            p.SetAllParentsAndTimestamps(notificationEventDto.Diff, dt, dt, DateTime.MinValue);
                            i++;
                        }
                        notificationEventDto.Diff.SetTimeStamp(dt);
                    }
                    Program.signalNewData(2);
                }
                else
                {
                    var statusCode = "";

                    if (result != null)
                    {
                        statusCode = result.ReasonCode.ToString();
                    }

                    if (notificationEventDto.Status != null
                        && notificationEventDto.Message != null)
                    {
                        notificationEventDto.Message.Value = "ERROR: " +
                            statusCode + " ; " +
                            " ; PUT " + notificationEventDto.MessageBroker.Value;
                        notificationEventDto.Status.SetTimeStamp(now);
                    }
                }
            }
            catch (Exception ex)
            {
                if (notificationEventDto.Message != null)
                {
                    notificationEventDto.Message.Value = "ERROR: " +
                        ex.Message +
                        " ; PUT " + notificationEventDto.MessageBroker.Value;
                }
                var now = DateTime.UtcNow;
                notificationEventDto.Status.SetTimeStamp(now);
                // d = eventData.LastUpdate.Value = "reconnect";
            }
        }

        notificationEventDto.env.setWrite(true);
    }
}
