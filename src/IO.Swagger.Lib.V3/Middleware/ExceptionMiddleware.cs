/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

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
    using System.Globalization;
    using System.Linq;

    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next) => _next = next;

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
            logger.LogDebug(exception.StackTrace ?? $"No Stacktrace for {exception}");
            context.Response.ContentType = "application/json";
            var result = new Result();
            var message = new Message();
            var currentDateTime = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            switch (exception)
            {
                case DuplicateException ex:
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                        message.Code                = HttpStatusCode.Conflict.ToString();
                        message.Text                = ex.Message;
                        message.Timestamp           = currentDateTime;
                        message.MessageType         = MessageTypeEnum.Error;
                        break;
                    }
                case FileNotFoundException:
                case NotFoundException:
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        message.Code = HttpStatusCode.NotFound.ToString();
                        message.Text = exception.Message;
                        message.Timestamp = currentDateTime;
                        message.MessageType = MessageTypeEnum.Error;
                        break;
                    }
                case NotAllowed:
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                        message.Code = HttpStatusCode.Forbidden.ToString();
                        message.Text = exception.Message;
                        message.Timestamp = currentDateTime;
                        message.MessageType = MessageTypeEnum.Error;
                        break;
                    }
                case MetamodelVerificationException ex:
                    {
                        //Print the errors in debug level
                        foreach (var errorText in ex.ErrorList.Select(error => $"{Reporting.GenerateJsonPath(error.PathSegments)}:{error.Cause}"))
                        {
                            logger.LogDebug(errorText);
                        }

                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        message.Code = HttpStatusCode.BadRequest.ToString();
                        message.Text = exception.Message;
                        message.Timestamp = currentDateTime;
                        message.MessageType = MessageTypeEnum.Error;
                        break;
                    }
                case InvalidPaginationParameterException:
                case Jsonization.Exception:
                case InvalidIdShortPathException:
                case InvalidUpdateResourceException:
                case EmptyCursorException:
                case OperationNotSupported:
                case InvalidNumberOfChildElementsException:
                case NoIdentifierException:
                case ArgumentNullException:
                case OperationVariableException:
                case JsonDeserializationException:
                case Base64UrlDecoderException:
                case InvalidOperationException:
                case InvalidSerializationModifierException:
                {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        message.Code = HttpStatusCode.BadRequest.ToString();
                        message.Text = exception.Message;
                        message.Timestamp = currentDateTime;
                        message.MessageType = MessageTypeEnum.Error;
                        break;
                    }
                case Exceptions.NotImplementedException:
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
                        message.Code = HttpStatusCode.NotImplemented.ToString();
                        message.Text = exception.Message;
                        message.Timestamp = currentDateTime;
                        message.MessageType = MessageTypeEnum.Error;
                        break;
                    }
                case UnprocessableEntityException:
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
                        message.Code = HttpStatusCode.UnprocessableEntity.ToString();
                        message.Text = exception.Message;
                        message.Timestamp = currentDateTime;
                        message.MessageType = MessageTypeEnum.Error;
                        break;
                    }
                default:
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        message.Code = HttpStatusCode.InternalServerError.ToString();
                        message.Text = exception.Message;
                        message.Timestamp = currentDateTime;
                        message.MessageType = MessageTypeEnum.Error;
                        break;
                    }
            }

            result.Messages = new List<Message>() { message };
            await context.Response.WriteAsync(result.ToJson());
        }
    }
}
