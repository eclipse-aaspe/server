using AdminShellNS.Lib.V3.Models;
using DataTransferObjects;
using IO.Swagger.Lib.V3.SerializationModifiers;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;
using IO.Swagger.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using IValueDTO = DataTransferObjects.ValueDTOs.IValueDTO;

namespace IO.Swagger.Lib.V3.Formatters
{
    public class AasResponseFormatter : OutputFormatter
    {
        public AasResponseFormatter()
        {
            this.SupportedMediaTypes.Clear();
            this.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
        }

        private static bool IsGenericListOfIClass(object o)
        {
            var oType = o.GetType();
            return oType.IsGenericType &&
                   (oType.GetGenericTypeDefinition() == typeof(List<>) &&
                    (typeof(IClass).IsAssignableFrom(oType.GetGenericArguments()[0])));
        }

        private static bool IsGenericListOfIValueDTO(object o)
        {
            var oType = o.GetType();
            var IsValueDTO = false;
            if (o is not List<IDTO> list)
                return false;
            if (list[0] is IValueDTO)
            {
                IsValueDTO = true;
            }

            return oType.IsGenericType &&
                   (oType.GetGenericTypeDefinition() == typeof(List<>) &&
                    IsValueDTO);
        }

        public override bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            var objectType = context.ObjectType;
            var obj = context.Object;

            return typeof(IClass).IsAssignableFrom(objectType) ||
                   typeof(ValueOnlyPagedResult).IsAssignableFrom(objectType) ||
                   typeof(IValueDTO).IsAssignableFrom(objectType) ||
                   IsGenericListOfIClass(obj) ||
                   IsGenericListOfIValueDTO(obj) ||
                   typeof(PagedResult).IsAssignableFrom(objectType);
        }


        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            var response = context.HttpContext.Response;

            GetSerializationModifiersFromRequest(context.HttpContext.Request, out var level, out var extent);

            if (TryWriteJsonObject(response.Body, context.Object, level, extent))
            {
                await response.Body.FlushAsync();
            }
        }


        private bool TryWriteJsonObject(Stream responseBody, object obj, LevelEnum level, ExtentEnum extent)
        {
            var writer = new Utf8JsonWriter(responseBody);

            switch (obj)
            {
                case IClass classObj:
                    SerializationModifiersValidator.Validate(classObj, level, extent);
                    Jsonization.Serialize.ToJsonObject(classObj).WriteTo(writer);
                    break;
                case IList<IClass> genericListOfClass:
                    WriteJsonArray(writer, genericListOfClass.Select(Jsonization.Serialize.ToJsonObject));
                    break;
                case ValueOnlyPagedResult valuePagedResult:
                    WriteValueOnlyPagedResult2(writer, valuePagedResult);
                    break;
                case IValueDTO valueDto:
                    new ValueOnlyJsonSerializer().ToJsonObject(valueDto).WriteTo(writer);
                    break;
                case IList<IValueDTO> genericListOfValueDto:
                    WriteJsonArray(writer, genericListOfValueDto.Select(item => new ValueOnlyJsonSerializer().ToJsonObject(item)));
                    break;
                case PagedResult pagedResult:
                    WritePagedResult2(writer, pagedResult);
                    break;
                default:
                    return false;
            }

            writer.Flush();
            return true;
        }

        private void WriteJsonArray(Utf8JsonWriter writer, IEnumerable<JsonNode> nodes)
        {
            var jsonArray = new JsonArray();
            foreach (var node in nodes)
            {
                jsonArray.Add(node);
            }

            jsonArray.WriteTo(writer);
        }


        private void WriteValueOnlyPagedResult2(Utf8JsonWriter writer, ValueOnlyPagedResult valuePagedResult)
        {
            var jsonArray = new JsonArray();
            string cursor = valuePagedResult.paging_metadata?.cursor;

            foreach (var json in valuePagedResult.result.Select(item => new ValueOnlyJsonSerializer().ToJsonObject(item)))
            {
                jsonArray.Add(json);
            }

            var jsonNode = new JsonObject
            {
                ["result"] = jsonArray
            };

            if (cursor != null)
            {
                jsonNode["paging_metadata"] = new JsonObject {["cursor"] = cursor};
            }

            jsonNode.WriteTo(writer);
        }

        private void WritePagedResult2(Utf8JsonWriter writer, PagedResult pagedResult)
        {
            var jsonArray = new JsonArray();
            string cursor = pagedResult.paging_metadata?.cursor;

            foreach (var json in pagedResult.result.Select(Jsonization.Serialize.ToJsonObject))
            {
                jsonArray.Add(json);
            }

            var jsonNode = new JsonObject
            {
                ["result"] = jsonArray
            };

            if (cursor != null)
            {
                jsonNode["paging_metadata"] = new JsonObject {["cursor"] = cursor};
            }

            jsonNode.WriteTo(writer);
        }


        private void WriteValueOnlyPagedResult(Utf8JsonWriter writer, ValueOnlyPagedResult valuePagedResult)
        {
            var jsonArray = new JsonArray();
            var cursor = valuePagedResult.paging_metadata?.cursor;

            foreach (var json in valuePagedResult.result.Select(item => new ValueOnlyJsonSerializer().ToJsonObject(item)))
            {
                jsonArray.Add(json);
            }

            var jsonNode = new JsonObject
            {
                ["result"] = jsonArray
            };

            if (cursor != null)
            {
                jsonNode["paging_metadata"] = new JsonObject {["cursor"] = cursor};
            }

            jsonNode.WriteTo(writer);
        }

        private static void WritePagedResult(Utf8JsonWriter writer, PagedResult pagedResult)
        {
            var jsonArray = new JsonArray();
            var cursor = pagedResult.paging_metadata?.cursor;

            foreach (var json in pagedResult.result.Select(Jsonization.Serialize.ToJsonObject))
            {
                jsonArray.Add(json);
            }

            var jsonNode = new JsonObject
            {
                ["result"] = jsonArray
            };

            if (cursor != null)
            {
                jsonNode["paging_metadata"] = new JsonObject {["cursor"] = cursor};
            }

            jsonNode.WriteTo(writer);
        }


        private static void GetSerializationModifiersFromRequest(HttpRequest request, out LevelEnum level, out ExtentEnum extent)
        {
            request.Query.TryGetValue("level", out var levelValues);
            if (levelValues.Any())
            {
                Enum.TryParse(levelValues.First(), out level);
            }
            else
            {
                level = LevelEnum.Deep;
            }

            request.Query.TryGetValue("extent", out var extendValues);
            if (extendValues.Any())
            {
                Enum.TryParse(extendValues.First(), out extent);
            }
            else
            {
                extent = ExtentEnum.WithoutBlobValue;
            }
        }
    }
}