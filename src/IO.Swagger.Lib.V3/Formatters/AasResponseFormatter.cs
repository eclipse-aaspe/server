using AdminShellNS.Lib.V3.Models;
using DataTransferObjects;
using IO.Swagger.Lib.V3.SerializationModifiers;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;
using IO.Swagger.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections;
using System.Collections.Generic;
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

            return false;
        }
        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            var response = context.HttpContext.Response;

            //SerializationModifier
            GetSerializationModifiersFromRequest(context.HttpContext.Request, out var level, out var extent);


            if (typeof(IClass).IsAssignableFrom(context.ObjectType))
            {
                //Validate modifiers
                SerializationModifiersValidator.Validate((IClass)context.Object, level, extent);

                var json = Jsonization.Serialize.ToJsonObject((IClass)context.Object);
                var writer = new Utf8JsonWriter(response.Body);
                json.WriteTo(writer);
                writer.FlushAsync().GetAwaiter().GetResult();
            }
            else if (IsGenericListOfIClass(context.Object))
            {

                var jsonArray = new JsonArray();
                var genericList = (IList)context.Object;
                var contextObjectType = genericList.Cast<IClass>().ToList();

                foreach (var json in contextObjectType.Select(Jsonization.Serialize.ToJsonObject))
                {
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
                    foreach (var json in pagedResult.result.Select(item => new ValueOnlyJsonSerializer().ToJsonObject(item)))
                    {
                        jsonArray.Add(json);
                    }
                }
                var jsonNode = new JsonObject
                {
                    ["result"] = jsonArray
                };
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
                var json = new ValueOnlyJsonSerializer().ToJsonObject((IValueDTO)context.Object);
                var writer = new Utf8JsonWriter(response.Body);
                json.WriteTo(writer);
                writer.FlushAsync().GetAwaiter().GetResult();
            }
            else if (IsGenericListOfIValueDTO(context.Object))
            {
                var jsonArray = new JsonArray();
                var genericList = (IList)context.Object;
                var contextObjectType = genericList.Cast<IValueDTO>().ToList();

                foreach (var json in contextObjectType.Select(item => new ValueOnlyJsonSerializer().ToJsonObject(item)))
                {
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
                    foreach (var json in pagedResult.result.Select(Jsonization.Serialize.ToJsonObject))
                    {
                        jsonArray.Add(json);
                    }
                }
                var jsonNode = new JsonObject
                {
                    ["result"] = jsonArray
                };
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

        private void GetSerializationModifiersFromRequest(HttpRequest request, out LevelEnum level, out ExtentEnum extent)
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
