# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Updated

- **AasxServerStandardBib**: Microsoft.IdentityModel.Tokens from **6.13.1** to **7.5.0** because of a package vulnerability. (@Freezor)
  - Fixing open security issue: CVE-2024-21319
- **AasxServerStandardBib**: Grapevine from **4.1.2** to **4.2.2**. (@Freezor)
- **AasxServerStandardBib**: IdentityModel from **5.1.1** to **7.0.0**. (@Freezor)
- **AasxServerStandardBib**: jose-jwt from **2.5.0** to **5.0.0**. (@Freezor)
- **AasxServerStandardBib**: MailKit from **3.2.0** to **4.6.0**. (@Freezor)
- **AasxServerStandardBib**: Microsoft.AspNetCore.Components from **3.1.2** to **8.0.5**. (@Freezor)
- **AasxServerStandardBib**: Microsoft.EntityFrameworkCore.Sqlite from **7.0.5** to **7.0.19**. (@Freezor)
- **AasxServerStandardBib**: Microsoft.EntityFrameworkCore.Tools **7.0.5** to **7.0.19**. (@Freezor)
- **AasxServerStandardBib**: Microsoft.IdentityModel.Tokens **7.5.0** to **7.6.0**. (@Freezor)
- **AasxServerStandardBib**: Npgsql.EntityFrameworkCore.PostgreSQL **7.0.4** to **7.0.18**. (@Freezor)
- **AasxServerStandardBib**: OPCFoundation.NetStandard.Opc.Ua **7.0.4** to **7.0.18**. (@Freezor)
- **AasxServerStandardBib**: OSystem.IdentityModel.Tokens.Jwt **6.13.1** to **7.5.0**. (@Freezor)
- **AasxServerStandardBib**: System.Security.Permissions **6.0.0** to **8.0.0**. (@Freezor)
- **AasxServerStandardBib**: jose-jwt **2.5.0** to **5.0.0**. (@Freezor)
- **AasxServerBlazor**: HotChocolate.AspNetCore **13.2.1** to **13.9.4**. (@Freezor)
- **AasxServerBlazor**: Microsoft.AspNetCore.Components **3.1.2** to **6.0.30**. (@Freezor)
- **AasxServerBlazor**: Microsoft.EntityFrameworkCore.Tools **7.0.5** to **7.0.19**. (@Freezor)
- **AasxServerBlazor**: Swashbuckle.AspNetCore **6.5.0** to **6.6.2**. (@Freezor)

### Changed

- Renamed Program1.cs to BlazorServerStarter for better readability and to avoid misunderstandings, as there already is a Program class. (@Freezor)
- Cleaned BlazorServerStarter in general to have an easier understanding on the process (@Freezor)
- Extracted dependency registration into DependencyRegistry.cs and server configuration into ServerConfiguration.cs from Startup.cs (@Freezor)
- Refactored ServerConfiguration.cs into smaller parts and applying Clean code and SOLID principles. (@Freezor)

## [Released]

### [x.x.x] - yyyy-mm-dd

Here comes the next release version.
