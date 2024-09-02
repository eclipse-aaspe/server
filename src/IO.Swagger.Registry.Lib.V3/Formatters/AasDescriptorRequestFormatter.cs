/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
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

using IO.Swagger.Registry.Lib.V3.Models;
using IO.Swagger.Registry.Lib.V3.Serializers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace IO.Swagger.Registry.Lib.V3.Formatters
{
    public class AasDescriptorRequestFormatter : InputFormatter
    {
        public AasDescriptorRequestFormatter()
        {
            this.SupportedMediaTypes.Clear();
            this.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
        }

        public override bool CanRead(InputFormatterContext context)
        {
            if (typeof(AssetAdministrationShellDescriptor).IsAssignableFrom(context.ModelType))
            {
                return true;
            }
            else if (typeof(List<AssetAdministrationShellDescriptor>).IsAssignableFrom(context.ModelType))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            Type type = context.ModelType;
            var request = context.HttpContext.Request;
            object result = null;


            JsonNode node = System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(request.Body).Result;
            if (type == typeof(AssetAdministrationShellDescriptor))
            {
                result = DescriptorDeserializer.AssetAdministrationShellDescriptorFrom(node);
            }
            else if (type == typeof(List<AssetAdministrationShellDescriptor>))
            {
                var aasDescList = new List<AssetAdministrationShellDescriptor>();
                var jsonArray = node as JsonArray;
                foreach (var item in jsonArray)
                {
                    aasDescList.Add(DescriptorDeserializer.AssetAdministrationShellDescriptorFrom(item));
                }

                result = aasDescList;
            }
            else
            {
                throw new NotSupportedException();
            }

            return InputFormatterResult.SuccessAsync(result);
        }
    }
}