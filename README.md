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

## Docker

We provide pre-built docker images for the different variants at the following 
repositories:

* `blazor`: https://hub.docker.com/repository/docker/mristin/aasx-server-blazor
* `core`: https://hub.docker.com/repository/docker/mristin/aasx-server-core

For example, to pull the latest `core` variant of the server, invoke:

```
docker pull mristin/aasx-server-core
```

You can then run the container with:

```
docker run -d -p 51210:51210 -p 51310:51310 aasx-server-core
```

### Build Docker Container on Linux/MacOS

We provide a powershell script to build the docker container at 
[`src/BuildDockerImages.ps1`](src/BuildDockerImages.ps1). 

Once the images have been built, you can execute:
* [`src/RunAasxServerBlazorDocker.ps1`](
src/RunAasxServerBlazorDocker.ps1
) for the `blazor` variant or
* [`src/RunAasxServerCoreDocker.ps1`](
src/RunAasxServerCoreDocker.ps1
) for the `core` variant, respectively.
