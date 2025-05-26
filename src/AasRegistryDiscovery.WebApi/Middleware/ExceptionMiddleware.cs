namespace AasRegistryDiscovery.WebApi.Middleware;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AasRegistryDiscovery.WebApi.Exceptions;
using AasRegistryDiscovery.WebApi.Models;
using AasRegistryDiscovery.WebApi.Serializers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using static AasRegistryDiscovery.WebApi.Models.Message;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext httpContext, ILogger<ExceptionMiddleware> logger)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(httpContext, ex, logger);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, ILogger<ExceptionMiddleware> logger)
    {
        logger.LogError(exception.Message);
        logger.LogInformation(exception.StackTrace);
        context.Response.ContentType = "application/json";
        var result = new Result();
        var message = new Message();
        var currentDateTime = DateTime.Now.ToString(CultureInfo.InvariantCulture);
        switch (exception)
        {
            case NotImplementedException:
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
                message.Code = HttpStatusCode.NotImplemented.ToString();
                message.Text = exception.Message;
                message.Timestamp = currentDateTime;
                message.MessageType = MessageTypeEnum.ErrorEnum;
                break;
            }
            case DuplicateResourceException ex:
            {
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                message.Code = HttpStatusCode.Conflict.ToString();
                message.Text = ex.Message;
                message.Timestamp = currentDateTime;
                message.MessageType = MessageTypeEnum.ErrorEnum;
                break;
            }
            case InvalidPaginationParameterException:
            case Jsonization.Exception:
            case NoIdentifierException:
            case ArgumentNullException:
            case Base64UrlDecoderException:
            case InvalidOperationException:
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                message.Code = HttpStatusCode.BadRequest.ToString();
                message.Text = exception.Message;
                message.Timestamp = currentDateTime;
                message.MessageType = MessageTypeEnum.ErrorEnum;
                break;
            }
            case NotFoundException:
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                message.Code = HttpStatusCode.NotFound.ToString();
                message.Text = exception.Message;
                message.Timestamp = currentDateTime;
                message.MessageType = MessageTypeEnum.ErrorEnum;
                break;
            }
            default:
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                message.Code = HttpStatusCode.InternalServerError.ToString();
                message.Text = exception.Message;
                message.Timestamp = currentDateTime;
                message.MessageType = MessageTypeEnum.ErrorEnum;
                break;
            }
        }

        result.Messages = new List<Message>() { message };
        await context.Response.WriteAsync(DescriptorSerializer.ToJsonObject(result)!.ToString());
    }
}
