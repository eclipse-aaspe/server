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

using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using IO.Swagger.Models;

namespace IO.Swagger.Lib.V3.Middleware
{
    public static class ExceptionResultSerializer
    {
        public static JsonObject? ToJsonObject(object that)
        {
            if (that == null)
                return null;
            else if(that is Result result)
            {
                return Transform(result);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private static JsonObject? Transform(Result that)
        {
            if (that == null)
                throw new ArgumentNullException(nameof(that));

            var result = new JsonObject();

            if(that.Messages != null)
            {
                var arrayMessages = new JsonArray();
                foreach (var message in that.Messages)
                {
                    arrayMessages.Add(Transform(message));
                }

                result["messages"] = arrayMessages;
            }

            return result;
        }

        private static JsonObject? Transform(Message that)
        {
            if (that == null)
                throw new ArgumentNullException(nameof(that));

            var result = new JsonObject();
            if(that.MessageType != null)
            {
                result["messageType"] = MessageTypeToJsonValue(that.MessageType);
            }

            if (that.Text != null)
            {
                result["text"] = JsonValue.Create(that.Text);
            }

            if (that.Code != null)
            {
                result["code"] = JsonValue.Create(that.Code);
            }

            if (that.CorrelationId != null)
            {
                result["correlationId"] = JsonValue.Create(that.CorrelationId);
            }

            if (that.Timestamp != null)
            {
                result["timestamp"] = JsonValue.Create(that.Timestamp);
            }

            return result;
            
        }

        private static readonly Dictionary<MessageTypeEnum, string> MessageTypeToString = 
            new Dictionary<MessageTypeEnum, string>()
            {
                { MessageTypeEnum.Warning, "Warning" },
                { MessageTypeEnum.Error, "Error" },
                { MessageTypeEnum.Undefined, "Undefined" },
                { MessageTypeEnum.Exception, "Exception" },
                { MessageTypeEnum.Info, "Info" }
            };

        private static JsonNode? MessageTypeToJsonValue(MessageTypeEnum? that)
        {
            string value = null;
            if (!that.HasValue)
            {
                value = null;
            }
            else
            {
                if (MessageTypeToString.TryGetValue(that.Value, out value))
                {
                    
                }
                else
                {
                    value = null;
                }
            }
            return JsonValue.Create(value)
                ?? throw new ArgumentException(
                    $"Invalid MessageType: {that}");
        }
    }
}