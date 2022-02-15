# AASX Server
![Check-release-workflow](
https://github.com/admin-shell-io/aasx-server/workflows/Check-release-workflow/badge.svg
) ![Check-style-workflow](
https://github.com/admin-shell-io/aasx-server/workflows/Check-style-workflow/badge.svg
) ![Build-and-package-release-workflow](
https://github.com/admin-shell-io/aasx-server/workflows/Build-and-package-release-workflow/badge.svg
) ![Build-and-publish-docker-images-workflow](
https://github.com/admin-shell-io/aasx-server/workflows/Build-and-publish-docker-images-workflow/badge.svg
)

AASX Server serves Industrie 4.0 AASX packages accessible by REST, OPC UA and
MQTT protocols.

The AASX Server is based on code of AASX Package Explorer (
https://github.com/admin-shell-io/aasx-package-explorer
).

There are three variants of the server:

* **blazor**. This variant uses Blazor framework to provide a graphical user
  interface in the browser for exploring the AASX packages. The other APIs
  are the same as in the *core* variant.

* **core**. This is a server based on .NET Core 3.1.

* **windows**. This variant uses .NET Framework 4.7.2, which is the only way
  how you can start a server on your Windows machine without administrator privileges.
  If you run on windows start with this variant first and try *blazor* later.


  Mind that *blazor* and *core* variants require administrator privileges, so they
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

```
./startForDemo.sh
```

On Windows please start ```startForDemo.bat```.

We provide a couple of sample admin shells (packaged as .aasx) for you to test
and play with the software at: http://www.admin-shell-io.com/samples/.
Please copy these to the ```aasxs``` subdirectory as needed.

### Running on Windows

Change to the directory where you extracted the release bundle.

Start ```startForDemo.bat``` or invoke the executable with the same name as the
server variant. For example:

```
AasxServerWindows.exe --opc --rest -data-path /path/to/aasxs
```

You can see the AAS on the server with: http://localhost:51310/server/listaas.
To show the JSON of the exampleMotor AAS please use: http://localhost:51310/aas/ExampleMotor.
To show submodel "Identification" please use: http://localhost:51310/aas/ExampleMotor/submodels/Identification/complete.

### Options

To obtain help on individual flags and options, supply the argument `--help`:

```AasxServerWindows.exe --help``` or ```AasxServerCore.exe --help```

<!--- Help starts. -->
```
AasxServerCore:
  serve AASX packages over different interfaces

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
  --name <name>                          Name of the server
  --version                              Show version information
  -?, -h, --help                         Show help and usage information
```
<!--- Help ends. -->

### Running on Linux

Change to the directory where you extracted the release bundle.

Start ```startForDemo.bat``` or use `dotnet` to execute the DLL with the same name
as the server variant. For example:

```
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

## Docker Containers for Demonstration

We provide pre-built docker images meant for demonstration purposes at the
following DockerHub repositories:

* `blazor` [linux/amd64](https://hub.docker.com/r/adminshellio/aasx-server-blazor-for-demo)
* `blazor` [linux/arm32](https://hub.docker.com/r/adminshellio/aasx-server-blazor-for-demo-arm32)
* `blazor` [linux/arm64](https://hub.docker.com/r/adminshellio/aasx-server-blazor-for-demo-arm64)
* `core` [linux/amd64](https://hub.docker.com/r/adminshellio/aasx-server-core-for-demo)
* `core` [linux/arm32](https://hub.docker.com/r/adminshellio/aasx-server-core-for-demo-arm32)
* `core` [linux/arm64](https://hub.docker.com/r/adminshellio/aasx-server-core-for-demo-arm64)

In case you want to deploy on Raspberry PI, you probably need to use ARM 32-bit.

Ideally, we would like to set up a multi-arch docker container (see [this article](
https://www.docker.com/blog/multi-arch-build-and-images-the-simple-way/)). If you have experience with multi-arch
images and would like to help, please let us know by [creating an issue](
https://github.com/admin-shell-io/aasx-server/issues/new).

For example, to pull the latest `core` variant of the server for the
demonstration on a x86 64-bit machine (linux/amd64), invoke:

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

## Basic API

Please find a short description of the REST API below.

{aas-identifier} = idShort of AAS <br />
{submodel-identifier} = idShort of Submodel <br />
{se-identifier} = idShort of SubmodelElement <br />
{sec-identifier} = idShort of SubmodelElementCollection <br />

### Asset Administration Shell Repository Interface

Cmd | String | Example
------- | ------ | -------
GET | /server/profile | <http://localhost:51310/server/profile>
GET | /server/listaas | <http://localhost:51310/server/listaas>

### Asset Administration Shell Interface

Cmd | String | Example
------- | ------ | -------
GET | /aas/{aas-identifier}  <br />  /aas/{aas-identifier}/core  <br />  /aas/{aas-identifier}/complete  <br />  /aas/{aas-identifier}/thumbnail  <br />  /aas/{aas-identifier}/aasenv   | <http://localhost:51310/aas/ExampleMotor>  <br />  <http://localhost:51310/aas/ExampleMotor/core>  <br />  <http://localhost:51310/aas/ExampleMotor/complete>  <br />  <http://localhost:51310/aas/ExampleMotor/thumbnail>  <br />  <http://localhost:51310/aas/ExampleMotor/aasenv>


### Submodel Interface

Cmd | String
------- | ------
GET | /aas/{aas-identifier}/submodels/\{submodel-identifier} <br /> /aas/{aas-identifier}/submodels/\{submodel-identifier}/core <br /> /aas/{aas-identifier}/submodels/\{submodel-identifier}/deep <br /> /aas/{aas-identifier}/submodels/\{submodel-identifier}/complete <br /> /aas/{aas-identifier}/submodels/\{submodel-identifier}/table

> *Example:* <http://localhost:51310/aas/ExampleMotor/submodels/Documentation/complete>


### Submodel Element Interface

Cmd | String
------- | ------
GET | /aas/{aas-identifier}/submodels/{submodel-identifier}/elements/{se-identifier} <br /> /aas/{aas-identifier}/submodels/{submodel-identifier}/elements/{se-identifier}/core <br /> /aas/{aas-identifier}/submodels/{submodel-identifier}/elements/{se-identifier}/complete <br /> /aas/{aas-identifier}/submodels/{submodel-identifier}/elements/{se-identifier}/deep <br /> /aas/{aas-identifier}/submodels/{submodel-identifier}/elements/{se-identifier}/value <br/>
PUT | /aas/{aas-identifier}/submodels/{submodel-identifier}/elements/ *+ Payload* <br />  *Payload = content of "elem"-part of a SubmodelElement (see example below)*
DELETE |  /aas/{aas-identifier}/submodels/{submodel-identifier}/elements/{se-identifier} | <http://localhost:51310/aas/ExampleMotor/submodels/OperationalData/elements/RotationSpeed>

> *Example:* <http://localhost:51310/aas/ExampleMotor/submodels/OperationalData/elements/RotationSpeed/complete>

### Submodel Element Collection Interface
Cmd | String
------- | ------
GET | /aas/{aas-identifier}/submodels/{submodel-identifier}/elements/{sec-identifier}/{se-identifier} <br /> /aas/{aas-identifier}/submodels/{submodel-identifier}/elements/{sec-identifier}/{se-identifier}/core <br /> /aas/{aas-identifier}/submodels/{submodel-identifier}/elements/{sec-identifier}/{se-identifier}/complete <br /> /aas/{aas-identifier}/submodels/{submodel-identifier}/elements/{sec-identifier}/{se-identifier}/deep <br /> /aas/{aas-identifier}/submodels/{submodel-identifier}/elements/{sec-identifier}/{se-identifier}/value
PUT |     /aas/{aas-identifier}/submodels/{submodel-identifier}/elements/{sec-identifier} *+ Payload* <br /> *Payload = content of "elem"-part of a SubmodelElement (see example below)*
DELETE |  /aas/{aas-identifier}/submodels/{submodel-identifier}/elements/{sec-identifier}/{se-identifier}

> *Example:* <http://localhost:51310/aas/ExampleMotor/submodels/Documentation/elements/OperatingManual/DocumentId/complete>

### Example: PUT SubmodelElement
`PUT` <http://localhost:51310/aas/ExampleMotor/submodels/OperationalData/elements>   
Payload:
```
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

Code contributions are very welcome! Please see 
[CONTRIBUTING.md](CONTRIBUTING.md) for more information.
