using AdminShellNS.Lib.V3.Models;
using DataTransferObjects;
using DataTransferObjects.ValueDTOs;
using IO.Swagger.Lib.V3.SerializationModifiers;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;
using IO.Swagger.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace IO.Swagger.Lib.V3.Formatters
{
    public class AasResponseFormatter : OutputFormatter
    {
        public AasResponseFormatter()
        {
            SupportedMediaTypes.Clear();
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
        }
        public static bool IsGenericListOfIClass(object o)
        {
            var oType = o.GetType();
            return oType.IsGenericType &&
                (oType.GetGenericTypeDefinition() == typeof(List<>) &&
                (typeof(IClass).IsAssignableFrom(oType.GetGenericArguments()[0])));
        }

        public static bool IsGenericListOfIValueDTO(object o)
        {
            var oType = o.GetType();
            bool IsValueDTO = false;
            if (o is List<IDTO> list)
            {
                if (list[0] is IValueDTO)
                {
                    IsValueDTO = true;
                }
            }
            return oType.IsGenericType &&
                (oType.GetGenericTypeDefinition() == typeof(List<>) &&
                IsValueDTO);
        }

        public override bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            if (context.Object is List<string>)
            {
                return true;
            }
            if (typeof(IClass).IsAssignableFrom(context.ObjectType))
            {
                return base.CanWriteResult(context);
            }
            if (typeof(ValueOnlyPagedResult).IsAssignableFrom(context.ObjectType))
            {
                return base.CanWriteResult(context);
            }
            if (typeof(IValueDTO).IsAssignableFrom(context.ObjectType))
            {
                return base.CanWriteResult(context);
            }
            if (IsGenericListOfIClass(context.Object))
            {
                return base.CanWriteResult(context);
            }
            if (IsGenericListOfIValueDTO(context.Object))
            {
                return base.CanWriteResult(context);
            }
            if (typeof(PagedResult).IsAssignableFrom(context.ObjectType))
            {
                return base.CanWriteResult(context);
            }
            else
                return false;
        }
        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            var httpContext = context.HttpContext;
            var response = context.HttpContext.Response;

            //SerializationModifier
            GetSerializationMidifiersFromRequest(context.HttpContext.Request, out LevelEnum level, out ExtentEnum extent);

            if (context.Object is List<string> s)
            {
                /*
                string json = JsonSerializer.Serialize(context.Object);
                response.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
                response.ContentType = "text/plain";

                using (var writer = new StreamWriter(response.Body))
                {
                    writer.WriteAsync(json);
                    writer.FlushAsync(); // Ensure the response is flushed
                }
                */

                var jsonResult = new JsonResult(context.Object)
                {
                    StatusCode = StatusCodes.Status200OK,
                    ContentType = "application/json"
                };

                jsonResult.ExecuteResultAsync(new ActionContext
                {
                    HttpContext = httpContext,
                    RouteData = httpContext.GetRouteData(),
                    ActionDescriptor = new ActionDescriptor()
                });
            }
            else if (typeof(IClass).IsAssignableFrom(context.ObjectType))
            {
                //Validate modifiers
                SerializationModifiersValidator.Validate((IClass)context.Object, level, extent);

                JsonObject json = Jsonization.Serialize.ToJsonObject((IClass)context.Object);
                var writer = new Utf8JsonWriter(response.Body);
                json.WriteTo(writer);
                writer.FlushAsync().GetAwaiter().GetResult();
            }
            else if (IsGenericListOfIClass(context.Object))
            {

                var           jsonArray         = new JsonArray();
                IList         genericList       = (IList)context.Object;
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
            else if (typeof(ValueOnlyPagedResult).IsAssignableFrom(context.ObjectType))
            {
                var jsonArray = new JsonArray();
                string cursor = null;
                if (context.Object is ValueOnlyPagedResult pagedResult)
                {
                    cursor = pagedResult.paging_metadata.cursor;
                    foreach (var item in pagedResult.result)
                    {
                        var json = ValueOnlyJsonSerializer.ToJsonObject(item);
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
            else if (typeof(IValueDTO).IsAssignableFrom(context.ObjectType))
            {
                JsonNode? json = ValueOnlyJsonSerializer.ToJsonObject((IValueDTO)context.Object);
                var writer = new Utf8JsonWriter(response.Body);
                json.WriteTo(writer);
                writer.FlushAsync().GetAwaiter().GetResult();
            }
            else if (IsGenericListOfIValueDTO(context.Object))
            {
                var jsonArray = new JsonArray();
                IList genericList = (IList)context.Object;
                List<IValueDTO> contextObjectType = new List<IValueDTO>();
                foreach (var generic in genericList)
                {
                    contextObjectType.Add((IValueDTO)generic);
                }

                foreach (var item in contextObjectType)
                {
                    var json = ValueOnlyJsonSerializer.ToJsonObject(item);
                    jsonArray.Add(json);
                }
                var writer = new Utf8JsonWriter(response.Body);
                jsonArray.WriteTo(writer);
                writer.FlushAsync().GetAwaiter().GetResult();
            }
            else if (typeof(PagedResult).IsAssignableFrom(context.ObjectType))
            {
                var jsonArray = new JsonArray();
                string cursor = null;
                if (context.Object is PagedResult pagedResult)
                {
                    cursor = pagedResult.paging_metadata.cursor;
                    foreach (var item in pagedResult.result)
                    {
                        var json = Jsonization.Serialize.ToJsonObject(item);
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

            return Task.FromResult(response);
        }

        private void GetSerializationMidifiersFromRequest(HttpRequest request, out LevelEnum level, out ExtentEnum extent)
        {
            request.Query.TryGetValue("level", out StringValues levelValues);
            if (levelValues.Any())
            {
                Enum.TryParse(levelValues.First(), out level);
            }
            else
            {
                level = LevelEnum.Deep;
            }

            request.Query.TryGetValue("extent", out StringValues extenValues);
            if (extenValues.Any())
            {
                Enum.TryParse(extenValues.First(), out extent);
            }
            else
            {
                extent = ExtentEnum.WithoutBlobValue;
            }
        }
    }
}
