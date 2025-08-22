/********************************************************************************
* Copyright (c) {2019 - 2025} Contributors to the Eclipse Foundation
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

//ToDo: Move to some kind of communication project
namespace AasxServerDB;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using System.Security.Authentication;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text;

public class MqttClientServiceMessageReceivedEventArgs
{
    public JsonArray Message
    {
        get; set;
    }

    public string ClientId { get; set; }
}

public class MqttClientService
{
    private readonly List<IMqttClient> _mqttClients;
    private readonly MqttClientFactory _factory;
    private readonly ILogger<MqttClientService> _logger;

    public event EventHandler<MqttClientServiceMessageReceivedEventArgs>? MessageReceived;

    public MqttClientService(ILogger<MqttClientService> logger)
    {
        _logger = logger;

        _factory = new MqttClientFactory();

        _mqttClients = new List<IMqttClient>();
    }

    private async Task<MqttClientConnectResult> ConnectAsync(IMqttClient mqttClient, string clientId, string messageBroker, string userName, string password)
    {
        var messageBrokerData = messageBroker.Split(':');

        if (messageBrokerData.Length != 3)
        {
            return null;
        }

        bool withTLS = messageBrokerData[0].Equals("MQTTS", StringComparison.OrdinalIgnoreCase);
        var serverHost = messageBrokerData[1].Replace("//", "");
        var serverPort = int.Parse(messageBrokerData[2]);

        var options = new MqttClientOptionsBuilder()
                            .WithClientId(clientId)
                            .WithTcpServer(serverHost, serverPort)
                            .WithCredentials(userName, password)
                            .WithTlsOptions(new MqttClientTlsOptions
                            {
                                UseTls = withTLS,
                                SslProtocol = SslProtocols.Tls12, // oder Tls13, je nach Server
                                AllowUntrustedCertificates = false,
                                IgnoreCertificateChainErrors = false,
                                IgnoreCertificateRevocationErrors = false
                            })
                            .WithCleanSession(false)
                            .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
                            .Build();
        var result = await mqttClient.ConnectAsync(options);
        _logger.LogInformation("MQTT connected.");
        Console.WriteLine("MQTT connected.");

        return result;
    }

    private async Task DisconnectAsync(string clientId)
    {
        var mqttClient = _mqttClients.Where(cl => cl.Options.ClientId == clientId).FirstOrDefault();

        if (mqttClient.IsConnected)
        {
            _logger.LogInformation("MQTT disconnected.");
            Console.WriteLine("MQTT disconnected.");
        }
    }
    public async Task<MqttClientSubscribeResult> SubscribeAsync(string clientId, string messageBroker, string messageTopic, string userName, string password)
    {
        var mqttClient = _mqttClients.FirstOrDefault(cl => cl.Options.ClientId == clientId);

        if (mqttClient == null)
        {
            mqttClient = _factory.CreateMqttClient();
        }

        mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            var payload = e.ApplicationMessage?.Payload == null
                ? null
                : Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

            try
            {
                var json = JsonArray.Parse(payload) as JsonArray;

                if (json != null)
                {
                    OnMessageReceived(new MqttClientServiceMessageReceivedEventArgs()
                    {
                        ClientId = clientId,
                        Message = json
                    });
                }
            }
            catch (Exception)
            {
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        };

        mqttClient.DisconnectedAsync += e =>
        {
            _logger.LogWarning($"Disconnected: Reason={e.Reason}, Exception={e.Exception?.Message}");

            return Task.CompletedTask;
        };

        if (!mqttClient.IsConnected)
        {
            var result = await ConnectAsync(mqttClient, clientId, messageBroker, userName, password);
            if (result != null && result.ResultCode == MqttClientConnectResultCode.Success)
            {
                _mqttClients.Add(mqttClient);
            }
        }

        if (mqttClient != null && mqttClient.IsConnected)
        {
            var messageResult = await mqttClient.SubscribeAsync(messageTopic);
            return messageResult;
        }

        return null;
    }

    protected virtual void OnMessageReceived(MqttClientServiceMessageReceivedEventArgs result)
    {
        MessageReceived?.Invoke(this, result);
    }

    public async Task<MqttClientPublishResult> PublishAsync(string clientId, string messageBroker, string messageTopic, string userName, string password, string payload)
    {
        var mqttClient = _mqttClients.FirstOrDefault(cl => cl.Options.ClientId == clientId);

        if (mqttClient == null)
        {
            mqttClient = _factory.CreateMqttClient();
        }

        if (!mqttClient.IsConnected)
        {
            var result = await ConnectAsync(mqttClient, clientId, messageBroker, userName, password);
            if (result != null && result.ResultCode == MqttClientConnectResultCode.Success)
            {
                _mqttClients.Add(mqttClient);
            }
        }

        if (mqttClient != null && mqttClient.IsConnected)
        {
            var message = new MqttApplicationMessageBuilder()
            .WithTopic(messageTopic)
            .WithPayload(payload)
            .WithContentType("application/json")
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
            .WithRetainFlag()
            .Build();

            var messageResult = await mqttClient.PublishAsync(message);
            return messageResult;
        }

        return null;
    }
}

