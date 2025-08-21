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

namespace Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AasCore.Aas3_0;
using AdminShellNS;
using Contracts.Events;

public interface IEventService
{
    public EventDto TryAddDto(EventDto eventDto);

    public event EventHandler? CalculateCfpRequestReceived;

    public void RegisterMqttMessage(EventDto eventData, string submodelId, string idShortPath);
    void CheckMqttMessages(EventDto eventData, string submodelId, string idShortPath);

    public void PublishMqttMessage(EventDto eventDto, string submodelId, string idShortPath);

    public Events.EventPayload CollectPayload(Dictionary<string, string> securityCondition, string changes, int depth, SubmodelElementCollection statusData,
        ReferenceElement reference, IReferable referable, AasCore.Aas3_0.Property conditionSM, AasCore.Aas3_0.Property conditionSME,
        string diff, List<String> diffEntry, bool withPayload, bool smOnly, int limitSm, int limitSme, int offsetSm, int offsetSme);

    public int ChangeData(string json, EventDto eventData, AdminShellPackageEnv[] env, IReferable referable, out string transmit, out string lastDiffValue, out string statusValue, List<String> diffEntry, int packageIndex = -1);

    public Operation FindEvent(ISubmodel submodel, string eventPath);

    public EventDto ParseData(Operation op, AdminShellPackageEnv env);
}
