namespace AasxServerDB;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using System.Security.Authentication;
using System.Text.Json;
public class MqttClientService
{
    private readonly IMqttClient _mqttClient;
    private readonly MqttClientOptions _options;
    private readonly ILogger<MqttClientService> _logger;

    public MqttClientService(ILogger<MqttClientService> logger)
    {
        _logger = logger;
        Console.WriteLine("MqttClientService");

        var factory = new MqttClientFactory();
        _mqttClient = factory.CreateMqttClient();

        _options = new MqttClientOptionsBuilder()
            .WithClientId("AasxServerClient")
            .WithTcpServer("mqtt-broker.aas-voyager.com", 8883)
            .WithCredentials("phoenixcontact.com", "phoenixcontact.com")
            .WithTlsOptions(new MqttClientTlsOptions
            {
                UseTls = true,
                SslProtocol = SslProtocols.Tls12, // oder Tls13, je nach Server
                AllowUntrustedCertificates = false,
                IgnoreCertificateChainErrors = false,
                IgnoreCertificateRevocationErrors = false
            })
            .WithCleanSession()
            .Build();
    }

    public async Task ConnectAsync()
    {
        if (!_mqttClient.IsConnected)
        {
            await _mqttClient.ConnectAsync(_options);
            _logger.LogInformation("MQTT connected.");
            Console.WriteLine("MQTT connected.");
        }
    }

    public async Task DisconnectAsync()
    {
        if (_mqttClient.IsConnected)
        {
            _logger.LogInformation("MQTT disconnected.");
            Console.WriteLine("MQTT disconnected.");
        }
    }

    public async Task PublishAsync(string topic, string payload)
    {
        if (!_mqttClient.IsConnected)
        {
            await ConnectAsync();
        }

        if (_mqttClient.IsConnected)
        {
            var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
            .WithRetainFlag()
            .Build();

            await _mqttClient.PublishAsync(message);
            _logger.LogInformation("MQTT message sent.");
            Console.WriteLine("MQTT message sent.");
        }
    }
}

public class SubmodelPublisherService : BackgroundService
{
    public static bool SystemRunning = false;

    private readonly MqttClientService _mqttService;
    private readonly ILogger<SubmodelPublisherService> _logger;
    private DateTime _lastPublish;
    private bool lastPublishInitialized;
    private bool enable;

    public SubmodelPublisherService(MqttClientService mqttService, ILogger<SubmodelPublisherService> logger)
    {
        _mqttService = mqttService;
        _logger = logger;

        var envValue = Environment.GetEnvironmentVariable("AASX_MQTT");
        enable = envValue == "1";

        _logger.LogInformation($"SubmodelPublisherService enabled: {enable}");
        Console.WriteLine("SubmodelPublisherService");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (enable)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (Contracts.Running.GetRunning())
                {
                    if (!lastPublishInitialized)
                    {
                        _lastPublish = DateTime.UtcNow;
                        lastPublishInitialized = true;
                    }

                    var changed = getChangedSubmodels(); // Deine Methode

                    if (changed != null && changed.Count != 0)
                    {
                        var json = JsonSerializer.Serialize(changed);
                        await _mqttService.PublishAsync("/noauth/submodels", json);
                        await _mqttService.PublishAsync("/fx/all/submodels", json);
                        await _mqttService.PublishAsync("/fx/domain/phoenixcontact.com/submodels", json);
                        _lastPublish = DateTime.UtcNow;
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private List<string?>? getChangedSubmodels()
    {
        using (var db = new AasContext())
        {
            // var s = db.SMSets.Where(sm => sm.TimeStampTree >= _lastPublish).Select(sm => sm.Identifier).ToList();
            // return s;

            var smDBList = db.SMSets.Where(sm => sm.TimeStampTree >= _lastPublish).ToList();

            List<string?>? result = [];
            foreach (var sm in smDBList)
            {
                var output = $"{{\r\n \"specversion\" : \"1.0\",\r\n \"type\" : \"UpdatedSubmodel\",\r\n \"source\" : \"https://pathAddedLater\",\r\n " +
                    $"\"subject\" : {{\r\n \"id\":  \"{sm.Identifier}\",\r\n \"semanticId\": \"{sm.SemanticId}\" \r\n   }},\r\n" +
                    $"\"id\" : \"later-4b1286f6-f6e9-4de3-944a-d565675ef7b1\",\r\n   \"time\" : \"{DateTime.UtcNow}\",\r\n   \"datacontenttype\" : \"application/json+submodel\", \r\n   \"data\" : {{}}\r\n}} \r\n";
                Console.WriteLine(output);
                result.Add(output);
            }
            return result;
        }
    }
}
