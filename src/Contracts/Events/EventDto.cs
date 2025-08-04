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
using AasCore.Aas3_0;

public class EventDto
{
    public SubmodelElementCollection Authentication = null;
    public AasCore.Aas3_0.Property AuthType = null;
    public AasCore.Aas3_0.Property AuthServerEndPoint = null;
    public AasCore.Aas3_0.Property AccessToken = null;
    public AasCore.Aas3_0.Property UserName = null;
    public AasCore.Aas3_0.Property PassWord = null;
    public string BasicAuth = null;
    public AasCore.Aas3_0.File AuthServerCertificate = null;
    public AasCore.Aas3_0.File ClientCertificate = null;
    public AasCore.Aas3_0.Property ClientCertificatePassWord = null;
    public AasCore.Aas3_0.Property ClientToken = null;

    public AasCore.Aas3_0.Property Direction = null;
    public AasCore.Aas3_0.Property Mode = null;
    public AasCore.Aas3_0.Property Changes = null;
    public AasCore.Aas3_0.Property EndPoint = null;
    public Submodel DataSubmodel = null;
    public ISubmodelElement DataCollection = null;
    public AasCore.Aas3_0.Property DataMaxSize = null;
    public SubmodelElementCollection StatusData = null;
    public AasCore.Aas3_0.Property NoPayload = null;

    // memory || database
    public AasCore.Aas3_0.Property Persistence = null;
    // * = all SM, else query condition for SM
    public AasCore.Aas3_0.Property ConditionSM = null;
    // "" only conditionSM, * = all SME in SM, else query condition for SME
    public AasCore.Aas3_0.Property ConditionSME = null;
    public AasCore.Aas3_0.ReferenceElement DataReference = null;

    public SubmodelElementCollection Status = null;
    public AasCore.Aas3_0.Property Message = null;
    public AasCore.Aas3_0.Property Transmitted = null;
    public AasCore.Aas3_0.Property LastUpdate = null;
    public SubmodelElementCollection Diff = null;

    //public string SubmodelId { get; set; }
    //public string IdShortPath { get; set; } 
}

