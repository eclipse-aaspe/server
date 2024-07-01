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
### [V3 2023-11-17.alpha] - 2023-11-17

- First version of EDC connection (Disabled in View)
- Calculate CFP only when BOM changes
- Add security for control cabinet
- Add security for scanner app
- Change PCF architecture
- Upgrade PCF UI
- Support for database
- Show SVG
- New GraphQL query: `SearchSMEsInSubmodel`
- PCF showcase with usage policies
- Merge V3 with database
- Increase multipart file size
- Add submodel to AAS (with query parameter via API)
- Add "PutThumbnail" API
- Disable "--rest" support in command arguments
- Timeseries support with V3
- Update Atex view
- Support for core version of AASX Server with new APIs and Swagger UI

During the conversion of V1 or V2 compatible AASX files, if the files contain empty lists of AAS-resources (e.g., an empty list of keys in a reference), such lists are converted to corresponding null objects (e.g., a null reference).

Currently, the DataSpecification from the EmbeddedDataSpecification is treated as an optional parameter. An inconsistency in the cardinality of DataSpecification has been noticed among the specifications, SwaggerHub definitions, and the aas-core-works rendered HTML pages. An issue has been created for the same.

### [V3 2023-09-13.alpha] - 2023-09-13

This is the first release of AASX Server for V3.

- Supports V3.0.1 of the AAS schema and AAS OpenAPI
- Convert V2 AASX on the fly to V3 AASX
- Connect to AASX File Server API by AASX Package Explorer
- Includes security with V3.0.1
- Includes registry with V3.0.1
- See `/swagger` to show SwaggerUI and supported APIs

The related docker is:
`docker.io/adminshellio/aasx-server-blazor-for-demo:main`

You may run it by:
```bash
docker run
-p 5001:5001
--restart unless-stopped
-v ./aasxs:/AasxServerBlazor/aasxs
docker.io/adminshellio/aasx-server-blazor-for-demo:main
```

### [V2 2022-07-25.alpha] - 2022-07-25

- REST API according to Part 2:
  - Support for AASX File Server Interface
  - Swagger as library
  - Add internal AAS registry
  - POST to external registry
  - Add semanticId to SubmodelRegistry

ZVEI PCF Demo (Product Carbon Footprint):
- Viewer PCF.RAZOR for PCF model
- Add `calculatecfp` to iterate model and add values
- Add `calculatecfp` by REST
- Add `/server/listasset`
- Update PCF for nested BOMs
- Improve PCF viewer with CSS TailWind

Show changes on server:
- Add `DIFFJSON` (to be documented)

TimeSeries Extensions:
- Plot timeseries in browser
- Add plotting filter, create `TimeSeriesPlotting.cs`
- Add qualifiers for `latestData`
- Add filter for timeseries JSON endpoints
- Add `plotRowOffest` for AASX Package Explorer
- Add status parameter to `GETDIFF` and `PUTDIFF`
- Add `limitCount` for collections to prevent continuous addition in `PUTDIFF`
- Add `TimeSeries10` format for Modbus
- Add `posttimeseries` to push data to server
- Improve TimeSeries to allow restart/update of local server
- Get OPC UA server time for timeseries
- Get OPC UA HA from last received timestamp
- Allow any number of OPC UA values in timeseries
- Add HtmlIds in viewer for testing

Miscellaneous:
- Add CORS headers
- Update blazor for updated OPC UA values
- Add `proxy.txt` to specify `proxy.dat`
- Expose docker port to external
- Add route `/aasenvjson`
- Change to `LICENSE.TXT`
- Add IDTA Logo and new AAS Icon
- Add start option `--read-temp`
- Add Verifiable Credential for Nameplate
- Add `TreeComponent` (Tree.razor, treeStyles.css) to `LICENSE.TXT`

### [V2 2022-01-13.alpha] - 2022-01-13

- Add `/aas/` to REST PATH for event messages
- Add `/diff/aas` to REST PATH for HTML diff
- Add REST routes `/aas/#/getveventmessages`

- Update `README.md` (#72)
  - Explain REST API in README

- Change example server to `admin-shell-io.com/5001`
- Only use port 443 on example server to comply with large IT systems

- Add additional email token for IDunion authentication (#79)

- Add keycloak authentication

- Add replicator for submodels (#81)
  - AASX server can now authenticate as client to other AASX servers.
  - After authentication, a submodel can be fetched from one server and sent to another server.
- Add cycleCount and time of next cycle for cyclic task
- Get new token if no longer valid
- Add correct timestamps
- Add security check for path in `PUT` submodelElements
- Add security for submodels

- Fix bug displaying file details in blazor
- Fix bug for blazor display of non-existing file
- Fix bugs for blazor display of empty data

- Remove time correction for OPC UA

### [V2 2021-06-04.alpha] - 2021-06-04

- Turn off HTTPS redirection
  - AASX Server can now run behind a frontend forwarder like nginx. Example: `https://admin-shell-io.com/5011/` using port 443.

- Add new option `--external-rest`
  - Running behind a frontend forwarder may need an internal host:port for the server, but blazor API and REST API will be different.

- Add security rules per submodel element (#70)
  - Now rules can also be defined for `targetObject` submodel element
  - The rules in the element tree are:
    - The nearest elements in the tree up must not be a "deny" but an "allow" or nothing
    - No elements with "deny" are allowed in the subtree below

- Add first version of events (#71)
  - Show timestamp in blazor tree

- Add `/diff` with time and display of `DELETE`, `CREATE`, and `UPDATE`
  - Add message for older deleted items
  - Show diff as HTTP table
  - Path `/diff/updates` can also show values of properties
  - See example `https://admin-shell-io.com:5031/`

### [V2 2021-05-08.alpha] - 2021-05-08

- Add `OPCWRITE` (by Qualifier `OPCWRITE` in OPC submodel)

- Add CORS header for 3D viewer

- Show external 3D model by iframe in blazor view

### [V2 2021-05-02.alpha] - 2021-05-02

- Extend security according to security meta model

- Push endpoints into RIC Python registry

- Add route to HTTP server for BaSyx-style of getting submodel elements

- Add get AASX by `AssetId` (#62)

- Update base docker images (#64)
- Make blazor docker rely on asp.net docker (#65)

- Add timeseries data (#68)
  - Start and stop cloud recording of time series

- Add `GET /authserver` to authenticate before `GET /server/listaas`

- Correct blazor AASX download
- Correct path for QR code

- Update `getaasx` with operational data

- Change read thumbnail
- Change to AASX buffered by TEMP file

- Fix blazor update bug with multiple browser windows

- Show product image in `getaasxbyassetid`

### [V2 2021-02-04.alpha] - 2021-02-04

- REST Query parameter `refresh=seconds`

- Add rest values for collections

- Delete `Lutz_Test_Root_CA.cer`

- Add any JSON as AAS collection at startup
  - By qualifier `GetJSON` with URL, any JSON response can be inserted into a submodel.

- Add OPC client read with numeric NodeId
  - If an `idshort` includes a `#` sign, the part after `#` is used as numeric NodeId. `OPCPATH` is not used then.

- Limit I40 language message output
  - Number of messages is limited to 100. `+++` added then.

- Add connect block transfer (#53)
  - Make connect transfer of large files in several blocks (#53)
  - Also increase connect speed during block transfer

- Add `put aasx` (#54)
  - `Put /server/getaasx` for updated .AASX
  - Recursive online value retrieval

- Fix OPC UA server NodeIDs with hierarchical names
- Fix OPC server hierarchical names extended

- Add `PUT AASX` (#55)
  - Delete OPC UA duplicates
  - Fix OPC UA hierarchical names

- Update of AASX on server by AASX Package Explorer (#56)

### [V2 2020-11-29.alpha] - 2020-11-29

- Add update of dynamic tree
- Delete latest change (commented

out)
- Add registry information
- Add option `--name`
- Add URL-coded AssetID in blazor view
- Remove hack
- Update order of options
- Improve message in `CheckHelpInReadme` (#52)
- Add QR code to blazor view
- Add missing copyright

### [V2 2020-11-12.alpha] - 2020-11-12

- Change authentication for REST `/server/getaasx2`

  - If `--no-security` is set, an .AASX can be retrieved by `getaasx2`.
  - This works together with file `/connect` in AASX Package Explorer.

### [V2 2020-11-01.alpha] - 2020-11-01

- `AasxServerWindows.exe` is running now again
- Provide docker files for x64, ARM32, and ARM64
- Change method to read .AASX as readonly
- GET property value from any endpoint
- Publish dockers only on master and releases (#43)
- First version of generalized I40 language
- Convert AasxServerWindows project to SDK-style (#47)
- Test `assetId` address for humans and machine

### [V2 2020-09-16.alpha] - 2020-09-16

- Add redirect (#33)
- Extend picture preview by .png
- Create subtrees in browser view only for expanded nodes: much faster performance
- Set define `UseAasxCompatibilityModels` (to V10) always true
- Improve browser tree view
- Update license printout and check empty directories
- Set V10 compatibility and copy subdirectories
