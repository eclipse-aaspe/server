using AasxServerStandardBib.Exceptions;
using IO.Swagger.V1RC03.ApiModel;
using IO.Swagger.V1RC03.Exceptions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace IO.Swagger.V1RC03.Middleware
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
            logger.LogInformation(exception.StackTrace);
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
                case InvalidOutputModifierException:
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

            result.Success = false;
            result.Messages = new List<Message>() { message };
            await context.Response.WriteAsync(result.ToJson());
        }
    }
}
