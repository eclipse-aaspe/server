using AasSecurity.Exceptions;
using AasxServerStandardBib.Exceptions;
using AasxServerStandardBib.Logging;
using AdminShellNS.Exceptions;
using IO.Swagger.Lib.V3.Exceptions;
using IO.Swagger.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace IO.Swagger.Lib.V3.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext, IAppLogger<ExceptionMiddleware> logger)
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


        private async Task HandleExceptionAsync(HttpContext context, Exception exception, IAppLogger<ExceptionMiddleware> logger)
        {
            logger.LogError(exception.Message);
            logger.LogDebug(exception.StackTrace);
            context.Response.ContentType = "application/json";
            var result = new Result();
            var message = new Message();

            switch (exception)
            {
                case DuplicateException ex:
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                        message.Code = HttpStatusCode.Conflict.ToString();
                        message.Text = ex.Message;
                        message.Timestamp = DateTime.Now.ToString();
                        message.MessageType = Message.MessageTypeEnum.ErrorEnum;
                        break;
                    }
                case FileNotFoundException:
                case NotFoundException:
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        message.Code = HttpStatusCode.NotFound.ToString();
                        message.Text = exception.Message;
                        message.Timestamp = DateTime.Now.ToString();
                        message.MessageType = Message.MessageTypeEnum.ErrorEnum;
                        break;
                    }
                case NotAllowed:
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                        message.Code = HttpStatusCode.Forbidden.ToString();
                        message.Text = exception.Message;
                        message.Timestamp = DateTime.Now.ToString();
                        message.MessageType = Message.MessageTypeEnum.ErrorEnum;
                        break;
                    }
                case MetamodelVerificationException ex:
                    {
                        //Print the errors in debug level
                        foreach (var error in ex.ErrorList)
                        {
                            var errorText = Reporting.GenerateJsonPath(error.PathSegments) + ":" + error.Cause;
                            logger.LogDebug(errorText);
                        }

                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        message.Code = HttpStatusCode.BadRequest.ToString();
                        message.Text = exception.Message;
                        message.Timestamp = DateTime.Now.ToString();
                        message.MessageType = Message.MessageTypeEnum.ErrorEnum;
                        break;
                    }
                case InvalidUpdateResourceException:
                case EmptyCursorException:
                case OperationNotSupported:
                case InvalidNumberOfChildElementsException:
                case NoIdentifierException:
                case ArgumentNullException:
                case OperationVariableException:
                case JsonDeserializationException:
                case Base64UrlDecoderException:
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        message.Code = HttpStatusCode.BadRequest.ToString();
                        message.Text = exception.Message;
                        message.Timestamp = DateTime.Now.ToString();
                        message.MessageType = Message.MessageTypeEnum.ErrorEnum;
                        break;
                    }
                case InvalidSerializationModifierException:
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        message.Code = HttpStatusCode.MethodNotAllowed.ToString();
                        message.Text = exception.Message;
                        message.Timestamp = DateTime.Now.ToString();
                        message.MessageType = Message.MessageTypeEnum.ErrorEnum;
                        break;
                    }
                case Exceptions.NotImplementedException:
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
                        message.Code = HttpStatusCode.NotImplemented.ToString();
                        message.Text = exception.Message;
                        message.Timestamp = DateTime.Now.ToString();
                        message.MessageType = Message.MessageTypeEnum.ErrorEnum;
                        break;
                    }
                case UnprocessableEntityException:
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
                        message.Code = HttpStatusCode.UnprocessableEntity.ToString();
                        message.Text = exception.Message;
                        message.Timestamp = DateTime.Now.ToString();
                        message.MessageType = Message.MessageTypeEnum.ErrorEnum;
                        break;
                    }
                default:
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        message.Code = HttpStatusCode.InternalServerError.ToString();
                        message.Text = exception.Message;
                        message.Timestamp = DateTime.Now.ToString();
                        message.MessageType = Message.MessageTypeEnum.ErrorEnum;
                        break;
                    }
            }

            result.Messages = new List<Message>() { message };
            await context.Response.WriteAsync(result.ToJson());
        }
    }
}
