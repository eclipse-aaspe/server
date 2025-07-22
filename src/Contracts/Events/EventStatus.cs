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

public class EventStatus
{
    // public string Mode { get; set; } // PULL or PUSH must be the same in publisher and consumer
    public string transmitted { get; set; } // timestamp of GET or PUT
    public string lastUpdate { get; set; } // latest timeStamp for all entries
    public int countSM { get; set; }
    public int countSME { get; set; }
    public string cursor { get; set; }
    public EventStatus()
    {
        // Mode = "";
        transmitted = "";
        lastUpdate = "";
        countSM = 0;
        countSME = 0;
        cursor = "";
    }
}
