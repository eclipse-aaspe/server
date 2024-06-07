# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- Renamed Program1.cs to BlazorServerStarter for better readability and to avoid misunderstandings, as there already is a Program class. (@Freezor)
- Cleaned BlazorServerStarter in general to have an easier understanding on the process (@Freezor)
- Extracted dependency registration into DependencyRegistry.cs and server configuration into ServerConfiguration.cs from Startup.cs (@Freezor)
- Refactored ServerConfiguration.cs into smaller parts and applying Clean code and SOLID principles. (@Freezor)

### Updated

- Microsoft.IdentityModel.Tokens from **6.13.1** to **6.35.0** because of a package vulnerability. (@Freezor)

### Removed

- **Removed all references to OPC UA** (@Freezor)
    - Removed files:
        - `AasEntityBuilder.cs`
        - `AasNodeManager.cs`
        - `AasUaEntities.cs`
        - `AasUaEntities.cs.bak`
        - `AasUaEntityFileType.cs`
        - `AasUaNodeHelper.cs`
        - `AasUaUtils.cs`
        - `AasxUaServerOptions.cs`
        - `DataChangeMonitoredItem.cs`
        - `MonitoredItemQueue.cs`
        - `MonitoredNode.cs`
        - `Opc.Ua.SampleClient.Config.xml`
        - `SampleNodeManager.cs`
        - `SampleServer.SampleModel.cs`
        - `SampleServer.UserAuthentication.cs`
    - **Paths**: `src/AasxServerStandardBib/`
- **Removed unused and incomplete attribute class** (@Freezor)
    - Removed file:
        - `src/AasxServerStandardBib/`
- **Removed EnergyModel references (demo showcase)** (@Freezor)
    - Removed files:
        - `EnergyModel.cs`
        - `EnergyModel_SourceSystem_Azure.cs`
        - `PrefEnergyModel10.cs`
    - **Path**: `src/AasxServerStandardBib/`
- **Removed remaining references to GrapeVineLogger** (@Freezor)
    - Removed file:
        - `GrapevineLoggerConsumers.cs`
    - **Path**: `src/AasxServerStandardBib/`
- **Removed I40Message Broker (test implementation)** (@Freezor)
    - Removed file:
        - `I40Message.cs`
    - **Path**: `src/AasxServerStandardBib/`
- **Removed MQTT Client/Server (not fully implemented)** (@Freezor)
    - Removed files:
        - `MqttClient.cs`
        - `MqttServer.cs`
    - **Path**: `src/AasxServerStandardBib/`
- **Removed other unused files** (@Freezor)
    - `MultiTupleDictionary.cs`
    - `NodeStateCollection.cs`
    - `Program.cs`
    - **Path**: `src/AasxServerStandardBib/`

## [Released]

### [x.x.x] - yyyy-mm-dd

Here comes the next release version.
