# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Test Project for `IO.Swagger.Lib.V3`.
- New unit tests for `JsonSerializerStrategy`, `RequestMetadataMapper`, `ValueObjectParser`, `RequestValueMapper`, `ResponseValueMapper`, `ResponseValueTransformer`, `ValueOnlyJsonDeserializer`, and `ValueOnlyJsonSerializer`.

### Changed

- Refactored `SerializationModifiersValidator` for better testability and introduced `ISerializationModifiersValidator`.
- Refactored `JsonSerializerStrategy` for better testability and introduced `IJsonSerializerStrategy`.
- Simplified `AssetAdministrationShellService.IsSubmodelPresentWithinAAS` to reduce complexity overhead.
- Refactored `AasResponseFormatter` and extracted JSON formatting into `IJsonSerializerStrategy`.
- Refactored `MappingService` for simpler, more understandable code.
- Refactored `RequestMetadataMapper` to apply best practices and design patterns.
- Refactored `SerializationModifiersValidator` to apply best practices and design patterns.

### Removed

- Unused method `EnumeratesChildren` in `ExtendIReferable`.

## [Released]

### [x.x.x] - yyyy-mm-dd

Here comes the next release version