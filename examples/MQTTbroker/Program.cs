/********************************************************************************
* Copyright (c) 2025 Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the MIT License which is available at
* https://mit-license.org/
*
* SPDX-License-Identifier: MIT
********************************************************************************/

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using MQTTnet;
using MQTTnet.Server;

class Program
{
    static async Task Main()
    {
        var mqttServerOptions = new MqttServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(1883)
            .WithDefaultEndpointBoundIPAddress(IPAddress.Any)
            .Build();

        var mqttServer = new MqttServerFactory().CreateMqttServer(mqttServerOptions);

        mqttServer.ValidatingConnectionAsync += context =>
        {
            Console.WriteLine();
            Console.WriteLine("ClientID: " + context.ClientId);

            var authType = "noauth";
            var user = "";
            var domain = "";
            var serverName = "";

            string? jwtToken = context.UserProperties?
                .FirstOrDefault(p => p.Name.Equals("authorization", StringComparison.OrdinalIgnoreCase))?.Value?
                .Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)?
                .ElementAtOrDefault(1);

            var username = context.UserName;
            var password = context.Password;

            if (string.IsNullOrEmpty(jwtToken) && !string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                if (username == "jwt")
                {
                    jwtToken = password;
                }
            }

            if (!string.IsNullOrEmpty(jwtToken))
            {
                Console.WriteLine("Validating jwtToken: " + jwtToken);
                var handler = new JwtSecurityTokenHandler();
                var jwtSecurityToken = handler.ReadJwtToken(jwtToken);
                if (jwtSecurityToken != null)
                {
                    if (jwtSecurityToken.Claims != null)
                    {

                        var emailClaim = jwtSecurityToken.Claims.FirstOrDefault(c => c.Type.Equals("email"));
                        if (emailClaim != null && !string.IsNullOrEmpty(emailClaim.Value))
                        {
                            user = emailClaim.Value.ToLower();
                        }

                        var serverNameClaim = jwtSecurityToken.Claims.FirstOrDefault(c => c.Type.Equals("serverName"));
                        if (serverNameClaim != null && !string.IsNullOrEmpty(serverNameClaim.Value))
                        {
                            serverName = serverNameClaim.Value;
                        }

                        var userNameClaim = jwtSecurityToken.Claims.FirstOrDefault(c => c.Type.Equals("userName"));
                        if (userNameClaim != null && !string.IsNullOrEmpty(userNameClaim.Value))
                        {
                            var userName = userNameClaim.Value;
                            if (!string.IsNullOrEmpty(userName))
                            {
                                user = userName.ToLower();
                            }
                        }
                        Console.WriteLine($"user: {user}, serverName: {serverName}");
                        if (user != "")
                        {
                            if (user.Contains("@"))
                            {
                                var split = user.Split("@");
                                if (split.Count() == 2)
                                {
                                    domain = split[1];
                                }
                            }

                            context.SessionItems["authtype"] = authType;
                            context.SessionItems["username"] = user;
                            context.SessionItems["domain"] = domain;
                            Console.WriteLine("Success: JWT");
                            context.ReasonCode = MQTTnet.Protocol.MqttConnectReasonCode.Success;
                            return Task.CompletedTask;
                        }
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine($"Validating password: {username}:{password}");

                if (username == password)
                {
                    authType = "pw";
                    user = username;
                    if (user.Contains("@"))
                    {
                        var split = user.Split("@");
                        if (split.Count() == 2)
                        {
                            domain = split[1];
                        }
                    }

                    context.SessionItems["authtype"] = authType;
                    context.SessionItems["username"] = user;
                    context.SessionItems["domain"] = domain;
                    Console.WriteLine("Success: PW");
                    context.ReasonCode = MQTTnet.Protocol.MqttConnectReasonCode.Success;
                }
                else
                {
                    Console.WriteLine("BadUserNameOrPassword");
                    context.ReasonCode = MQTTnet.Protocol.MqttConnectReasonCode.BadUserNameOrPassword;
                }
            }
            else
            {
                context.SessionItems["authtype"] = authType;
                context.SessionItems["username"] = user;
                context.SessionItems["domain"] = domain;
                Console.WriteLine("Success: NOAUTH");
                context.ReasonCode = MQTTnet.Protocol.MqttConnectReasonCode.Success;
            }

            return Task.CompletedTask;
        };

        mqttServer.InterceptingPublishAsync += context =>
        {
            Console.WriteLine();
            Console.WriteLine($"Client {context.ClientId} wants to publish.");

            var authType = "";
            var user = "";
            var domain = "";

            authType = context.SessionItems["authtype"]?.ToString();
            user = context.SessionItems["username"]?.ToString();
            domain = context.SessionItems["domain"]?.ToString();
            Console.WriteLine("AuthType: " + authType);
            Console.WriteLine("User: " + user);
            Console.WriteLine("Domain: " + domain);

            var payload = context.ApplicationMessage?.Payload == null
            ? null
            : Encoding.UTF8.GetString(context.ApplicationMessage.Payload);

            Console.WriteLine("Message received:");
            Console.WriteLine($"topic: {context.ApplicationMessage?.Topic}");
            Console.WriteLine($"payload: {payload}");
            Console.WriteLine($"QoS: {context.ApplicationMessage?.QualityOfServiceLevel}");
            Console.WriteLine($"Retain: {context.ApplicationMessage?.Retain}");

            if (string.IsNullOrWhiteSpace(user))
            {
                Console.WriteLine("Publish rejected: username is empty.");
                context.ProcessPublish = false;
            }

            return Task.CompletedTask;
        };

        mqttServer.InterceptingSubscriptionAsync += context =>
        {
            Console.WriteLine();
            Console.WriteLine($"Client {context.ClientId} wants to subscribe: {context.TopicFilter.Topic}");

            var authType = "";
            var user = "";
            var domain = "";

            authType = context.SessionItems["authtype"]?.ToString();
            user = context.SessionItems["username"]?.ToString();
            domain = context.SessionItems["domain"]?.ToString();
            Console.WriteLine("AuthType: " + authType);
            Console.WriteLine("User: " + user);
            Console.WriteLine("Domain: " + domain);

            var topic = context.TopicFilter.Topic;

            // user * accepts all
            if (user == "*")
            {
                Console.WriteLine("Subscribe all: " + topic);
                context.ProcessSubscription = true;
                return Task.CompletedTask;
            }

            // ignore standard topics
            if (topic == "$SYS/#")
            {
                Console.WriteLine("Ignore: " + topic);
                context.ProcessSubscription = false;
                return Task.CompletedTask;
            }

            // rewrite topics for #
            if (topic == "#")
            {
                context.ProcessSubscription = false;

                var allowedTopics = new List<string>();

                if (user == "")
                {
                    allowedTopics.Add("/noauth/#");
                }
                else
                {
                    allowedTopics.Add("/noauth/#");
                    allowedTopics.Add("/fx/all/#");
                    if (!string.IsNullOrWhiteSpace(domain))
                    {
                        allowedTopics.Add($"/fx/domain/{domain}/#");
                    }
                }

                foreach (var allowedTopic in allowedTopics)
                {
                    Console.WriteLine($"Subscribed {context.ClientId} to {allowedTopic}");
                    mqttServer.SubscribeAsync(context.ClientId, new MqttTopicFilterBuilder()
                    .WithTopic(allowedTopic)
                    .WithAtMostOnceQoS()
                    .Build());
                }

                return Task.CompletedTask;
            }

            if (user == "" && topic != "/noauth/#")
            {
                Console.WriteLine("Subscription rejected: topic != /noauth/#");
                context.ProcessSubscription = false;
            }
            else
            {
                if (topic != "/noauth/#" && topic != "/fx/all/#")
                {
                    if (string.IsNullOrWhiteSpace(domain))
                    {
                        Console.WriteLine("Subscription rejected: no domain for /fx/domain");
                        context.ProcessSubscription = false;
                    }
                    else if (topic != $"/fx/domain/{domain}/#")
                    {
                        Console.WriteLine("Subscription rejected: other domain");
                        context.ProcessSubscription = false;
                    }
                }
            }

            return Task.CompletedTask;
        };

        await mqttServer.StartAsync();

        Console.WriteLine("MQTT broker is running on port 1883. Stop with CTRL+C.");
        await Task.Delay(Timeout.Infinite);

        await mqttServer.StopAsync();
    }
}
