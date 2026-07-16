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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AasxServer;
using AasxServerDB;
using AasxServerDB.Entities;
using AasxServerStandardBib.Logging;
using AdminShellNS;
using Contracts;
using Contracts.Events;
using Extensions;
using IdentityModel.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

public class EventService : IEventService
{
    private static readonly ILogger _logger = ApplicationLogging.CreateLogger("EventService");

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
        if (!IsPublishMqttConfigured(eventData))
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
                                eventData.UserName.Value, eventData.PassWord.Value, eventData?.AccessToken?.Value);
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
        if (!IsPublishMqttConfigured(eventData))
        {
            //ToDo: logging?
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
        if (!IsPublishMqttConfigured(eventData))
        {
            //ToDo: logging?
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

        string domain = null;
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

        bool showTransmitted = false;
        if (eventData.ShowTransmitted != null
                && eventData.ShowTransmitted.Value != null)
        {
            showTransmitted = eventData.ShowTransmitted.Value.ToLower() == "true";
        }

        var e = CollectPayload(null, false, sourceString, semanticId, domain, eventData.ConditionSM, eventData.ConditionSME,
            d, diffEntry, transmitted, minInterval, maxInterval, wp, smOnly, 1000, 0, showTransmitted);

        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        if (e.Count > 0)
        {
            foreach (var eventElement in e)
            {
                var payloadObjString = JsonSerializer.Serialize(eventElement, options);

                try
                {
                    var result = await _mqttClientService.PublishAsync(clientId, eventData.MessageBroker.Value, eventData.MessageTopicType.Value,
                        eventData?.UserName?.Value, eventData?.PassWord?.Value, eventData?.AccessToken?.Value, payloadObjString);

                    var now = DateTime.UtcNow;

                    bool isSucceeded = false;

                    if (result != null && result.IsSuccess)
                    {
                        isSucceeded = true;
                        _logger.LogDebug($"MQTT message sent on message topic {eventData.MessageTopicType.Value}.");
                    }

                    if (isSucceeded)
                    {
                        if (eventData.Transmitted != null)
                        {
                            eventData.Transmitted.Value = e[0].transmitted;
                            eventData.Transmitted.SetTimeStamp(now);
                        }
                        var dt = DateTime.Parse(e[0].time);
                        if (eventData.LastUpdate != null)
                        {
                            eventData.LastUpdate.Value = e[0].time;
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

                    _logger.LogDebug($"FAILED: Send MQTT message on message topic {eventData.MessageTopicType.Value}.");

                    // d = eventData.LastUpdate.Value = "reconnect";
                }
            }

            eventData.env.setWrite(true);
        }
    }

    public async void PublishRestApiMessage(EventDto eventData, string submodelId, string idShortPath)
    {
        if (!IsPublishRestApiConfigured(eventData))
        {
            //ToDo: logging?
            return;
        }

        var minInterval = TimeSpan.Zero;
        var maxInterval = TimeSpan.Zero;
        var diffTime = DateTime.MinValue;


        var d = "";
        if (eventData.LastUpdate != null && !eventData.LastUpdate.Value.IsNullOrEmpty())
        {
            diffTime = DateTime.ParseExact(eventData.LastUpdate.Value, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        }

        bool wp = false;

        //ToDo: Is empty data in Events Feed API spec allowed?
        //if (eventData.Include != null
        //    && eventData.Include.Value != null)
        //{
        //    wp = eventData.Include.Value.ToLower() == "true";
        //}

        wp = true;

        var e = CollectPayloadForRestApi(null, eventData.ConditionSM, eventData.ConditionSME,
             minInterval, maxInterval, wp, diffTime);

        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var now = DateTime.UtcNow;

        if (e.Count > 0)
        {
            var payloadObjString = JsonSerializer.Serialize(e, options);

            try
            {
                HttpClientHandler handler = new HttpClientHandler()
                {
                    Proxy = HttpClient.DefaultProxy,
                    DefaultProxyCredentials = CredentialCache.DefaultCredentials
                };

                HttpClient client = new HttpClient(handler);

                string user = "John Doe";
                string password = null;

                if (eventData.AccessToken != null && eventData.AccessToken.Value != null && eventData.AccessToken.Value != "")
                {
                    client.SetBearerToken(eventData.AccessToken.Value);
                }
                else
                {
                    if (eventData.UserName != null && eventData.PassWord != null)
                    {
                        user = eventData.UserName.Value;
                        password = eventData.PassWord.Value;
                    }
                }

                string requestPath = $"{eventData.EndPoint.Value}?user={user}";

                using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestPath))
                {
                    var content = new StringContent(payloadObjString, System.Text.Encoding.UTF8, "application/json");
                    requestMessage.Content = content;

                    if (!user.IsNullOrEmpty()
                         && !password.IsNullOrEmpty())
                    {
                        requestMessage.Headers.Authorization = new BasicAuthenticationHeaderValue(user, eventData.PassWord.Value);
                    }

                    client.DefaultRequestHeaders.Add("user", user);

                    HttpResponseMessage response = null;
                    var task = Task.Run(async () =>
                    {
                        response = await client.SendAsync(requestMessage);

                        var now = DateTime.UtcNow;
                        if (!response.IsSuccessStatusCode)
                        {
                            if (eventData.Status != null)
                            {
                                if (eventData.Message != null)
                                {
                                    eventData.Message.Value = "ERROR: " +
                                        response.StatusCode.ToString() + " ; " +
                                        response.Content.ReadAsStringAsync().Result +
                                        " ; PUT " + requestPath;
                                }
                                eventData.Status.SetTimeStamp(now);
                                // d = eventData.LastUpdate.Value = "reconnect";
                                eventData.LastUpdate.SetTimeStamp(now);
                            }
                        }
                        else
                        {
                            if (eventData.Transmitted != null)
                            {
                                eventData.Transmitted.Value = e[0].transmitted;
                                eventData.Transmitted.SetTimeStamp(now);
                            }
                            var maxTimeDt = e.Max(e => e.lastUpdate);

                            if (eventData.LastUpdate != null)
                            {
                                eventData.LastUpdate.Value = TimeStamp.TimeStamp.DateTimeToString(maxTimeDt);
                                eventData.LastUpdate.SetTimeStamp(maxTimeDt);
                            }
                        }
                    });
                    task.Wait();
                }
            }
            catch (Exception ex)
            {
                if (eventData.Message != null)
                {
                    eventData.Message.Value = "ERROR: " +
                        ex.Message +
                        " ; PUT " + eventData.EndPoint.Value;
                }
                eventData.Status.SetTimeStamp(now);
            }

            eventData.env.setWrite(true);
        }
    }

    private bool IsPublishMqttConfigured(EventDto eventData)
    {
        if (!_enableMqtt)
            return false;

        bool isMessageBrokerPresent = eventData.MessageBroker != null
            || !eventData.MessageBroker.Value.IsNullOrEmpty();

        //ToDo: allow empty username or password or not?
        bool isAuthenticationPresent = (eventData.PassWord != null
            && eventData.PassWord.Value != null
            && eventData.UserName != null
            && eventData.UserName.Value != null)
            || (eventData.AccessToken != null
            && !eventData.AccessToken.Value.IsNullOrEmpty());

        return isMessageBrokerPresent
            && isAuthenticationPresent;
    }

    private bool IsPublishRestApiConfigured(EventDto eventData)
    {
        bool isRestApiAdressPresent = eventData.EndPoint != null
            || !eventData.EndPoint.Value.IsNullOrEmpty();

        //bool isAuthenticationPresent = (eventData.PassWord != null
        //    && eventData.PassWord.Value != null
        //    && eventData.UserName != null
        //    && eventData.UserName.Value != null)
        //    || (eventData.AccessToken != null
        //    && !eventData.AccessToken.Value.IsNullOrEmpty());

        return isRestApiAdressPresent;
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

    private string CreateIdString(EventPayload entry)
    {
        return $"{GetSha1Base64URL(entry.source)}~~{entry.time}~~{entry.cursor}";
    }

    private string GetSha1Base64URL(string input)
    {
        var hashBytes = SHA1.HashData(Encoding.UTF8.GetBytes(input));

        return Base64UrlEncoder.Encode(hashBytes);
    }

    private List<EventPayload> CollectPayloadForRestApi(SqlConditions? securitySqlConditions,
    AasCore.Aas3_1.Property conditionSM, AasCore.Aas3_1.Property conditionSME, TimeSpan minInterval, TimeSpan maxInterval,
    bool withPayload, DateTime diffTime)
    {
        bool isREST = true;

        var eventPayloadList = new List<EventPayload>();

        var eventPayload = new EventPayload(isREST);
        diffTime = diffTime.AddMilliseconds(1);

        var offsetSm = 0;
        var limitSm = 500;

        lock (EventLock)
        {
            string searchSM = string.Empty;
            string searchSME = string.Empty;
            if (conditionSM != null && conditionSM.Value != null)
            {
                searchSM = conditionSM.Value;
            }
            var securityConditionSM = securitySqlConditions?.FormulaConditionsCSharp.GetValueOrDefault("sm.", "");
            if (!string.IsNullOrWhiteSpace(securityConditionSM))
            {
                if (searchSM == string.Empty)
                {
                    searchSM = securityConditionSM;
                }
                else
                {
                    searchSM = $"({securityConditionSM})&&({searchSM})";
                }
            }
            if (conditionSME != null && conditionSME.Value != null)
            {
                searchSME = conditionSME.Value;
            }
            var securityConditionSME = securitySqlConditions?.FormulaConditionsCSharp.GetValueOrDefault("sme.", "");
            if (!string.IsNullOrWhiteSpace(securityConditionSME))
            {
                if (searchSME == string.Empty)
                {
                    searchSME = securityConditionSME;
                }
                else
                {
                    searchSME = $"({securityConditionSME})&&({searchSME})";
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

            eventPayload.SetTime(timeStampMax);

            IQueryable<SMSet> smSearchSet = db.SMSets;
            if (!searchSM.IsNullOrEmpty() && searchSM != "*")
            {
                smSearchSet = smSearchSet.Where(searchSM);
            }

            smSearchSet = smSearchSet.Where(sm => (sm.TimeStampCreate > diffTime) ||
                ((sm.TimeStampTree != sm.TimeStampCreate) && (sm.TimeStampTree > diffTime) && (sm.TimeStampCreate <= diffTime)));

            smSearchSet = smSearchSet.OrderBy(sm => sm.TimeStampTree).Skip(offsetSm).Take(limitSm);
            var smSearchList = smSearchSet.ToList();

            foreach (var sm in smSearchList)
            {
                var entryType = EventPayloadType.Updated;
                if (sm.TimeStampCreate > diffTime)
                {
                    entryType = EventPayloadType.Created;
                }

                if (sm.TimeStampTree > timeStampMax)
                {
                    timeStampMax = sm.TimeStampTree;
                }

                var sourceString = Program.externalBlazor + "/submodels/" + Base64UrlEncoder.Encode(sm.Identifier);

                var entry = new EventPayload(isREST);

                entry.source = sourceString;
                entry.SetSubmodelType(entryType);
                entry.SetTime(sm.TimeStampTree);

                //entry.time = TimeStamp.TimeStamp.DateTimeToString(sm.TimeStampTree);

                entry.dataSchema = EventPayload.REST_API_SM_SCHEMA_URL;

                if (sm.SemanticId != null && !isREST)
                {
                    entry.semanticid = sm.SemanticId;
                }

                if (isREST && sm.Identifier != null)
                {
                    entry.subject = sm.Identifier;
                }

                if (withPayload)
                {
                    var s = CrudOperator.ReadSubmodel(db, sm, loadIntoMemoryWithoutElements: true,
                        securitySqlConditions: securitySqlConditions,
                        skipAllowCheck: true);
                    if (s != null)
                    {
                        var j = Jsonization.Serialize.ToJsonObject(s);
                        if (j != null)
                        {
                            entry.data = ConvertSmJsonToRestApiSpecSmJson(j);
                        }
                    }
                }
                _logger.LogDebug($"Event id: {entry.id}, Type: {entry.type}");
                eventPayloadList.Add(entry);
            }
        }

        return eventPayloadList;
    }

    private JsonObject ConvertSmJsonToRestApiSpecSmJson(JsonObject json)
    {
        var restApiSubmodel = new JsonObject();

        var jSemanticId = json["semanticId"]?.DeepClone();

        if (jSemanticId != null)
        {
            var restApiSemanticId = new JsonObject();
            restApiSemanticId.TryAdd("_type", jSemanticId["type"]?.DeepClone());

            var jKeys = jSemanticId["keys"] as JsonArray;
            var restApiSemanticIdKeys = new JsonArray();

            for (int i = 0; i < jKeys.Count(); i++)
            {
                var restApiSemanticKey = new JsonObject();

                restApiSemanticKey.TryAdd("_type", jKeys[i]["type"].DeepClone());
                restApiSemanticKey.TryAdd("value", jKeys[i]["value"].DeepClone());

                restApiSemanticIdKeys.Add(restApiSemanticKey);
            }
            restApiSemanticId.TryAdd("keys", restApiSemanticIdKeys);

            restApiSubmodel.TryAdd("semanticId", restApiSemanticId);
        }

        restApiSubmodel.TryAdd("submodelId", json["id"]?.DeepClone());

        return restApiSubmodel;
    }

    public List<EventPayload> CollectPayload(SqlConditions? securitySqlConditions, bool isREST, string basicEventElementSourceString,
        string basicEventElementSemanticId, string domain, AasCore.Aas3_1.Property conditionSM, AasCore.Aas3_1.Property conditionSME,
        string diff, List<String> diffEntry, DateTime transmitted, TimeSpan minInterval, TimeSpan maxInterval,
        bool withPayload, bool smOnly, int limitSm, int offsetSm, bool showTransmitted, SubmodelElementCollection statusData = null)
    {
        var eventPayloadList = new List<EventPayload>();

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
                return eventPayloadList;
            }

            diffTime = DateTime.Parse(diff);
            diffTime = diffTime.AddMilliseconds(1);
        }

        if (showTransmitted)
        {
            eventPayload.transmitted = TimeStamp.TimeStamp.DateTimeToString(DateTime.UtcNow);
        }

        eventPayload.SetTime(DateTime.MinValue);
        eventPayload.domain = domain;

        //ToDo: Currently statusData parameter is set to null in every call, to be deleted?
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
            var securityConditionSM = securitySqlConditions?.FormulaConditionsCSharp.GetValueOrDefault("sm.", "");
            if (!string.IsNullOrWhiteSpace(securityConditionSM))
            {
                if (searchSM == string.Empty)
                {
                    searchSM = securityConditionSM;
                }
                else
                {
                    searchSM = $"({securityConditionSM})&&({searchSM})";
                }
            }
            if (conditionSME != null && conditionSME.Value != null)
            {
                searchSME = conditionSME.Value;
            }
            var securityConditionSME = securitySqlConditions?.FormulaConditionsCSharp.GetValueOrDefault("sme.", "");
            if (!string.IsNullOrWhiteSpace(securityConditionSME))
            {
                if (searchSME == string.Empty)
                {
                    searchSME = securityConditionSME;
                }
                else
                {
                    searchSME = $"({securityConditionSME})&&({searchSME})";
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

            eventPayload.SetTime(timeStampMax);

            if (diff == "status")
            {
                //ToDo: What should happen here?
                //AddStatus(basicEventElementSourceString, basicEventElementSemanticId, ref eventPayload);
                eventPayloadList.Add(eventPayload);
                return eventPayloadList;
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
                        AddStatus(basicEventElementSourceString, basicEventElementSemanticId, ref eventPayload);

                        if (!basicEventElementSourceString.IsNullOrEmpty())
                        {
                            eventPayloadList.Add(eventPayload);
                        }
                        else
                        {
                            //Empty list (do not send anything)
                            //ToDo: Add element for non-basiceventelement keep alive message
                        }
                        return eventPayloadList;
                    }
                    else
                    {
                        //Empty list (do not send anything)
                        return eventPayloadList;
                    }
                }
                else
                {
                    //Empty list (do not send anything)
                    return eventPayloadList;
                }
            }
            else if (!basicEventElementSourceString.IsNullOrEmpty())
            {
                AddStatus(basicEventElementSourceString, basicEventElementSemanticId, ref eventPayload);
                eventPayloadList.Add(eventPayload);
                return eventPayloadList;
            }


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
                var entryType = EventPayloadType.Updated;
                if (sm.TimeStampCreate > diffTime)
                {
                    entryType = EventPayloadType.Created;
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
                            .ToList();
                        if (smeSearchTimeStamp.Count != 0)
                        {
                            // smeSearchTimeStamp = smeSearchSM.Where(sme => sme.ParentSMEId == null).ToList();
                            var tree = CrudOperator.GetTree(db, sm, smeSearchTimeStamp);
                            var treeMerged = CrudOperator.GetSmeMerged(db, tree, sm);
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
                                    entryType = EventPayloadType.NotDeleted;
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
                                            entryType = EventPayloadType.Created;
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
                                            entryType = EventPayloadType.Updated;
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

                                var entry = new EventPayload(isREST);

                                if (showTransmitted)
                                {
                                    entry.transmitted = eventPayload.transmitted;
                                }
                                entry.domain = eventPayload.domain;


                                var sourceString = Program.externalBlazor + "/submodels/" + Base64UrlEncoder.Encode(sm.Identifier);
                                sourceString += "/submodel-elements/" + idShortPath;


                                entry.SetSubmodelType(entryType);
                                entry.SetTime(sme.TimeStampTree);

                                entry.source = sourceString;

                                entry.dataSchema = EventPayload.SCHEMA_URL + CrudOperator.GetModelType(sme.SMEType);

                                if (notDeletedIdShortList != null && notDeletedIdShortList.Count > 0)
                                {
                                    entry.notDeletedIdShortList = notDeletedIdShortList;
                                }

                                if (sm.SemanticId != null && !isREST)
                                {
                                    entry.semanticid = sme.SemanticId;
                                }

                                if (entryType != EventPayloadType.Deleted && withPayload)
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


                                eventPayloadList.Add(entry);

                                diffEntry.Add(entry.type + " " + entry.GetIdShortPath());
                                _logger.LogDebug($"Event {entry.type} Schema: {entry.dataSchema} idShortPath: {entry.GetIdShortPath()}");
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

                    var entry = new EventPayload(isREST);

                    if (showTransmitted)
                    {
                        entry.transmitted = eventPayload.transmitted;
                    }
                    entry.domain = eventPayload.domain;

                    entry.source = sourceString;
                    entry.SetSubmodelType(entryType);
                    entry.SetTime(sm.TimeStampTree);

                    entry.dataSchema = EventPayload.SCHEMA_URL + "submodel";

                    if (sm.SemanticId != null && !isREST)
                    {
                        entry.semanticid = sm.SemanticId;
                    }

                    if (isREST && sm.Identifier != null)
                    {
                        entry.subject = sm.Identifier;
                    }

                    if (withPayload)
                    {
                        var s = CrudOperator.ReadSubmodel(db, sm, loadIntoMemoryWithoutElements: true,
                            securitySqlConditions: securitySqlConditions,
                            skipAllowCheck: true);
                        if (s != null)
                        {
                            var j = Jsonization.Serialize.ToJsonObject(s);
                            if (j != null)
                            {
                                entry.data = j;
                            }
                        }
                    }

                    diffEntry.Add(entry.type + " " + entry.GetIdShortPath());
                    _logger.LogDebug($"Event {entry.type} Type: {entry.dataSchema} idShortPath: {entry.GetIdShortPath()}");

                    eventPayloadList.Add(entry);
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
                eventPayload.SetTime(timeStampMax);

                eventPayloadList.Add(eventPayload);
            }

            if (!isREST)
            {
                eventPayloadList[0].cursor = "0";
                eventPayloadList[0].id = CreateIdString(eventPayloadList[0]);

                if (basicEventElementSourceString.IsNullOrEmpty())
                {
                    //eventPayload.countSM = countSM;
                    //e.status.countSME = countSME;
                    if (countSM == limitSm)
                    {
                        eventPayload.cursor = $"offsetSM={offsetSm + limitSm}";
                    }
                    else
                    {
                        eventPayload.cursor = "0";
                    }
                }

                foreach (var ep in eventPayloadList)
                {
                    ep.cursor = eventPayload.cursor;
                    ep.id = CreateIdString(ep);
                }
            }

        }

        return eventPayloadList;
    }

    private void AddStatus(string basicEventElementSourceString, string basicEventElementSemanticId, ref EventPayload eventPayload)
    {
        if (!basicEventElementSourceString.IsNullOrEmpty())
        {
            eventPayload.dataSchema = "https://api.swaggerhub.com/domains/Plattform_i40/Part1-MetaModel-Schemas/V3.1.0#/components/schemas/BasicEventElement";
            eventPayload.id = $"{basicEventElementSourceString}-{eventPayload.time}";
            eventPayload.source = basicEventElementSourceString;
            eventPayload.cursor = "0";
            eventPayload.id = CreateIdString(eventPayload);

            eventPayload.SetSubmodelType(EventPayloadType.Updated);
            eventPayload.source = basicEventElementSourceString;
            eventPayload.semanticid = basicEventElementSemanticId;
        };
    }

    public int ChangeData(string json, EventDto eventData, AdminShellPackageEnv[] env, IReferable referable, out string transmit, out string lastDiffValue, out string statusValue, List<String> diffEntry, int packageIndex = -1)
    {
        transmit = "";
        lastDiffValue = "";
        statusValue = "ERROR";
        int count = 0;

        var statusData = eventData.StatusData;

        List<EventPayload> eventPayload = null;
        try
        {
            eventPayload = JsonSerializer.Deserialize<List<EventPayload>>(json);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex.ToString());
        }
        transmit = eventPayload[0].transmitted;
        var dt = TimeStamp.TimeStamp.StringToDateTime(eventPayload[0].time);
        dt = DateTime.Parse(eventPayload[0].time);
        var dtTransmit = DateTime.Parse(eventPayload[0].transmitted);
        lastDiffValue = TimeStamp.TimeStamp.DateTimeToString(dt);

        ISubmodelElementCollection statusDataCollection = null;
        if (eventPayload[0].statusData != null && statusData != null)
        {
            ISubmodelElement receiveSme = null;
            MemoryStream mStrm = new MemoryStream(Encoding.UTF8.GetBytes(eventPayload[0].statusData.ToJsonString()));
            JsonNode node = System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(mStrm).Result;
            receiveSme = Jsonization.Deserialize.ISubmodelElementFrom(node);
            if (receiveSme is AasCore.Aas3_1.SubmodelElementCollection smc)
            {
                statusData.Value = new List<ISubmodelElement>();
                statusData.Add(smc);
                receiveSme.TimeStampCreate = dtTransmit;
                receiveSme.TimeStampDelete = new DateTime();
                receiveSme.SetAllParentsAndTimestamps(statusData, dtTransmit, receiveSme.TimeStampCreate, receiveSme.TimeStampDelete);
                receiveSme.SetTimeStamp(dtTransmit);
            }
        }

        AasCore.Aas3_1.Environment aasEnv = null;
        int index = -1;
        ISubmodelElementCollection dataCollection = null;
        List<ISubmodelElement> data = new List<ISubmodelElement>();
        SubmodelElementCollection status = null;
        AasCore.Aas3_1.Property message = null;
        AasCore.Aas3_1.Property transmitted = null;
        AasCore.Aas3_1.Property lastUpdate = null;
        SubmodelElementCollection diff = null;

        // sort by submodelID + entryType + idShortPath by CompareTo(EventPayloadEntry)
        eventPayload.Sort();
        var entriesSubmodel = new List<EventPayload>();
        foreach (var entry in eventPayload)
        {
            _logger.LogDebug($"Event {entry.type} Type: {entry.dataSchema} idShortPath: {entry.GetIdShortPath()}");
            Submodel receiveSM = null;
            if (entry.dataSchema.Split("/")?.Last().ToLower() == "submodel")
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
                        if (entry.eventPayloadEntryType == EventPayloadType.Created)
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
                        visitor.update = entry.eventPayloadEntryType == EventPayloadType.Updated;
                        visitor.currentDataTime = dt;
                        visitor.VisitSubmodel(receiveSM);
                        db.Add(visitor._smDB);
                        db.SaveChanges();
                        count++;
                    }
                }
            }
            if (entry.dataSchema.Split("/")?.Last().ToLower() != "submodel")
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
                if (entry == eventPayload.Last())
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
                            var smeSmMerged = CrudOperator.GetSmeMerged(db, smeSmList, smDB);
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
                                    visitor.update = e.eventPayloadEntryType == EventPayloadType.Updated;
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
                                            case EventPayloadType.Created:
                                            case EventPayloadType.Updated:
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
                                    if (e.eventPayloadEntryType == EventPayloadType.Deleted)
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

    private int ChangeSubmodelElement(EventDto eventData, EventPayload entry, IReferable parent, List<ISubmodelElement> submodelElements, string idShortPath, List<String> diffEntry)
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

        if (entry.eventPayloadEntryType == EventPayloadType.Created)
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
                    _logger.LogDebug("Event CREATE SME: " + entry.GetIdShortPath());
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
                    diffEntry.Add(entry.type + " " + entry.GetIdShortPath());
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
        if (entry.eventPayloadEntryType == EventPayloadType.Updated)
        {
            for (int i = 0; i < submodelElements.Count; i++)
            {
                var sme = submodelElements[i];
                if (entry.GetIdShortPath() == idShortPath + sme.IdShort)
                {
                    _logger.LogDebug("Event UPDATE SME: " + entry.GetIdShortPath());
                    receiveSme.TimeStampCreate = submodelElements[i].TimeStampCreate;
                    receiveSme.TimeStampDelete = submodelElements[i].TimeStampDelete;
                    submodelElements[i] = receiveSme;
                    receiveSme.SetAllParentsAndTimestamps(parent, dt, receiveSme.TimeStampCreate, receiveSme.TimeStampDelete);
                    receiveSme.SetTimeStamp(dt);
                    diffEntry.Add(entry.type + " " + entry.GetIdShortPath());
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
        if (entry.eventPayloadEntryType == EventPayloadType.Deleted)
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
                        _logger.LogDebug("Event DELETE SME: " + entry.GetIdShortPath());
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
                        diffEntry.Add(entry.type + " " + entry.GetIdShortPath() + ".*");
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
        AasCore.Aas3_1.Property p = null;
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
            if (inputRef is AasCore.Aas3_1.Property)
            {
                p = (inputRef as AasCore.Aas3_1.Property);
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
                                _logger.LogDebug($"{p.Value} = {url}");
                                p.Value = url;
                            }
                            else
                            {
                                _logger.LogWarning($"Environment variable {envVarName} not found.");
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
                case "showtransmitted":
                    if (p != null)
                        eventDto.ShowTransmitted = p;
                    break;
            }
        }
        /*
        if (dataMaxSize == null)
        {
            var timeStamp = DateTime.UtcNow;
            dataMaxSize = new AasCore.Aas3_1.Property(DataTypeDefXsd.String, idShort: "dataMaxSize", value: "");
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
            if (outputRef is AasCore.Aas3_1.Property)
            {
                p = (outputRef as AasCore.Aas3_1.Property);
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
                        if (sme is AasCore.Aas3_1.Property)
                        {
                            eventDto.Message = sme as AasCore.Aas3_1.Property;
                        }
                        break;
                    case "transmitted":
                        if (sme is AasCore.Aas3_1.Property)
                        {
                            eventDto.Transmitted = sme as AasCore.Aas3_1.Property;
                        }
                        break;
                    case "lastupdate":
                        if (sme is AasCore.Aas3_1.Property)
                        {
                            eventDto.LastUpdate = sme as AasCore.Aas3_1.Property;
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
                        if (sme2 is AasCore.Aas3_1.Property)
                        {
                            eventDto.AuthType = sme2 as AasCore.Aas3_1.Property;
                        }

                        break;

                    case "accesstoken":
                        if (sme2 is AasCore.Aas3_1.Property)
                        {
                            eventDto.AccessToken = sme2 as AasCore.Aas3_1.Property;
                        }

                        break;

                    case "clienttoken":
                        if (sme2 is AasCore.Aas3_1.Property)
                        {
                            eventDto.ClientToken = sme2 as AasCore.Aas3_1.Property;
                        }

                        break;

                    case "username":
                        if (sme2 is AasCore.Aas3_1.Property)
                        {
                            eventDto.UserName = sme2 as AasCore.Aas3_1.Property;
                        }

                        break;

                    case "password":
                        if (sme2 is AasCore.Aas3_1.Property)
                        {
                            eventDto.PassWord = sme2 as AasCore.Aas3_1.Property;
                        }

                        break;

                    case "authservercertificate":
                        if (sme2 is AasCore.Aas3_1.File)
                        {
                            eventDto.AuthServerCertificate = sme2 as AasCore.Aas3_1.File;
                        }

                        break;

                    case "authserverendpoint":
                        if (sme2 is AasCore.Aas3_1.Property)
                        {
                            eventDto.AuthServerEndPoint = sme2 as AasCore.Aas3_1.Property;
                        }

                        break;

                    case "clientcertificate":
                        if (sme2 is AasCore.Aas3_1.File)
                        {
                            eventDto.ClientCertificate = sme2 as AasCore.Aas3_1.File;
                        }

                        break;

                    case "clientcertificatepassword":
                        if (sme2 is AasCore.Aas3_1.Property)
                        {
                            eventDto.ClientCertificatePassWord = sme2 as AasCore.Aas3_1.Property;
                        }

                        break;
                }
            }
        }
        return eventDto;
    }

    public async void NotifyDeleted(ISubmodel submodel, string idShortPath, string smeModelType, string smeSemanticId)
    {
        var outEventDtos = EventDtos.Where(eventData => eventData.Direction?.Value == "OUT");

        string searchSM = string.Empty;
        List<EventDto> notificationEventDtos = new List<EventDto>();

        foreach (var outEventDto in outEventDtos)
        {
            if (outEventDto.ConditionSM != null && outEventDto.ConditionSM.Value != null)
            {
                searchSM = outEventDto.ConditionSM.Value;

                var smList = new List<ISubmodel>() { submodel };

                if (searchSM is "(*)" or "*" or "")
                {
                    notificationEventDtos.Add(outEventDto);
                }
                else
                {
                    var queryable = smList.AsQueryable().Where(searchSM).ToList();

                    if (queryable.Any())
                    {
                        notificationEventDtos.Add(outEventDto);
                    }
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
            else
            {
                notificationEventDtos.Add(outEventDto);
            }
        }

        var mqttEventDtos = notificationEventDtos.Where(ne => ne.Mode.Value == "MQTT");

        if (mqttEventDtos.Any()
            && _enableMqtt)
        {
            var eventPayload = new EventPayload(false);
            List<String> diffEntry = new List<String>();


            foreach (var mqttEventDto in mqttEventDtos)
            {
                if (IsPublishMqttConfigured(mqttEventDto))
                {
                    if (mqttEventDto.ShowTransmitted != null
                        && mqttEventDto.ShowTransmitted.Value != null)
                    {
                        eventPayload.transmitted = TimeStamp.TimeStamp.DateTimeToString(DateTime.UtcNow);
                    }

                    if (mqttEventDto.Domain != null)
                    {
                        eventPayload.domain = mqttEventDto.Domain.Value;
                    }

                    eventPayload.SetTime(DateTime.UtcNow);

                    bool isPublishEventElement = mqttEventDto.PublishBasicEventElement != null
                        && mqttEventDto.PublishBasicEventElement.Value != null && mqttEventDto.PublishBasicEventElement.Value == "true";

                    if (isPublishEventElement)
                    {
                        if (mqttEventDto.IdShort != null)
                        {
                            eventPayload.source = $"{Program.externalBlazor}/submodels/{Base64UrlEncoder.Encode(mqttEventDto.SubmodelId)}/events/{mqttEventDto.IdShortPath}.{mqttEventDto.IdShort}";
                        }
                        eventPayload.semanticid = (mqttEventDto.SemanticId != null && mqttEventDto.SemanticId?.Keys != null) ? mqttEventDto.SemanticId?.Keys[0].Value : "";
                        eventPayload.dataSchema = "https://api.swaggerhub.com/domains/Plattform_i40/Part1-MetaModel-Schemas/V3.1.0#/components/schemas/BasicEventElement";

                    }
                    else
                    {
                        var submmodelsOnly = mqttEventDto.SubmodelsOnly != null
                            && mqttEventDto.SubmodelsOnly.Value != null && mqttEventDto.SubmodelsOnly.Value == "true";
                        var sourceString = Program.externalBlazor + "/submodels/" + Base64UrlEncoder.Encode(submodel.Id);

                        if (submmodelsOnly
                            && !idShortPath.IsNullOrEmpty())
                        {
                            sourceString += "/submodel-elements/" + idShortPath;
                            eventPayload.semanticid = smeSemanticId;
                            eventPayload.dataSchema = EventPayload.SCHEMA_URL + smeModelType;
                        }
                        else
                        {
                            eventPayload.dataSchema = EventPayload.SCHEMA_URL + "submodel";

                            if (submodel.SemanticId != null)
                            {
                                eventPayload.semanticid = (submodel.SemanticId != null && submodel.SemanticId?.Keys != null) ? submodel.SemanticId?.Keys[0].Value : "";
                            }

                        }
                        eventPayload.source = sourceString;

                        //ToDo: Check diff entry
                        diffEntry.Add(eventPayload.type + " " + eventPayload.source);
                    }


                    eventPayload.id = $"{eventPayload.source}-{eventPayload.time}";
                    eventPayload.SetSubmodelType(EventPayloadType.Deleted);

                    var options = new JsonSerializerOptions
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    };

                    var payloadObjString = JsonSerializer.Serialize(eventPayload, options);

                    try
                    {
                        var clientId = Program.externalBlazor + "/submodels/" + Base64UrlEncoder.Encode(mqttEventDto.SubmodelId);

                        if (!mqttEventDto.IdShortPath.IsNullOrEmpty())
                        {
                            clientId += $"/submodel-elements/{mqttEventDto.IdShortPath}.{mqttEventDto.IdShort}";
                        }

                        var result = await _mqttClientService.PublishAsync(clientId, mqttEventDto.MessageBroker.Value,
                        mqttEventDto.MessageTopicType.Value,
                            mqttEventDto?.UserName?.Value, mqttEventDto?.PassWord?.Value, mqttEventDto?.AccessToken?.Value, payloadObjString);

                        var now = DateTime.UtcNow;

                        bool isSucceeded = false;

                        if (result != null && result.IsSuccess)
                        {
                            isSucceeded = true;
                            _logger.LogDebug("MQTT message sent.");
                        }

                        if (isSucceeded)
                        {
                            if (mqttEventDto.Transmitted != null)
                            {
                                mqttEventDto.Transmitted.Value = eventPayload.transmitted;
                                mqttEventDto.Transmitted.SetTimeStamp(now);
                            }
                            var dt = DateTime.Parse(eventPayload.time);
                            if (mqttEventDto.LastUpdate != null)
                            {
                                mqttEventDto.LastUpdate.Value = eventPayload.time;
                                mqttEventDto.LastUpdate.SetTimeStamp(dt);
                            }
                            if (mqttEventDto.Status != null)
                            {
                                if (mqttEventDto.Message != null)
                                {
                                    //ToDo: Is message really correct? 
                                    mqttEventDto.Message.Value = "on";
                                }
                                mqttEventDto.Status.SetTimeStamp(now);
                            }
                            if (mqttEventDto.Diff != null && diffEntry.Count > 0)
                            {
                                mqttEventDto.Diff.Value = new List<ISubmodelElement>();
                                int i = 0;
                                foreach (var dif in diffEntry)
                                {
                                    var p = new Property(DataTypeDefXsd.String);
                                    p.IdShort = "diff" + i;
                                    p.Value = dif;
                                    p.SetTimeStamp(dt);
                                    mqttEventDto.Diff.Value.Add(p);
                                    p.SetAllParentsAndTimestamps(mqttEventDto.Diff, dt, dt, DateTime.MinValue);
                                    i++;
                                }
                                mqttEventDto.Diff.SetTimeStamp(dt);
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

                            if (mqttEventDto.Status != null
                                && mqttEventDto.Message != null)
                            {
                                mqttEventDto.Message.Value = "ERROR: " +
                                    statusCode + " ; " +
                                    " ; PUT " + mqttEventDto.MessageBroker.Value;
                                mqttEventDto.Status.SetTimeStamp(now);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (mqttEventDto.Message != null)
                        {
                            mqttEventDto.Message.Value = "ERROR: " +
                                ex.Message +
                                " ; PUT " + mqttEventDto.MessageBroker.Value;
                        }
                        var now = DateTime.UtcNow;
                        mqttEventDto.Status.SetTimeStamp(now);
                        // d = eventData.LastUpdate.Value = "reconnect";
                    }
                }

                mqttEventDto.env.setWrite(true);
            }
        }

        var restEventDtos = notificationEventDtos.Where(ne => ne.Mode.Value == "REST_API");

        if (smeModelType.IsNullOrEmpty())
        {
            if (restEventDtos.Any())
            {
                foreach (var restEventDto in restEventDtos.Where(IsPublishRestApiConfigured))
                {
                    var eventPayload = new EventPayload(true);

                    var sourceString = Program.externalBlazor + "/submodels/" + Base64UrlEncoder.Encode(submodel.Id);

                    eventPayload.source = sourceString;
                    eventPayload.SetSubmodelType(EventPayloadType.Deleted);
                    eventPayload.SetTime(submodel.TimeStampTree);

                    eventPayload.dataSchema = EventPayload.REST_API_SM_SCHEMA_URL;

                    if (submodel.SemanticId != null)
                    {
                        eventPayload.semanticid = (submodel.SemanticId != null && submodel.SemanticId?.Keys != null) ? submodel.SemanticId?.Keys[0].Value : "";
                    }

                    eventPayload.subject = submodel.Id;

                    bool wp = false;

                    //ToDo: Is empty data in Events Feed API spec allowed?
                    //if (eventData.Include != null
                    //    && eventData.Include.Value != null)
                    //{
                    //    wp = eventData.Include.Value.ToLower() == "true";
                    //}

                    wp = true;

                    if (wp)
                    {
                        var j = Jsonization.Serialize.ToJsonObject(submodel);
                        if (j != null)
                        {
                            eventPayload.data = ConvertSmJsonToRestApiSpecSmJson(j);
                        }
                    }

                    var options = new JsonSerializerOptions
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    };
                    _logger.LogDebug($"Event id: {eventPayload.id}, Type: {eventPayload.type}");

                    var now = DateTime.UtcNow;

                    var payloadObjString = JsonSerializer.Serialize(new List<EventPayload> { eventPayload }, options);

                    try
                    {
                        using var handler = new HttpClientHandler()
                        {
                            Proxy = HttpClient.DefaultProxy,
                            DefaultProxyCredentials = CredentialCache.DefaultCredentials
                        };

                        using var client = new HttpClient(handler);

                        string user = "John Doe";
                        string password = null;

                        if (restEventDto.AccessToken != null && restEventDto.AccessToken.Value != null && restEventDto.AccessToken.Value != "")
                        {
                            client.SetBearerToken(restEventDto.AccessToken.Value);
                        }
                        else
                        {
                            if (restEventDto.UserName != null && restEventDto.PassWord != null)
                            {
                                user = restEventDto.UserName.Value;
                                password = restEventDto.PassWord.Value;
                            }
                        }

                        string requestPath = $"{restEventDto.EndPoint.Value}?user={user}";

                        using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestPath))
                        {
                            var content = new StringContent(payloadObjString, System.Text.Encoding.UTF8, "application/json");
                            requestMessage.Content = content;

                            if (!user.IsNullOrEmpty()
                                 && !password.IsNullOrEmpty())
                            {
                                requestMessage.Headers.Authorization = new BasicAuthenticationHeaderValue(user, password);
                            }

                            client.DefaultRequestHeaders.Add("user", user);

                            HttpResponseMessage response = null;
                            var task = Task.Run(async () =>
                            {
                                response = await client.SendAsync(requestMessage);

                                var now = DateTime.UtcNow;
                                if (!response.IsSuccessStatusCode)
                                {
                                    if (restEventDto.Status != null)
                                    {
                                        if (restEventDto.Message != null)
                                        {
                                            restEventDto.Message.Value = "ERROR: " +
                                                response.StatusCode + " ; " +
                                                response.Content.ReadAsStringAsync().Result +
                                                " ; PUT " + requestPath;
                                        }
                                        restEventDto.Status.SetTimeStamp(now);
                                        // d = restEventDto.LastUpdate.Value = "reconnect";
                                        restEventDto.LastUpdate.SetTimeStamp(now);
                                    }
                                }
                                else
                                {
                                    if (restEventDto.Transmitted != null)
                                    {
                                        restEventDto.Transmitted.Value = eventPayload.transmitted;
                                        restEventDto.Transmitted.SetTimeStamp(now);
                                    }
                                    var maxTime = eventPayload.time;
                                    var dt = DateTime.ParseExact(maxTime, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);

                                    if (restEventDto.LastUpdate != null)
                                    {
                                        restEventDto.LastUpdate.Value = maxTime;
                                        restEventDto.LastUpdate.SetTimeStamp(dt);
                                    }
                                }
                            });
                            task.Wait();
                        }
                    }
                    catch (Exception ex)
                    {
                        if (restEventDto.Message != null)
                        {
                            restEventDto.Message.Value = "ERROR: " +
                                ex.Message +
                                " ; PUT " + restEventDto.EndPoint.Value;
                        }
                        restEventDto.Status.SetTimeStamp(now);
                    }

                    restEventDto.env.setWrite(true);
                }
            }
        }
    }
}


