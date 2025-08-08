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

namespace AasxServerDB;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using System.Security.Authentication;
using System.Text.Json;
public class MqttClientService
{
    private readonly List<IMqttClient> _mqttClients;
    private readonly MqttClientFactory _factory;
    private readonly ILogger<MqttClientService> _logger;


    //private MqttClientOptions _options;


    public MqttClientService(ILogger<MqttClientService> logger)
    {
        _logger = logger;

        _factory = new MqttClientFactory();

        _mqttClients = new List<IMqttClient>();
    }

    public async Task<MqttClientConnectResult> ConnectAsync(IMqttClient mqttClient, string clientId, string messageBroker, string userName, string password)
    {
        var messageBrokerData = messageBroker.Split(':');

        if (messageBrokerData.Length != 3)
        {
            return null;
        }

        bool withTLS = messageBrokerData[0].Equals("MQTTS",StringComparison.OrdinalIgnoreCase);
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
                            .WithCleanSession()
                            .Build();
        var result = await mqttClient.ConnectAsync(options);
        _logger.LogInformation("MQTT connected.");
        Console.WriteLine("MQTT connected.");

        return result;
    }

    public async Task DisconnectAsync(string clientId)
    {
        var mqttClient = _mqttClients.Where(cl => cl.Options.ClientId == clientId).FirstOrDefault();

        if (mqttClient.IsConnected)
        {
            _logger.LogInformation("MQTT disconnected.");
            Console.WriteLine("MQTT disconnected.");
        }
    }

    public async Task<MqttClientPublishResult> PublishAsync(string clientId, string messageBroker, string userName, string password, string topic, string payload)
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
            .WithTopic(topic)
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

//public class SubmodelPublisherService : BackgroundService
//{
//    public static bool SystemRunning = false;

//    private readonly MqttClientService _mqttService;
//    private readonly ILogger<SubmodelPublisherService> _logger;
//    private DateTime _lastPublish;
//    private bool lastPublishInitialized;
//    private bool enable;

//    public SubmodelPublisherService(MqttClientService mqttService, ILogger<SubmodelPublisherService> logger)
//    {
//        _mqttService = mqttService;
//        _logger = logger;

//        var envValue = Environment.GetEnvironmentVariable("AASX_MQTT");
//        enable = envValue == "1";

//        _logger.LogInformation($"SubmodelPublisherService enabled: {enable}");
//        Console.WriteLine("SubmodelPublisherService");
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        if (enable)
//        {
//            while (!stoppingToken.IsCancellationRequested)
//            {
//                if (Contracts.Running.GetRunning())
//                {
//                    if (!lastPublishInitialized)
//                    {
//                        _lastPublish = DateTime.UtcNow;
//                        lastPublishInitialized = true;
//                    }

//                    var changed = GetChangedSubmodelsJson(); // Deine Methode

//                    if (changed != null)
//                    {
//                        // var json = JsonSerializer.Serialize(changed);
//                        var json = changed;
//                        await _mqttService.PublishAsync("test", "/noauth/submodels", json);
//                        await _mqttService.PublishAsync("test", "/fx/all/submodels", json);
//                        await _mqttService.PublishAsync("test", "/fx/domain/phoenixcontact.com/submodels", json);
//                        _lastPublish = DateTime.UtcNow;
//                    }
//                }

//                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
//            }
//        }
//    }

//    private string? GetChangedSubmodelsJson()
//    {
//        using (var db = new AasContext())
//        {
//            var smDBList = db.SMSets
//                .Where(sm => sm.TimeStampTree >= _lastPublish)
//                .ToList();

//            var payloadList = new List<object>();

//            foreach (var sm in smDBList)
//            {
//                var payloadObject = new
//                {
//                    specversion = "1.0",
//                    type = "UpdatedSubmodel",
//                    source = "https://pathAddedLater",
//                    subject = new
//                    {
//                        id = sm.Identifier,
//                        semanticId = sm.SemanticId
//                    },
//                    id = $"later-{Guid.NewGuid()}",
//                    time = DateTime.UtcNow.ToString("o"),
//                    datacontenttype = "application/json+submodel",
//                    data = new { }
//                };

//                payloadList.Add(payloadObject);
//            }

//            if (payloadList.Count > 0)
//            {
//                var jsonArray = JsonSerializer.Serialize(payloadList);
//                Console.WriteLine(jsonArray);
//                return jsonArray;
//            }

//            return null;
//        }
//    }
//}

