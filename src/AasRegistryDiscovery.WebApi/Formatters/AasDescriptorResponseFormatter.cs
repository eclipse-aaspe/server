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

using AasRegistryDiscovery.WebApi.Models;
using AasRegistryDiscovery.WebApi.Serializers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace AasRegistryDiscovery.WebApi.Formatters
{
    public class AasDescriptorResponseFormatter : OutputFormatter
    {
        public AasDescriptorResponseFormatter()
        {
            this.SupportedMediaTypes.Clear();
            this.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
        }

        public static bool IsGenericListOfAasDesc(object o)
        {
            var oType = o.GetType();
            return oType.IsGenericType &&
                   (oType.GetGenericTypeDefinition() == typeof(List<>) &&
                    (typeof(AssetAdministrationShellDescriptor).IsAssignableFrom(oType.GetGenericArguments()[ 0 ])));
        }

        public static bool IsGenericListOfIClass(object o)
        {
            var oType = o.GetType();
            return oType.IsGenericType &&
                   (oType.GetGenericTypeDefinition() == typeof(List<>) &&
                    (typeof(IClass).IsAssignableFrom(oType.GetGenericArguments()[ 0 ])));
        }

        public override bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            if (typeof(AssetAdministrationShellDescriptor).IsAssignableFrom(context.ObjectType))
            {
                return base.CanWriteResult(context);
            }
            else if (typeof(SubmodelDescriptor).IsAssignableFrom(context.ObjectType))
            {
                return base.CanWriteResult(context);
            }
            else if (IsGenericListOfAasDesc(context.Object))
            {
                //return base.CanWriteResult(context);
                return true;
            }
            if (IsGenericListOfIClass(context.Object))
            {
                return base.CanWriteResult(context);
            }
            else if (typeof(AasDescriptorPagedResult).IsAssignableFrom(context.ObjectType))
            {
                return base.CanWriteResult(context);
            }
            else if (typeof(SubmodelDescriptorPagedResult).IsAssignableFrom(context.ObjectType))
            {
                return base.CanWriteResult(context);
            }
            else if(typeof(GetAllAssetAdministrationShellIdsResult).IsAssignableFrom(context.ObjectType))
            {
                return base.CanWriteResult(context);
            }
            else if (typeof(ServiceDescription).IsAssignableFrom(context.ObjectType))
            {
                return true;
            }
            else if(typeof(ValidationProblemDetails).IsAssignableFrom(context.ObjectType))
            {
                return true;
            }
            else
                return false;
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            var response = context.HttpContext.Response;

            if (typeof(AssetAdministrationShellDescriptor).IsAssignableFrom(context.ObjectType))
            {
                JsonObject? json = DescriptorSerializer.ToJsonObject(context.Object);
                var writer = new Utf8JsonWriter(response.Body);
                json.WriteTo(writer);
                writer.FlushAsync().GetAwaiter().GetResult();
            }
            else if (typeof(SubmodelDescriptor).IsAssignableFrom(context.ObjectType))
            {
                JsonObject? json = DescriptorSerializer.ToJsonObject(context.Object);
                var writer = new Utf8JsonWriter(response.Body);
                json.WriteTo(writer);
                writer.FlushAsync().GetAwaiter().GetResult();
            }
            else if (typeof(AasDescriptorPagedResult).IsAssignableFrom(context.ObjectType))
            {
                var jsonArray = new JsonArray();
                string cursor = null;
                if (context.Object is AasDescriptorPagedResult pagedResult)
                {
                    cursor = pagedResult.paging_metadata.Cursor;
                    foreach (var item in pagedResult.result)
                    {
                        var json = DescriptorSerializer.ToJsonObject(item);
                        jsonArray.Add(json);
                    }
                }

                JsonObject jsonNode = new JsonObject();
                jsonNode[ "result" ] = jsonArray;
                var pagingMetadata = new JsonObject();
                if (cursor != null)
                {
                    pagingMetadata[ "cursor" ] = cursor;
                }

                jsonNode[ "paging_metadata" ] = pagingMetadata;
                var writer = new Utf8JsonWriter(response.Body);
                jsonNode.WriteTo(writer);
                writer.FlushAsync().GetAwaiter().GetResult();
            }
            else if (typeof(SubmodelDescriptorPagedResult).IsAssignableFrom(context.ObjectType))
            {
                var jsonArray = new JsonArray();
                string cursor = null;
                if (context.Object is SubmodelDescriptorPagedResult pagedResult)
                {
                    cursor = pagedResult.paging_metadata.Cursor;
                    foreach (var item in pagedResult.result)
                    {
                        var json = DescriptorSerializer.ToJsonObject(item);
                        jsonArray.Add(json);
                    }
                }

                JsonObject jsonNode = new JsonObject();
                jsonNode["result"] = jsonArray;
                var pagingMetadata = new JsonObject();
                if (cursor != null)
                {
                    pagingMetadata["cursor"] = cursor;
                }

                jsonNode["paging_metadata"] = pagingMetadata;
                var writer = new Utf8JsonWriter(response.Body);
                jsonNode.WriteTo(writer);
                writer.FlushAsync().GetAwaiter().GetResult();
            }
            else if (IsGenericListOfAasDesc(context.Object))
            {
                var jsonArray = new JsonArray();
                IList genericList = (IList) context.Object;
                List<AssetAdministrationShellDescriptor> contextObjectType = new List<AssetAdministrationShellDescriptor>();
                foreach (var generic in genericList)
                {
                    contextObjectType.Add((AssetAdministrationShellDescriptor) generic);
                }

                foreach (var item in contextObjectType)
                {
                    var json = DescriptorSerializer.ToJsonObject(item);
                    jsonArray.Add(json);
                }

                var writer = new Utf8JsonWriter(response.Body);
                jsonArray.WriteTo(writer);
                writer.FlushAsync().GetAwaiter().GetResult();
            }
            else if (IsGenericListOfIClass(context.Object))
            {

                var jsonArray = new JsonArray();
                IList genericList = (IList)context.Object;
                List<IClass?> contextObjectType = new List<IClass?>();
                foreach (var generic in genericList)
                {
                    contextObjectType.Add((IClass)generic);
                }

                foreach (var item in contextObjectType)
                {
                    var json = Jsonization.Serialize.ToJsonObject(item);
                    jsonArray.Add(json);
                }
                var writer = new Utf8JsonWriter(response.Body);
                jsonArray.WriteTo(writer);
                writer.FlushAsync().GetAwaiter().GetResult();
            }
            else if (typeof(GetAllAssetAdministrationShellIdsResult).IsAssignableFrom(context.ObjectType))
            {
                var jsonArray = new JsonArray();
                string cursor = null;
                if (context.Object is GetAllAssetAdministrationShellIdsResult pagedResult)
                {
                    cursor = pagedResult.paging_metadata.Cursor;
                    foreach (var item in pagedResult.result)
                    {
                        jsonArray.Add(item);
                    }
                }

                JsonObject jsonNode = new JsonObject();
                jsonNode["result"] = jsonArray;
                var pagingMetadata = new JsonObject();
                if (cursor != null)
                {
                    pagingMetadata["cursor"] = cursor;
                }

                jsonNode["paging_metadata"] = pagingMetadata;
                var writer = new Utf8JsonWriter(response.Body);
                jsonNode.WriteTo(writer);
                writer.FlushAsync().GetAwaiter().GetResult();
            }
            else if (typeof(ServiceDescription).IsAssignableFrom(context.ObjectType))
            {
                if (context.Object is ServiceDescription serviceDescription)
                {
                    var jsonObject = new JsonObject();
                    jsonObject["profiles"] = JsonSerializer.SerializeToNode(serviceDescription.Profiles);
                    var writer = new Utf8JsonWriter(response.Body);
                    jsonObject.WriteTo(writer);
                    writer.FlushAsync().GetAwaiter().GetResult(); 
                }
            }
            else if(typeof(ValidationProblemDetails).IsAssignableFrom(context.ObjectType))
            {
                var problem = context.Object;
            }
            else
            {
                throw new NotSupportedException();
            }

            return Task.FromResult(response);
        }
    }
}