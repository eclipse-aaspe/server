using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AdminShellNS;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
   Copyright (c) 2019 Phoenix Contact GmbH & Co. KG <opensource@phoenixcontact.com>, author: Andreas Orzelski
   Copyright (c) 2019 Fraunhofer IOSB-INA Lemgo, eine rechtlich nicht selbständige Einrichtung der Fraunhofer-Gesellschaft
    zur Förderung der angewandten Forschung e.V. <florian.pethig@iosb-ina.fraunhofer.de>, author: Florian Pethig

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

/* For Mqtt Content:

MIT License

MQTTnet Copyright (c) 2016-2019 Christian Kratky
*/

namespace AasxMqttClient
{
    using System.Text.Json;

    public class MqttClient
    {
        public MqttClient()
        {

        }

        static int lastAASEnv;
        static int lastSubmodel;
        public static async Task StartAsync(AdminShellPackageEnv[] package, GrapevineLoggerSuper logger = null)
        {
            // Create TCP based options using the builder.
            var options = new MqttClientOptionsBuilder()
                .WithClientId("AASXPackageXplorer MQTT Client")
                .WithTcpServer("localhost", 1883)
                .Build();

            //create MQTT Client and Connect using options above
            IMqttClient mqttClient = new MqttFactory().CreateMqttClient();
            await mqttClient.ConnectAsync(options);

            int iAASEnv = 0;
            for (iAASEnv = 0; iAASEnv < package.Length; iAASEnv++)
            {
                if (iAASEnv == lastAASEnv && package[iAASEnv] != null)
                {
                    //publish AAS to AAS Topic
                    foreach (AssetAdministrationShell aas in package[iAASEnv].AasEnv.AssetAdministrationShells)
                    {


                        //publish submodels
                        int iSubmodel = 0;
                        foreach (var sm in package[iAASEnv].AasEnv.Submodels)
                        {
                            if (iSubmodel == lastSubmodel)
                            {
                                Console.WriteLine("Publish MQTT AAS " + aas.IdShort + " Submodel_" + sm.IdShort);

                                var message2 = new MqttApplicationMessageBuilder()
                                                .WithTopic("Submodel_" + sm.IdShort)
                                                .WithPayload(JsonSerializer.Serialize(sm))
                                                .WithExactlyOnceQoS()
                                                .WithRetainFlag()
                                                .Build();

                                await mqttClient.PublishAsync(message2);
                                lastSubmodel++;
                                iSubmodel = -1;
                                break;
                            }
                            iSubmodel++;
                        }
                        if (iSubmodel != -1)
                        {
                            lastSubmodel = 0;
                            lastAASEnv++;
                        }
                        break;
                    }
                    break;
                }
            }
            if (package[lastAASEnv] == null)
            {
                lastAASEnv = 0;
            }
        }
    }
}
