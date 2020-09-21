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
  
A blazor demo server is running on https://admin-shell-io.com:5001/

## Binaries

The binaries are available in the [Releases section](
https://github.com/admin-shell-io/aasx-server/releases
). We provide portable dotnet assemblies.

### Installation

AASX Server depends on .NET Core 3.1 runtime (`blazor` and `core` variants)
and .NET Framework (`windows` variant), respectively. You need to install the
respective runtimes before you start the server.
See https://dotnet.microsoft.com/download/dotnet-core/3.1

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

We provide a couple of sample admin shells (packaged as .aasx) for you to test
and play with the software at: http://www.admin-shell-io.com/samples/

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
  -h, --host <host>                      Host which the server listens on [default: localhost]
  -p, --port <port>                      Port which the server listens on [default: 51310]
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
docker run \
    --detach \
    --network host \
    adminshellio/aasx-server-core-for-demo
```

The server should be accessible now on your localhost. For example, curl:

```
curl http://localhost:51310/server/listaas
```

should give you something like this:

```
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

```
docker ps
```

Then use `docker cp` to copy the AASX packages into the `aasxs` directory
(assuming your docker container ID is `70fe45f1f102`):

```
docker cp /path/to/aasx/samples/  70fe45f1f102:/AasxServerCore/aasxs/
```

If you demo with `blazor` variant, change the destination path analogously to 
`AasxServerBlazor`.

For example a docker with blazor may be startet by
```
docker run -p 51000:51310 -p 51001:5001 -v ~/samples:/AasxServerBlazor/aasxs adminshellio/aasx-server-blazor-for-demo
/AasxServerBlazor
```
connecting host port 51000 to REST port 51310 and host port 51001 to blazor
view port 5001. In addition the host directory ~/samples is used to load
.AASX files from inside the docker.

Mind that there are many other options for managing containers for custom demos 
such as [Docker multi-stage builds][multi-stage]
(using one of our demo images as base), [bind mounts][bind-mounts] *etc*.

[samples]: http://admin-shell-io.com/samples/
[multi-stage]: https://docs.docker.com/develop/develop-images/multistage-build/
[bind-mounts]: https://docs.docker.com/storage/bind-mounts/

### Build Docker Containers for Demonstration on Linux/MacOS

We provide a powershell script to build the docker containers meant for
demonstrations at [`src/BuildDockerImages.ps1`](src/BuildDockerImages.ps1).

## Issues

If you want to request new features or report bugs, please 
[create an issue](
https://github.com/admin-shell-io/aasx-server/issues/new). 

## Contributing

Code contributions are very welcome! Please see 
[CONTRIBUTING.md](CONTRIBUTING.md) for more information.
