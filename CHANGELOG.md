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
- Applied general fixes in naming, layout, and applied Resharper suggestions across the AasxServerBlazor project to adhere to best practices and enhance future
  maintainability. (@Freezor)
- Changed TargetFramework from Net6.0 to Net8.0 because of library issues and not runnable docker images. (@Freezor)

### Removed

- I40Languages.cs and I40Messages.cs removed (@Freezor)
- References and usings of Newtonsoft in favor of a complete usage of System.Text.Json in the whole project. (@Freezor)

### Updated

- Microsoft.IdentityModel.Tokens from **6.13.1** to **6.34.0** because of a package vulnerability. (@Freezor)

## [Released]

### [x.x.x] - yyyy-mm-dd

Here comes the next release version.
