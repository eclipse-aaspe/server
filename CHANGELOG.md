# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- Renamed Program1.cs to BlazorServerStarter for better readability and to avoid misunderstandings, as there already is a Program class.(@Freezor)
- Cleaned BlazorServerStarter in general to have an easier understanding on the process(@Freezor)
- Extracted dependency registration into DependencyRegistry.cs and server configuration into ServerConfiguration.cs from Startup.cs(@Freezor)
- Refactored ServerConfiguration.cs into smaller parts and applying Clean code and SOLID principles.(@Freezor)
- Renamed AasxServer.Program.signalNewData to Program.SignalNewData (@Freezor)
- Renamed AasxServer.Program.NewDataAvailableArgs to Program.NewDataAvailableEventArgs, in order to apply common naming scheme for EventArgs classes (@Freezor)
- Renamed AasxServer.Program.getDataVersion to Program.GetDataVersion (@Freezor)
- Renamed AasxServer.Program.con to Program.Configuration (@Freezor)
- Renamed AasxServer.Program.saveEnv to Program.SaveEnvironment (@Freezor)
- Renamed AasxServer.Program.loadPackageForAas to Program.LoadPackageForAas (@Freezor)
- Renamed AasxServer.Program.loadPackageForSubmodel to Program.LoadPackageForSubmodel (@Freezor)
- Renamed AasxServer.Program.parseJson to Program.ParseJson (@Freezor)
- Renamed AasxServer.Program.changeDataVersion to Program.ChangeDataVersion (@Freezor)
- Renamed AasxServer.Program.creatAASDescriptor to Program.CreateAASDescriptor (@Freezor)
- Renamed AasxServer.Program.publishDescriptorData to Program.PublishDescriptorData (@Freezor)
- Renamed AasxServer.Program.connectThreadLoop to Program.ConnectThreadLoop (@Freezor)
- Renamed AasxServer.Program.getDataVersion to Program.GetDataVersion (@Freezor)
- Renamed AasxServer.aasDirectoryParameters to AasDirectoryParameters (@Freezor)

### Updated

- Microsoft.IdentityModel.Tokens from **6.13.1** to **7.5.0** because of a package vulnerability.(@Freezor)

### Removed

- Unused methods in AasxServer.Program: GetBetween, createDbFiles, OnRestTimedEvent (@Freezor)

## [Released]

### [x.x.x] - yyyy-mm-dd

Here comes the next release version.
