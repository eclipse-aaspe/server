﻿using DataTransferObjects;
using DataTransferObjects.ValueDTOs;
using IO.Swagger.Lib.V3.SerializationModifiers;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;
using IO.Swagger.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace IO.Swagger.Lib.V3.Formatters
{
    public class AasResponseFormatter : OutputFormatter
    {
        public AasResponseFormatter()
        {
            this.SupportedMediaTypes.Clear();
            this.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
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
            if (typeof(IClass).IsAssignableFrom(context.ObjectType))
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
            else
                return false;
        }
        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            var httpContext = context.HttpContext;
            var response = context.HttpContext.Response;

            //SerializationModifier
            GetSerializationMidifiersFromRequest(context.HttpContext.Request, out LevelEnum level, out ExtentEnum extent);


            if (typeof(IClass).IsAssignableFrom(context.ObjectType))
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

                var jsonArray = new JsonArray();
                IList genericList = (IList)context.Object;
                List<IClass> contextObjectType = new List<IClass>();
                foreach (var generic in genericList)
                {
                    contextObjectType.Add((IClass)generic);
                }

                foreach (var item in contextObjectType)
                {
                    var json = Jsonization.Serialize.ToJsonObject(item);
                    //var json = LevelExtentSerializer.ToJsonObject(item, modifierContext);
                    jsonArray.Add(json);
                }
                var writer = new Utf8JsonWriter(response.Body);
                jsonArray.WriteTo(writer);
                writer.FlushAsync().GetAwaiter().GetResult();
            }
            else if (typeof(IValueDTO).IsAssignableFrom(context.ObjectType))
            {
                JsonNode json = ValueOnlyJsonSerializer.ToJsonObject((IValueDTO)context.Object);
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
                    var json = ValueOnlyJsonSerializer.ToJsonObject((IValueDTO)item);
                    jsonArray.Add(json);
                }
                var writer = new Utf8JsonWriter(response.Body);
                jsonArray.WriteTo(writer);
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