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

namespace Contracts.DbRequests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasCore.Aas3_0;

public class DbRequest
{
    public DbRequest(DbRequestOp dbRequestOp, DbRequestCrudType crudType, DbRequestContext dbRequestContext, TaskCompletionSource<DbRequestResult> completionSource)
    {
        Operation = dbRequestOp;
        Context = dbRequestContext;
        TaskCompletionSource = completionSource;
        CrudType = crudType;
    }

    public DbRequestCrudType CrudType { get; set; }

    public DbRequestOp Operation { get; set; }

    public DbRequestContext Context { get; set; }

    public TaskCompletionSource<DbRequestResult> TaskCompletionSource { get; set; }
}

