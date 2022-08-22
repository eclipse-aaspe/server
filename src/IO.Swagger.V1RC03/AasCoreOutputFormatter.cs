using AasCore.Aas3_0_RC02;
using IO.Swagger.V1RC03.APIModels.Core;
using IO.Swagger.V1RC03.APIModels.ValueOnly;
using IO.Swagger.V1RC03.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Nodes = System.Text.Json.Nodes;

namespace IO.Swagger.V1RC03
{
    public class AasCoreOutputFormatter : OutputFormatter
    {

        public AasCoreOutputFormatter()
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

        public override bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            if (typeof(IClass).IsAssignableFrom(context.ObjectType))
            {
                return base.CanWriteResult(context);
            }
            if (IsGenericListOfIClass(context.Object))
            {
                return base.CanWriteResult(context);
            }
            if (typeof(IValue).IsAssignableFrom(context.ObjectType))
            {
                return base.CanWriteResult(context);
            }
            else
                return false;
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            var response = context.HttpContext.Response;
            var httpContext = context.HttpContext;
            var serviceProvider = httpContext.RequestServices;
            var _modifierService = serviceProvider.GetRequiredService<IOutputModifiersService>();

            //Get output modifiers from request
            // 1. level
            context.HttpContext.Request.Query.TryGetValue("level", out StringValues levelValues);
            var level = levelValues.Any() ? levelValues.First() : null;

            // 1. content
            context.HttpContext.Request.Query.TryGetValue("content", out StringValues contentValues);
            var content = contentValues.Any() ? contentValues.First() : null;

            // 1. level
            context.HttpContext.Request.Query.TryGetValue("extent", out StringValues extentValues);
            var extent = extentValues.Any() ? extentValues.First() : null;

            var outputModifierContext = new OutputModifierContext(level, content, extent);

            //Validate Output modifiers
            _modifierService.ValidateOutputModifiers(context.Object, level, content, extent);


            if (IsGenericListOfIClass(context.Object))
            //if (IsGenericListOfIClass(output))
            {
                var jsonArray = new JsonArray();
                IList genericList = (IList)context.Object;
                List<IClass> contextObjectType = new List<IClass>();
                foreach (var generic in genericList)
                {
                    contextObjectType.Add((IClass)generic);
                }

                if (!string.IsNullOrEmpty(content) && content.Equals("path", StringComparison.OrdinalIgnoreCase))
                {
                    var output = new List<object>();
                    foreach (var item in contextObjectType)
                    {
                        var idShortPath = PathSerializer.ToIdShortPath(item, outputModifierContext);
                        output.Add(idShortPath);
                    }
                    var jsonPath = JsonSerializer.Serialize(output);
                    httpContext.Response.WriteAsync(jsonPath);
                }
                else
                {
                    foreach (var item in contextObjectType)
                    {
                        //var json = AasCore.Aas3_0_RC02.Jsonization.Serialize.ToJsonObject(item);
                        var json = CoreSerializer.ToJsonObject(item, outputModifierContext);
                        jsonArray.Add(json);
                    }
                    var writer = new Utf8JsonWriter(response.Body);
                    jsonArray.WriteTo(writer);
                    writer.FlushAsync().GetAwaiter().GetResult();
                }
            }
            else if (typeof(IClass).IsAssignableFrom(context.ObjectType))
            {
                //default
                //Nodes.JsonObject json = AasCore.Aas3_0_RC02.Jsonization.Serialize.ToJsonObject((IClass)output);
                if (!string.IsNullOrEmpty(content) && content.Equals("path", StringComparison.OrdinalIgnoreCase))
                {
                    var output = PathSerializer.ToIdShortPath((IClass)context.Object, outputModifierContext);
                    var jsonPath = JsonSerializer.Serialize(output);
                    httpContext.Response.WriteAsync(jsonPath);
                }
                else
                {
                    Nodes.JsonObject json = CoreSerializer.ToJsonObject((IClass)context.Object, outputModifierContext);
                    var writer = new Utf8JsonWriter(response.Body);
                    json.WriteTo(writer);
                    writer.FlushAsync().GetAwaiter().GetResult();
                }
            }
            //else if(typeof(JsonObject).IsAssignableFrom(output.GetType()))
            //{
            //    //Write ValueOnly
            //    JsonObject json = (JsonObject)output;
            //    var writer = new Utf8JsonWriter(response.Body);
            //    json.WriteTo(writer);
            //    writer.FlushAsync().GetAwaiter().GetResult();
            //}
            //else
            //{
            //    //Write IdShortPath
            //    var json = JsonSerializer.Serialize(output);
            //    httpContext.Response.WriteAsync(json);
            //}
            return Task.FromResult(response);
        }
    }
}