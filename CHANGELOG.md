# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Test Project for `IO.Swagger.Lib.V3`. (@Freezor)
- New unit tests for `JsonSerializerStrategy`, `RequestMetadataMapper`, `ValueObjectParser`, `RequestValueMapper`, `ResponseValueMapper`, `ResponseValueTransformer`, `ValueOnlyJsonDeserializer`, and `ValueOnlyJsonSerializer`. (@Freezor)

### Changed

- Refactored `SerializationModifiersValidator` for better testability and introduced `ISerializationModifiersValidator`. (@Freezor)
- Refactored `JsonSerializerStrategy` for better testability and introduced `IJsonSerializerStrategy`. (@Freezor)
- Simplified `AssetAdministrationShellService.IsSubmodelPresentWithinAAS` to reduce complexity overhead. (@Freezor)
- Refactored `AasResponseFormatter` and extracted JSON formatting into `IJsonSerializerStrategy`. (@Freezor)
- Refactored `MappingService` for simpler, more understandable code. (@Freezor)
- Refactored `RequestMetadataMapper` to apply best practices and design patterns. (@Freezor)
- Refactored `SerializationModifiersValidator` to apply best practices and design patterns. (@Freezor)
- Renamed Program1.cs to BlazorServerStarter for better readability and to avoid misunderstandings, as there already is a Program class. (@Freezor)
- Cleaned BlazorServerStarter in general to have an easier understanding on the process (@Freezor)
- Extracted dependency registration into DependencyRegistry.cs and server configuration into ServerConfiguration.cs from Startup.cs (@Freezor)
- Refactored ServerConfiguration.cs into smaller parts and applying Clean code and SOLID principles. (@Freezor)

### Updated

- Microsoft.IdentityModel.Tokens from **6.13.1** to **7.5.0** because of a package vulnerability. (@Freezor)

### Removed

- Unused method `EnumeratesChildren` in `ExtendIReferable`. (@Freezor)

## [Released]

### [x.x.x] - yyyy-mm-dd

Here comes the next release version
