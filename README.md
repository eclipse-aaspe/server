AASX Server - based on code of AASX Package Explorer

Currenty uses .NET Core Preview 3
https://dotnet.microsoft.com/download/dotnet-core/3.0
Runtime 3.0.0-preview3-27503-5

Copy opc*.xml into execution directory

Start with switch --help to see switches (or check in program.cs run())

Server loads all .AASX files in the execution directory
Server can be accessed by REST, OPC UA or MQTT (see console)
Every 5 seconds the next submodel is published by MQTT (see console)

If it runs on a PC as localhost
- Access REST on localhost:51310
    - e.g. http://localhost:51310/server/listaas
    - e.g. http://localhost:51310/aas/id/complete
    - e.g. http://localhost:51310/aas/id/submodels/CAD/complete
- Access OPC UA on opc.tcp://localhost:51210/UA/SampleServer

