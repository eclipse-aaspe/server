# AASX Server

AASX Server serves Industrie 4.0 AASX packages accessible by REST, OPC UA and 
MQTT protocols.

The AASX Server is based on code of AASX Package Explorer (
https://github.com/admin-shell-io/aasx-package-explorer
).

There are three variants of the server:

* **blazor**. This variant uses Blazor framework to provide a graphical user
  interface in the browser for exploring the AASX packages.

* **core**. This is a server based on .NET Core 3.1.

* **windows**. This variant uses .NET Framework 4.7.2. While the .NET Framework
  is outdated, this is the only way how you can start a server on your Windows
  machine without administrator privileges. 
  
  Mind that *blazor* and *core* variants require these privileges, so they
  can not be used for demonstration purposes on tightly-administered machines
  (which are wide-spread in larger organizations and enterprises). 

## Binaries

The binaries are available in the [Releases section](
https://github.com/admin-shell-io/aasx-server/releases
). We provide x64 binaries for Windows and Linux.

### Installation

AASX Server depends on .NET Core 3.1 runtime (`blazor` and `core` variants) 
and .NET Framework (`windows` variant), respectively. You need to install the 
respective runtimes before you start the server.

To deploy the binaries, simply extract the release bundle (*e.g.*, 
`AasxServerCore.zip`) somewhere on your system. 

### Running for Demonstration

We include an example AASX and various extra files (*e.g.*, certificates) in
the release bundle so that you can readily start the server for demonstration
purposes. The scripts `startForDemo.sh` and `startForDemo.bat` will start the
server with these pre-packaged files.

For example, if you run on Linux, change to the directory where you extracted
the release bundle and invoke:

```
./startForDemo.sh
``` 

### Running on Windows

Change to the directory where you extracted the release bundle.

Invoke the executable with the same name as the server variant. For example:

```
AasxServerCore.exe --opc --rest -data-path /path/to/aasxs
```

To obtain help on individual flags and options, supply the argument `--help`:

```
AasxServerCore.exe --help
```
<!--- Help starts. -->
```
AasxServerCore:
  serve AASX packages over different interface

Usage:
  AasxServerCore [options]

Options:
  --host <host>                          Host which the server listens on [default: localhost]
  --port <port>                          Port which the server listens on [default: 51310]
  --https                                If set, opens SSL connections. Make sure you bind a certificate to the port before.
  --data-path <data-path>                Path to where the AASXs reside
  --rest                                 If set, starts the REST server
  --opc                                  If set, starts the OPC server
  --mqtt                                 If set, starts a MQTT publisher
  --debug-wait                           If set, waits for Debugger to attach
  --opc-client-rate <opc-client-rate>    If set, starts an OPC client and refreshes on the given period (in milliseconds)
  --connect <connect>                    If set, connects to AAS connect server. Given as a comma-separated-values (server, node name, period in milliseconds) or as a flag (in which case it connects to a default server).
  --proxy-file <proxy-file>              If set, parses the proxy information from the given proxy file
  --no-security                          If set, no authentication is required
  --edit                                 If set, allows edits in the user interface
  --version                              Show version information
  -?, -h, --help                         Show help and usage information
```
<!--- Help ends. -->

### Running on Linux

Change to the directory where you extracted the release bundle.

Use `dotnet` to execute the DLL with the same name as the server variant.
For example:

```
dotnet AasxServerCore.dll --opc --rest --data-path /path/to/aasxs
```

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

## Docker Containers for Demonstration

We provide pre-built docker images meant for demonstration purposes at the 
following DockerHub repositories:

* `blazor`: https://hub.docker.com/repository/docker/adminshellio/aasx-server-blazor-for-demo
* `core`: https://hub.docker.com/repository/docker/adminshellio/aasx-server-core-for-demo

For example, to pull the latest `core` variant of the server for the 
demonstration, invoke:

```
docker pull adminshellio/aasx-server-core-for-demo
```

You can then run the container with:

```
docker run -d -p 51210:51210 -p 51310:51310 aasx-server-core-for-demo
```

### Build Docker Containers for Demonstration on Linux/MacOS

We provide a powershell script to build the docker containers meant for 
demonstrations at [`src/BuildDockerImages.ps1`](src/BuildDockerImages.ps1). 
