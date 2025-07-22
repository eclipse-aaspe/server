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

namespace IO.Swagger.Lib.V3.GraphQL;

using HotChocolate.Language;
using HotChocolate.Resolvers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class ParameterNamesMiddleware
{
    private readonly FieldDelegate _next;

    public ParameterNamesMiddleware(FieldDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(IMiddlewareContext context)
    {
        var parameterNames = new List<string>();

        if (context.Selection.SelectionSet != null)
        {
            parameterNames = context.Selection.SelectionSet.Selections
                .OfType<FieldNode>()
                .Select(subField => subField.Name.Value)
                .ToList();
        }

        context.ContextData["ParameterNames"] = parameterNames;

        await _next(context);
    }
}

