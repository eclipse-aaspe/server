using System.Text;
using MQTTnet;
using System.Text.Json;
using System.Security.Authentication;

class Program
{
    // args
    // MQTT-OZ pw mqtt-broker.aas-voyager.com 8883
    // MQTT-OZ pw localhost 1883
    static async Task Main(string[] args)
    {
        var clientID = "";
        var pw_jwt = "";
        var url = "";
        int iPort = 0;

        bool argsValid = false;
        if (args.Length == 4)
        {
            clientID = args[0];
            Console.WriteLine("clientID: " + clientID);
            if (args[1] == "pw" || args[1] == "jwt" || args[1] == "pwjwt" || args[1] == "noauth")
            {
                pw_jwt = args[1];
                Console.WriteLine("pw|jwt|pwjwt|noauth: " + pw_jwt);

                url = args[2];
                Console.WriteLine("url: " + url);

                iPort = Convert.ToInt32(args[3]);
                Console.WriteLine("port: " + iPort);

                argsValid = true;
            }
        }
        if (!argsValid)
        {
            Console.WriteLine("Arguments: clientID pw|jwt|pwjwt|noauth url port");
            return;
        }

        var bearer = "eyJhbGciOiJSUzI1NiIsImtpZCI6IkJENTVCQ0RGRDdEQzQzQTZCQUNENDI2RTZFQzFFMThBRUMzQ0UzNzVSUzI1NiIsInR5cCI6ImF0K2p3dCIsIng1dCI6InZWVzgzOWZjUTZhNnpVSnVic0hoaXV3ODQzVSJ9.eyJuYmYiOjE3NDkyMDg4OTAsImV4cCI6MTc0OTIxMjQ5MCwiaXNzIjoiaHR0cHM6Ly8xMjcuMC4wLjE6NTAwMDEiLCJhdWQiOiJyZXNvdXJjZTEiLCJjbGllbnRfaWQiOiJjbGllbnQuand0IiwianRpIjoiRUI5MTMzNDRGRUU0MDg1NkY2MjM2RDc3ODA1N0U1NUYiLCJjZXJ0aWZpY2F0ZSI6Ik1JSURLakNDQWhLZ0F3SUJBZ0lJUU1tdnNxNEhSQTB3RFFZSktvWklodmNOQVFFTEJRQXdQakVMTUFrR0ExVUVCaE1DUkVVeEVUQVBCZ05WQkFvVENFazBNQ0JVWlhOME1Sd3dHZ1lEVlFRREV4TkpOREFnVUdodlpXNXBlQ0JEYjI1MFlXTjBNQjRYRFRJME1EWXhNREEzTURnd01Gb1hEVEkyTURZeE1EQTNNRGd3TUZvd2JERUxNQWtHQTFVRUJoTUNSRVV4RVRBUEJnTlZCQW9UQ0VrME1DQlVaWE4wTVIwd0d3WURWUVFERXhSSk5EQWdRVzVrY21WaGN5QlBjbnBsYkhOcmFURXJNQ2tHQ1NxR1NJYjNEUUVKQVJZY1lXOXllbVZzYzJ0cFFIQm9iMlZ1YVhoamIyNTBZV04wTG1OdmJUQ0NBU0l3RFFZSktvWklodmNOQVFFQkJRQURnZ0VQQURDQ0FRb0NnZ0VCQUpQV3JuZ0Z0eHBKNzVnR0xoTjJuYTRMS3FiZnkycGNoYWV6Y094bDVZaFVEM0xQWFJ1RUJOSmI1YWRhZzJHQW5Sd1VLYlJXSTEwZGloOUdieWVTMzZ0L3Y3TTRKZ243ZWZSOW84Z0NFa2lkRnpaVWZ2L3NoQzlPVzlQZCtUeVVmOVdPbi9pNzgrMkJOeERITGZUR2YzU2FPNFptVWx4NEJOK0lLODBRZ3d1eGI3YjFTSTdMQUVPUU52NlQ3Q2NnQ3FKOUtaamozaVFkVXFGcEhHMmVSSG5jU0IwbGVFa3ZtdGxTM1BDMFY4VVB3L3M5T3lLV056YUtMZTlOUCtaV3FHUmRSeloxc0YrdDIzbnpkVTVhM3l5RGVkVE90MVRuMjFWYVArSXJwS2lKRE1NWXJ0akM2VzFsNkRiaU4yMS9nTFdqR3haS29hd3pJd2V0MkZyaXlkMENBd0VBQVRBTkJna3Foa2lHOXcwQkFRc0ZBQU9DQVFFQUJwNit2REhIaCtjVDFEQmE4MHZjaE8wUlg0UUFWWUc4OHBkMEVzRzdtK1k3ZkFPdXMvNlcvZmpHTnZLbDNHKzVzZ1JKeSsvaGdKWHhQajVrUzM4YkxRWGhybWFySGNDSm5Rc1NCNWNTZG02RE5ZQTFQNnlFcmVmREtJdTFtZzN0WHAwUjBsaWcwQzRDbk84bHhVV0c2Z2VhZjdHUGF2Y0pnRXdudjRobWtZaUo5YWpwaXFLbnZFcE1KVlRnVFdHaXJhV2VZN1V0RzNHd0pPeEVmUDF5aUxpU3UxOGtKbVpDa3VOQ21LMm1NUWZNbTd3MDZIa0tNMFVudC9FU1ZDYkdsSmJxODJ4bDZSUytML2ZhaVFmWkZqUHpEWThTK0NNcE1PZ3ZSNTU3TFgraHZFTE1CUFI4VUFmalJoT0Y5QnhxTzJCWnNkRHYzY1pMclk1T3l6NUVjdz09IiwidXNlck5hbWUiOiJhb3J6ZWxza2lAcGhvZW5peGNvbnRhY3QuY29tIiwic2VydmVyTmFtZSI6ImlkZW50aXR5c2VydmVyLnRlc3QucnNhIiwiaWF0IjoxNzQ5MjA4ODkwLCJzY29wZSI6InJlc291cmNlMS5zY29wZTEifQ.KV0QNMn5o11XkGIQmNTRLbz7u0___usdf43mPxDTMjCWvb3IFINrjFoLNvadI1oSYvHNd9eQqlxusqprCVTk7HFNvgRKc6XLOaz3cwdI4FNStR3y1i5itfEjtigPNBG89rODjhhYkLeDkbAtZy5slKo2i_-eGznQnjil8OMrazHPsZ-2plbfwOz10gUkvnrRWfjQ1v2EcQop9psV4JglgUu3K6s5tkJorVI3U-Vo7vGtM4x3ZYa3tFqqDsDXQDB-5ii4WjB6OJHYbUoVSYzWS1BmOOX3XALDgSiHn7L-uEhWWnTmfcjcL6mkkQ7Hsx4M3QuUqEFpOvGd8fwN9n_WJA";

        var factory = new MqttClientFactory();
        var mqttClient = factory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithClientId(clientID);

        if (pw_jwt == "pw")
        {
            options.WithCredentials("aorzelski@phoenixcontact.com", "aorzelski@phoenixcontact.com");
        }
        if (pw_jwt == "jwt")
        {
            options
            .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
            .WithUserProperty("authorization", $"bearer {bearer}");
        }
        if (pw_jwt == "pwjwt")
        {
            options.WithCredentials("jwt", bearer);
        }
        //.WithTcpServer("localhost", 1883)
        // .WithTcpServer("mqtt-broker.aas-voyager.com", 8883)
        options.WithTcpServer(url, iPort);
        if (url != "localhost")
        {
            options.WithTlsOptions(new MqttClientTlsOptions
            {
                UseTls = true,
                SslProtocol = SslProtocols.Tls12, // oder Tls13, je nach Server
                AllowUntrustedCertificates = false,
                IgnoreCertificateChainErrors = false,
                IgnoreCertificateRevocationErrors = false
            });
        }
        var optionsBuilder = options.WithCleanSession().Build();

        mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            Console.WriteLine();
            Console.WriteLine("ClientID: " + e.ClientId);

            var payload = e.ApplicationMessage?.Payload == null
                ? null
                : Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

            Console.WriteLine("Message received:");
            Console.WriteLine($"  topic: {e.ApplicationMessage?.Topic}");
            Console.WriteLine($"  payload: {payload}");
            return Task.CompletedTask;
        };

        mqttClient.ConnectedAsync += e =>
        {
            Console.WriteLine("Connected with broker.");
            return Task.CompletedTask;
        };

        mqttClient.DisconnectedAsync += e =>
        {
            Console.WriteLine("Unconnected.");
            return Task.CompletedTask;
        };

        await mqttClient.ConnectAsync(optionsBuilder);

        if (mqttClient.IsConnected)
        {
            string[] topics = [ "/noauth/submodels", "/fx/all/submodels", "/fx/domain/phoenixcontact.com/submodels" ];
            var stringList = new List<string> { "first", "second", "third", $"{DateTime.UtcNow}" };

            foreach (var topic in topics)
            {
                await mqttClient.SubscribeAsync(topic);

                var jsonPayload = JsonSerializer.Serialize(stringList);

                // Nachricht senden
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(jsonPayload)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
                    .WithRetainFlag()
                    .Build();

                await mqttClient.PublishAsync(message);
            }

            Console.WriteLine("Message sent. Press key to end …");
            Console.ReadKey();

            await mqttClient.DisconnectAsync();
        }
        else
        {
            Console.WriteLine("Client not connected!");
        }
    }
}
