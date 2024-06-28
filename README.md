> [!IMPORTANT]
> This repository has been moved alongside Eclipse AASX Package Explorer to [eclipse-aaspe](https://github.com/eclipse-aaspe) organisation.  
> Branches and issues will be unaffected.
> All links to the previous repository location are automatically redirected to the new location, e.g. when using `git clone`, `git push` etc.
> It is, however, recommended to update the `origin` of any clones to avoid confusion.

# Eclipse AASX Server

> ### Status
> [![Create Prerelease on Merge to Main](https://github.com/eclipse-aaspe/server/actions/workflows/prerelease-on-merge-to-main.yml/badge.svg)](https://github.com/eclipse-aaspe/server/actions/workflows/prerelease-on-merge-to-main.yml)<br>
> [![Draft Release on Merge to Release Branch](https://github.com/eclipse-aaspe/server/actions/workflows/draft-release-on-merge-to-release-branch.yml/badge.svg?branch=release)](https://github.com/eclipse-aaspe/server/actions/workflows/draft-release-on-merge-to-release-branch.yml)<br>
> [![Build and publish docker images when release is published](https://github.com/eclipse-aaspe/server/actions/workflows/build-and-publish-docker-images.yml/badge.svg)](https://github.com/eclipse-aaspe/server/actions/workflows/build-and-publish-docker-images.yml)<br>
> [![Code Style & Security Analysis](https://github.com/eclipse-aaspe/server/actions/workflows/code-analysis.yml/badge.svg)](https://github.com/eclipse-aaspe/server/actions/workflows/code-analysis.yml)<br>
> 
> ![GitHub repo size](https://img.shields.io/github/repo-size/eclipse-aaspe/server) ![GitHub code size in bytes](https://img.shields.io/github/languages/code-size/eclipse-aaspe/server)
> ### Docker Images
> [![Docker Pulls](https://img.shields.io/docker/pulls/adminshellio/aasx-server-aspnetcore-for-demo-arm32?label=aasx-server-aspnetcore-for-demo-arm32)](https://hub.docker.com/r/adminshellio/aasx-server-blazor-for-demo-arm32)<br>
> [![Docker Pulls](https://img.shields.io/docker/pulls/adminshellio/aasx-server-blazor-for-demo-arm64?label=aasx-server-blazor-for-demo-arm64)](https://hub.docker.com/r/adminshellio/aasx-server-blazor-for-demo-arm64)<br>
> [![Docker Pulls](https://img.shields.io/docker/pulls/adminshellio/aasx-server-blazor-for-demo-arm32?label=aasx-server-blazor-for-demo-arm32)](https://hub.docker.com/r/adminshellio/aasx-server-blazor-for-demo-arm32)<br>
> [![Docker Pulls](https://img.shields.io/docker/pulls/adminshellio/aasx-server-blazor-for-demo?label=aasx-server-blazor-for-demo)](https://hub.docker.com/r/adminshellio/aasx-server-blazor-for-demo)<br>
>
> ### Important
> We currently use Dotnet Version ![.NET Version](https://img.shields.io/badge/dotnet-8.0-blue)


AASX Server is a companion app for the [AASX Package Explorer](). It provides a local service to host and serve Industrie 4.0 AASX packages. The Core version
exposes endpoints for REST, OPC UA, and MQTT protocols. The GUI version offers the same functionality and additionally uses the Blazor Framework to provide a
browser-based GUI for exploring AASX packages.

> **IMPORTANT**
>
> AASX Server is now in V3, and the `main` branch includes the first release:  
> [AASX Server v2023-09-13.alpha](https://github.com/admin-shell-io/aasx-server/releases/tag/v2023-09-13.alpha)  
> The latest work takes place in the `policy3` branch, which will be merged into `main` soon.

> **TIP**
>
> A demo server is running at [https://v3.admin-shell-io.com](https://v3.admin-shell-io.com).  
> You can explore the API manually at [https://v3.admin-shell-io.com/swagger](https://v3.admin-shell-io.com/swagger).

An AASX Server with security enabled can be found here: https://v3security.admin-shell-io.com/.

## How-to

Currently, **AasxServerBlazor** is primarily used, but **AasxServerCore** is also supported. **AasxServerWindows** will no longer be developed, as .NET 6 works
well on Windows. The `--rest`, `--host`, and `--port` options are no longer supported and will be removed soon, as they pertain to the old V2 API.

Please ignore the "**Connect to REST by:**" message.

You can place your AASXs into the `./aasxs` directory. In the examples below, replace **YOURPORT** and **YOURURL** with your actual port and URL.

### Running AASX Server with .NET

You can run the AASX server directly using the `dotnet` command:

```sh
export DOTNET_gcServer=1  
export Kestrel__Endpoints__Http__Url=http://*:YOURPORT  
dotnet AasxServerBlazor.dll --no-security --data-path ./aasxs --external-blazor YOURURL  
```

> Note: ASP.NET Core Runtime 8.0 can be downloaded [here](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

### Running AASX Server with Docker

You can use the Docker image available at:  
[`docker.io/adminshellio/aasx-server-blazor-for-demo`](https://hub.docker.com/r/adminshellio/aasx-server-blazor-for-demo)

This image includes several tags for different purposes:
- `latest`: Latest stable release version.
- `main`: Nightly build from the main branch (unstable).
- `develop`: Build from any development state (highly unstable and potentially broken).
- Version-specific tags: You can pull a specific version of the container.

Place your AASXs into the `./aasxs` directory and run the Docker container with:

```sh
docker run \
-p 5001:5001 \
--restart unless-stopped \
-v ./aasxs:/AasxServerBlazor/aasxs \
docker.io/adminshellio/aasx-server-blazor-for-demo:main
```

### Using Docker Compose

If you prefer to use Docker Compose, see the `docker-compose.yaml` configuration below:

```yaml
services:
  aasx-server:
    container_name: aasx-server
    image: docker.io/adminshellio/aasx-server-blazor-for-demo:main
    restart: unless-stopped
    ports:
      - YOURPORT:5001
    environment:
      - Kestrel__Endpoints__Http__Url=http://*:5001
    volumes:
      - ./aasxs:/usr/share/aasxs
    command: --no-security --data-path /usr/share/aasxs --external-blazor YOURURL  
```

### Persistence

The V3 version of the server includes a basic implementation of persistence using a database. We use Entity Framework, which has been tested with SQLite and
PostgreSQL. SQLite is part of the standard deployment. (PostgreSQL details will be explained in the README in the future.)

Add `--with-db` to turn on database storage.
For the first start please add "`--start-index 0`" to get the AASX files in `--data-path` imported into the database.
For further starts add "`--start-index number`" with number greater than you number of AASX files, e.g. 1000.
If you change content by the API, you may add "`--save-temp number_of_seconds`" and the changes will be written to the database after the **number_of_seconds**.
With "`--aasx-in-memory number`" you can specify how many AAS shall be shown in the blazor tree. Only the latest changed AAS will be shown.

Click on the links on the right.
You can find an example server with database running here: [Example Server](https://cloudrepo.h2894164.stratoserver.net).
The database content can be seen here: [Database Content](https://cloudrepo.h2894164.stratoserver.net/db).
You may also do GraphQL queries to the database here: [GraphQL Queries](https://cloudrepo.h2894164.stratoserver.net/graphql/). On the GraphQL page, enter `{`
followed by a space, and the wizard will guide you further.
An example GraphQL query is:

```graphql
{
   searchSubmodels (semanticId: "https://admin-shell.io/zvei/nameplate/1/0/Nameplate")
   {
     submodelId
     url
   }
}
```

If you want to create a registry and also automatically POST to it, please take a look at
our [GitHub issues](https://github.com/admin-shell-io/aasx-server/issues) page.

## CREATE NEW RELEASES

We've transitioned to [semantic versioning](https://semver.org) for better version distinctness. All versions follow this schema:

```
<major>.<minor>.<patch>.<buildnumber>-<AAS Schema Version>-<alpha>-<stable|latest|develop>
```

- **buildnumber**: An incremented value for each build, crucial for distinguishing between builds, particularly for development or latest releases without new version numbers.
- **AAS Schema Version**: Indicates the AAS main schema used in this version.
- **alpha**: Denotes an alpha build, indicating it's not yet a finished release.
- **stable**: Represents the latest stable release, confirming that main features are working.
- **latest**: Indicates the most recent build on the main branch, generally stable but may have minor issues.
- **develop**: Refers to builds from branches other than main or develop, primarily for testing and potentially unstable.

### Release a New Version

With the switch to semantic versioning, our release process has been enhanced:

1. **Update the Changelog**
   - Move all recent changes to the [Released] section in the [changelog](CHANGELOG.md).
   - Determine the new version number based on semantic versioning and include the release date.
   
2. **Update the Version Configuration**
   - Update the [current_version.cfg](src/current_version.cfg) with the new version number.

3. **Push Changes**
   - Push these changes to the new branch, you made from the `main` branch state you want to release.

4. **Create a New PR to Release Branch**
   - Submit a pull request targeting the release branch. Ensure all necessary details are provided.

5. **Rebase to Main**
   - After the PR is merged into the release branch, rebase these changes onto the main branch. This ensures consistency across branches and updates [current_version.cfg](src/current_version.cfg) and [changelog](CHANGELOG.md) on the main branch.

Once the branch is merged into the release branch, GitHub Workflows will **automatically** initiate, creating a new draft release. Review the release to confirm everything is in order before publishing it in the release settings.

Docker image releases are handled automatically at this stage.

### Nightly Releases

We employ a cron job to check nightly for changes on the main branch. If changes are detected, it creates a new prerelease `latest alpha` build. This process automatically assigns a new version number, creates a tag, and releases the corresponding Docker images. A changelog is also automatically generated based on PR changes; however, direct merges into main are not included in this changelog.

You can manually trigger this process using the workflow [here](https://github.com/eclipse-aaspe/server/actions/workflows/prerelease-on-merge-to-main.yml).

### Example Version Tags
- `v1.0.0.1-aasV3-alpha-develop`: Alpha build on a develop branch.
- `v1.0.0.2-aasV3-alpha-stable`: Stable release.
- `v1.0.0.3-aasV3-alpha-latest`: Latest build on the main branch.

---

## OLD DOCUMENTATION

This documentation will be updated to V3 soon.

AASX Server serves Industrie 4.0 AASX packages accessible by REST, OPC UA and
MQTT protocols.

The AASX Server is based on code of [AASX Package Explorer](https://github.com/admin-shell-io/aasx-package-explorer).

There are three variants of the server:

* **blazor**. This variant uses Blazor framework to provide a graphical user
  interface in the browser for exploring the AASX packages. The other APIs
  are the same as in the *core* variant.

* **core**. This is a server based on .NET Core 3.1.

* **windows**. This variant uses .NET Framework 4.7.2, which is the only way
  how you can start a server on your Windows machine without administrator privileges.
  If you run on windows start with this variant first and try *blazor* later.

* Mind that *blazor* and *core* variants require administrator privileges, so they
  can not be used for demonstration purposes on tightly-administered machines
  (which are wide-spread in larger organizations and enterprises).

A blazor demo server is running on https://admin-shell-io.com/5001/.
Please click on an AAS and use the DOWNLOAD button on the right or
use "https://admin-shell-io.com/51411/server/getaasx/0" etc. by browser
or CURL on the command line.
You can connect to this AASX Server by AASX Package Explorer by
"File / AASX File Repository / Connect HTTP/REST repository" with
REST endpoint "https://admin-shell-io.com/51411".

## Binaries

The binaries are available in the [Releases section](
https://github.com/admin-shell-io/aasx-server/releases
). We provide portable dotnet assemblies.

### Installation

AASX Server depends on .NET Core 3.1 runtime (`blazor` and `core` variants)
and .NET Framework (`windows` variant), respectively. You need to install the
respective runtimes before you start the server. .NET framework is part of windows.
See https://dotnet.microsoft.com/download/dotnet-core/3.1

To deploy the binaries, simply extract the release bundle (*e.g.*,
`AasxServerWindows.zip` or `AasxServerCore.zip`) somewhere on your system.

### Running for Demonstration

We include an example AASX and various extra files (*e.g.*, certificates) in
the release bundle so that you can readily start the server for demonstration
purposes. The scripts `startForDemo.sh` and `startForDemo.bat` will start the
server with these pre-packaged files.

For example, if you run on Linux, change to the directory where you extracted
the release bundle and invoke:

```bash
./startForDemo.sh
```

On Windows please start ```startForDemo.bat```.

We provide a couple of sample admin shells (packaged as .aasx) for you to test
and play with the software at: http://www.admin-shell-io.com/samples/.
Please copy these to the ```aasxs``` subdirectory as needed.

### Running on Windows

1. Change to the directory where you extracted the release bundle.

2. Start the server by running `startForDemo.bat` or by invoking the executable directly with the appropriate server variant. For example:

   ```sh
   AasxServerWindows.exe --opc --rest --data-path /path/to/aasxs
   ```

You can see the AAS on the server with: http://localhost:51310/server/listaas.
To show the JSON of the exampleMotor AAS please use: http://localhost:51310/aas/ExampleMotor.
To show submodel "Identification" please use: http://localhost:51310/aas/ExampleMotor/submodels/Identification/complete.

<!--- Help starts. -->

### Options

To obtain help on individual flags and options, supply the argument `--help`:

```sh
AasxServerWindows.exe --help
```

or

```sh
AasxServerCore.exe --help
```

```sh
AasxServerCore:
  Serve AASX packages over different interfaces

Usage:
  AasxServerCore [options]

Options:
  -h, --host <host>                      Host which the server listens on [default: localhost]
  -p, --port <port>                      Port which the server listens on [default: 51310]
  --https                                If set, opens SSL connections. Make sure you bind a certificate to the port before.
  --data-path <data-path>                Path to where the AASXs reside
  --rest                                 If set, starts the REST server
  --opc                                  If set, starts the OPC server
  --mqtt                                 If set, starts a MQTT publisher
  --debug-wait                           If set, waits for Debugger to attach
  --opc-client-rate <opc-client-rate>    If set, starts an OPC client and refreshes on the given period (in milliseconds)
  --connect <connect>                    If set, connects to AAS connect server. Given as a comma-separated values (server, node name, period in milliseconds) or as a flag (in which case it connects to a default server).
  --proxy-file <proxy-file>              If set, parses the proxy information from the given proxy file
  --no-security                          If set, no authentication is required
  --edit                                 If set, allows edits in the user interface
  --name <name>                          Name of the server
  --version                              Show version information
  -?, -h, --help                         Show help and usage information
```

<!--- Help ends. -->

### Running on Linux

Change to the directory where you extracted the release bundle.

Start ```startForDemo.bat``` or use `dotnet` to execute the DLL with the same name
as the server variant. For example:

```sh
dotnet AasxServerCore.dll --opc --rest --data-path /path/to/aasxs
```

### Mono

You can use AasxServerWindows with Mono. Change to the directory where you extracted the release bundle.
Use `mono` to execute the EXE:

```
mono AasxServerWindows.exe --rest --data-path /path/to/aasxs
```

If you want to also use "--opc" with Mono you need to change Opc.Ua.SampleServer.Config.xml:
change `<StoreType>X509Store</StoreType>` to `<StoreType>Directory</StoreType>`.

Mono gives you the possibility to run AasxServer on platforms like x86, PowerPC or MIPS.

See supported Mono platforms on: https://www.mono-project.com/docs/about-mono/supported-platforms/

Find Mono downloads on: https://www.mono-project.com/download/stable/

### Build and Package Binaries

To build the binaries from the source code, run the powershell script
[`src/BuildForRelease.ps1`](src/BuildForRelease.ps1).

To package the binaries for release, call [`src/PackageRelease.ps1`](
src/PackageRelease.ps1).

For more information on continuous integration, see
[.github/workflows/build-and-package-release.yml](
.github/workflows/build-and-package-release.yml
) for a workflow executed on each release and
[.github/workflows/check-release.yml](.github/workflows/check-release.yml) for
a workflow which is executed on each push to master branch.


# Docker Containers for Demonstration

We provide pre-built Docker images for demonstration purposes at the following DockerHub repositories:

### Blazor Variants
* `blazor` [linux/amd64](https://hub.docker.com/r/adminshellio/aasx-server-blazor-for-demo)
* `blazor` [linux/arm32](https://hub.docker.com/r/adminshellio/aasx-server-blazor-for-demo-arm32)
* `blazor` [linux/arm64](https://hub.docker.com/r/adminshellio/aasx-server-blazor-for-demo-arm64)

### Core Variants
* `core` [linux/amd64](https://hub.docker.com/r/adminshellio/aasx-server-core-for-demo)
* `core` [linux/arm32](https://hub.docker.com/r/adminshellio/aasx-server-core-for-demo-arm32)
* `core` [linux/arm64](https://hub.docker.com/r/adminshellio/aasx-server-core-for-demo-arm64)

### Tags Information
Each Docker image includes the following tags:
- `latest`: Latest stable release version.
- `main`: Nightly build from the main branch (unstable).
- `develop`: Build from any development state (highly unstable and potentially broken).
- Version-specific tags: You can pull a specific version of the container.

### Multi-Architecture Support
To facilitate deployment on Raspberry Pi or other architectures, we aim to create multi-arch Docker containers. If you have experience or would like to contribute, please let us know by [creating an issue](https://github.com/admin-shell-io/aasx-server/issues/new).

### Example Usage
For instance, to pull the latest `core` variant of the server for demonstration on an x86 64-bit machine (linux/amd64), use:

```shell
docker pull adminshellio/aasx-server-core-for-demo
```

Run the container with:

```shell
docker run \
    --detach \
    --network host \
    adminshellio/aasx-server-core-for-demo
```

After running, you can access the server locally. For example, using curl:

```shell
curl http://localhost:51310/server/listaas
```

You should receive a response similar to this JSON:

```json
{
  "aaslist": [
    "0 : ExampleMotor : [IRI] http://customer.com/aas/9175_7013_7091_9168 : ./aasxs/Example_AAS_ServoDCMotor_21.aasx"
  ]
}
```

As you can see, we already provide an example AASX in the container.
For a more thorough demo, you might want to copy additional AASX packages
(*e.g.*, from the [samples][samples]) into the container. Find the container
ID of your running container with:

```shell
docker ps
```

Then use `docker cp` to copy the AASX packages into the `aasxs` directory
(assuming your docker container ID is `70fe45f1f102`):

```shell
docker cp /path/to/aasx/samples/  70fe45f1f102:/AasxServerCore/aasxs/
```

If you demo with `blazor` variant, change the destination path analogously to
`AasxServerBlazor`.

For example a docker with blazor may be started by

```shell
docker run -p 51000:51310 -p 51001:5001 -v ~/samples:/AasxServerBlazor/aasxs adminshellio/aasx-server-blazor-for-demo
/AasxServerBlazor
```

connecting host port 51000 to REST port 51310 and host port 51001 to blazor
view port 5001. In addition, the host directory ~/samples is used to load
.AASX files from inside the docker.

Mind that there are many other options for managing containers for custom demos
such as [Docker multi-stage builds][multi-stage]
(using one of our demo images as base), [bind mounts][bind-mounts] *etc*.

[samples]: http://admin-shell-io.com/samples/

[multi-stage]: https://docs.docker.com/develop/develop-images/multistage-build/

[bind-mounts]: https://docs.docker.com/storage/bind-mounts/

### Build Docker Containers for Demonstration on Linux/macOS

We provide a powershell script to build the docker containers meant for
demonstrations at [`src/BuildDockerImages.ps1`](src/BuildDockerImages.ps1).

## Basic API

Please find a short description of the REST API below.

{aas-identifier} = idShort of AAS <br />
{submodel-identifier} = idShort of Submodel <br />
{se-identifier} = idShort of SubmodelElement <br />
{sec-identifier} = idShort of SubmodelElementCollection <br />

### Asset Administration Shell Repository Interface

| Cmd | String            | Example                                                                        |
|-----|-------------------|--------------------------------------------------------------------------------|
| GET | `/server/profile` | [http://localhost:51310/server/profile](http://localhost:51310/server/profile) |
| GET | `/server/listaas` | [http://localhost:51310/server/listaas](http://localhost:51310/server/listaas) |

### Asset Administration Shell Interface

| Cmd | String                                                                                                                                                                     | Example                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
|-----|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| GET | `/aas/{aas-identifier}`<br />`/aas/{aas-identifier}/core`<br />`/aas/{aas-identifier}/complete`<br />`/aas/{aas-identifier}/thumbnail`<br />`/aas/{aas-identifier}/aasenv` | [http://localhost:51310/aas/ExampleMotor](http://localhost:51310/aas/ExampleMotor)<br />[http://localhost:51310/aas/ExampleMotor/core](http://localhost:51310/aas/ExampleMotor/core)<br />[http://localhost:51310/aas/ExampleMotor/complete](http://localhost:51310/aas/ExampleMotor/complete)<br />[http://localhost:51310/aas/ExampleMotor/thumbnail](http://localhost:51310/aas/ExampleMotor/thumbnail)<br />[http://localhost:51310/aas/ExampleMotor/aasenv](http://localhost:51310/aas/ExampleMotor/aasenv) |

### Submodel Interface

| Cmd | String                                                                                                                                                                                                                                                                                                                               |
|-----|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| GET | `/aas/{aas-identifier}/submodels/{submodel-identifier}`<br />`/aas/{aas-identifier}/submodels/{submodel-identifier}/core`<br />`/aas/{aas-identifier}/submodels/{submodel-identifier}/deep`<br />`/aas/{aas-identifier}/submodels/{submodel-identifier}/complete`<br />`/aas/{aas-identifier}/submodels/{submodel-identifier}/table` |

>
*Example:* [http://localhost:51310/aas/ExampleMotor/submodels/Documentation/complete](http://localhost:51310/aas/ExampleMotor/submodels/Documentation/complete)

### Submodel Element Interface

| Cmd    | String                                                                                                                                                                                                                                                                                                                                                                                                                                                            |
|--------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| GET    | `/aas/{aas-identifier}/submodels/{submodel-identifier}/elements/{se-identifier}`<br />`/aas/{aas-identifier}/submodels/{submodel-identifier}/elements/{se-identifier}/core`<br />`/aas/{aas-identifier}/submodels/{submodel-identifier}/elements/{se-identifier}/complete`<br />`/aas/{aas-identifier}/submodels/{submodel-identifier}/elements/{se-identifier}/deep`<br />`/aas/{aas-identifier}/submodels/{submodel-identifier}/elements/{se-identifier}/value` |
| PUT    | `/aas/{aas-identifier}/submodels/{submodel-identifier}/elements/` *+ Payload*<br />*Payload = content of "elem"-part of a SubmodelElement (see example below)*                                                                                                                                                                                                                                                                                                    |
| DELETE | `/aas/{aas-identifier}/submodels/{submodel-identifier}/elements/{se-identifier}`                                                                                                                                                                                                                                                                                                                                                                                  |

>
*Example:* [http://localhost:51310/aas/ExampleMotor/submodels/OperationalData/elements/RotationSpeed/complete](http://localhost:51310/aas/ExampleMotor/submodels/OperationalData/elements/RotationSpeed/complete)

### Submodel Element Collection Interface

| Cmd    | String                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |
|--------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| GET    | `/aas/{aas-identifier}/submodels/{submodel-identifier}/elements/{sec-identifier}/{se-identifier}`<br />`/aas/{aas-identifier}/submodels/{submodel-identifier}/elements/{sec-identifier}/{se-identifier}/core`<br />`/aas/{aas-identifier}/submodels/{submodel-identifier}/elements/{sec-identifier}/{se-identifier}/complete`<br />`/aas/{aas-identifier}/submodels/{submodel-identifier}/elements/{sec-identifier}/{se-identifier}/deep`<br />`/aas/{aas-identifier}/submodels/{submodel-identifier}/elements/{sec-identifier}/{se-identifier}/value` |
| PUT    | `/aas/{aas-identifier}/submodels/{submodel-identifier}/elements/{sec-identifier}` *+ Payload*<br />*Payload = content of "elem"-part of a SubmodelElement (see example below)*                                                                                                                                                                                                                                                                                                                                                                         |
| DELETE | `/aas/{aas-identifier}/submodels/{submodel-identifier}/elements/{sec-identifier}/{se-identifier}`                                                                                                                                                                                                                                                                                                                                                                                                                                                      |

> *Example:* <http://localhost:51310/aas/ExampleMotor/submodels/Documentation/elements/OperatingManual/DocumentId/complete>

### Example: PUT SubmodelElement

`PUT` <http://localhost:51310/aas/ExampleMotor/submodels/OperationalData/elements>   
Payload:

```json
{
  "value": "1234",
  "valueId": null,
  "semanticId": {
    "keys": [
      {
        "type": "ConceptDescription",
        "local": true,
        "value": "http://customer.com/cd//1/1/18EBD56F6B43D895",
        "index": 0,
        "idType": "IRI"
      }
    ]
  },
  "constraints": [],
  "hasDataSpecification": [],
  "idShort": "RotationSpeedNEW",
  "category": "VARIABLE",
  "modelType": {
    "name": "Property"
  },
  "valueType": {
    "dataObjectType": {
      "name": "integer"
    }
  }
}
```

Test with: `GET` <http://localhost:51310/aas/ExampleMotor/submodels/OperationalData/elements/RotationSpeedNEW>

## Issues

If you want to request new features or report bugs, please
[create an issue](
https://github.com/admin-shell-io/aasx-server/issues/new).

## Contributing

Please see [CONTRIBUTING](CONTRIBUTING.md) for instructions on joining the development and general contribution guidelines.
For a complete list of all contributing individuals and companies, please visit our [CONTRIBUTORS](CONTRIBUTORS.md) page.

