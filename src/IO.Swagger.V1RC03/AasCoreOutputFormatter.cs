
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
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml;
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
            else
                return false;
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            var response = context.HttpContext.Response;
            var httpContext = context.HttpContext;

            var contentType = httpContext.Request.ContentType;


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

            if (!string.IsNullOrEmpty(contentType) && contentType.Contains("xml"))
            {
                //TODO: jtikekar refactor
                response.ContentType = contentType;
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                StringBuilder xml = new StringBuilder();
                using (XmlWriter writer = XmlWriter.Create(xml, settings))
                {
                    Xmlization.Serialize.To((IClass)context.Object, writer);
                    writer.Flush();
                }
                response.WriteAsync(xml.ToString());
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

                //The list structure is only applicable for "GetAllSubmodelElements", bzw., only Submodel. Therefore, level = core, then indirect children should be avoided.
                //Hence, setting IncludeChildren = false here itself.
                if (outputModifierContext.Level.Equals("core", StringComparison.OrdinalIgnoreCase))
                {
                    outputModifierContext.IncludeChildren = false;
                }

                //content = path is not handled here.
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
            return Task.FromResult(response);
        }
    }
}