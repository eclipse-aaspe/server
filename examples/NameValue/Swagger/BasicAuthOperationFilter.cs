
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NameValue.Swagger;

public class BasicAuthOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var relativePath = context.ApiDescription.RelativePath ?? string.Empty;
        if (relativePath.StartsWith("FileUpload", StringComparison.OrdinalIgnoreCase)
            || relativePath.Contains("/FileUpload", StringComparison.OrdinalIgnoreCase))
        {
            operation.Security ??= new List<OpenApiSecurityRequirement>();
            var scheme = new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "basic" }
            };
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [scheme] = Array.Empty<string>()
            });
        }
    }
}
