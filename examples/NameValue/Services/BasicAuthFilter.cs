
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using Microsoft.AspNetCore.Http;

namespace ApiNoUi.Services;

public class BasicAuthFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        if (!http.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            http.Response.Headers["WWW-Authenticate"] = "Basic realm=\"FileUpload\"";
            return Results.Unauthorized();
        }

        var parts = authHeader.ToString().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !parts[0].Equals("Basic", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Unauthorized();
        }

        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(parts[1]));
        }
        catch
        {
            return Results.Unauthorized();
        }

        var colonIdx = decoded.IndexOf(':');
        if (colonIdx <= 0) return Results.Unauthorized();

        var user = decoded[..colonIdx];
        var pw = decoded[(colonIdx + 1)..];

        if (!string.Equals(user, pw, StringComparison.Ordinal))
        {
            return Results.Unauthorized();
        }

        http.Items["Username"] = user;

        return await next(context);
    }
}
