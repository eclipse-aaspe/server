# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Footer to [MainLayout.razor] that displays copyright and current server version as information. (@Freezor)
- Splitter between the Tree and the Node information, that can be adjusted by the user to change the width of each container. (@Freezor)

### Changed

- Renamed Program1.cs to BlazorServerStarter for better readability and to avoid misunderstandings, as there already is a Program class. (@Freezor)
- Cleaned BlazorServerStarter in general to have an easier understanding on the process (@Freezor)
- Extracted dependency registration into DependencyRegistry.cs and server configuration into ServerConfiguration.cs from Startup.cs (@Freezor)
- Refactored ServerConfiguration.cs into smaller parts and applying Clean code and SOLID principles. (@Freezor)
- Extracted [CarbonFootprintShowcaseLayout.razor] from MainLayout for 'Separation of Concerns'. (@Freezor)
- Refactored [MainLayout.razor] to have a smaller and clearer layout. (@Freezor)
- Cleaned [NavMenu.razor] to apply common code style. (@Freezor)
- Improved the lay-outing in [MainLayout.razor] for easier future development. (@Freezor)
- [MainLayout] header to show a small shadow and make it better stand out. (@Freezor)

### Updated

- Microsoft.IdentityModel.Tokens from **6.13.1** to **7.5.0** because of a package vulnerability.

## [Released]

### [x.x.x] - yyyy-mm-dd

Here comes the next release version.
