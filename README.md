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


AASX Server is a companion app for the [AASX Package Explorer](https://github.com/eclipse-aaspe/package-explorer). It provides a local service to host and serve Industrie 4.0 AASX packages. The Core variant exposes endpoints for REST, OPC UA, and MQTT protocols. The **Blazor** variant offers the same functionality and uses Blazor for a browser-based UI.

> **IMPORTANT**
>
> AASX Server is in **V3**; see [Releases](https://github.com/eclipse-aaspe/server/releases).  
> Ongoing development is also tracked on branches such as [`newdb2-main`](https://github.com/eclipse-aaspe/server/tree/newdb2-main) (see Docker tag `develop` below).

> **TIP**
>
> A demo server is running at [https://v3.admin-shell-io.com](https://v3.admin-shell-io.com).  
> You can explore the API manually at [https://v3.admin-shell-io.com/swagger](https://v3.admin-shell-io.com/swagger).

An AASX Server with security enabled can be found here: https://v3security.admin-shell-io.com/.

## Quick reference (V3)

| Topic | Detail |
|--------|--------|
| **Repository** | [eclipse-aaspe/server](https://github.com/eclipse-aaspe/server) |
| **Default HTTP port** | **5001** (Kestrel; configure with `Kestrel__Endpoints__Http__Url` or `ASPNETCORE_URLS`) |
| **API exploration** | Open **`/swagger`** on the same base URL as the UI (e.g. `http://localhost:5001/swagger`) |
| **AASX files** | Place packages under **`./aasxs`** (or the path you pass to `--data-path`) |

The legacy **V2** REST surface (`--rest`, `--host`, `--port`, fixed port **51310**) is **no longer** the primary API. Ignore the “**Connect to REST by:**” message in old tooling.

## How-to

Currently, **AasxServerBlazor** is the primary entry point; **AasxServerCore** may still be used in some deployments. **AasxServerWindows** is not actively developed; use **.NET 8** on Windows with the Blazor variant.

### Running from source (development)

Solution file: [`src/AasxServer.sln`](src/AasxServer.sln). Typical local run (from repository root):

```sh
dotnet restore src/AasxServer.sln
dotnet run --project src/AasxServerBlazor/AasxServerBlazor.csproj -- --no-security --data-path ./aasxs --external-blazor http://localhost:5001
```

Arguments after `--` are passed to the server. Adjust `--data-path` and `--external-blazor` to match your environment. See also [`src/AasxServerBlazor/Properties/launchSettings.json`](src/AasxServerBlazor/Properties/launchSettings.json) for examples.

### Remote debugging over SSH (Linux)

For a **non-Docker** run on a remote machine (bind `http://*:PORT`, optional Kestrel debug), start from an example script:

- Copy [`scripts/run-dev-ssh.sh.example`](scripts/run-dev-ssh.sh.example) to `run.sh`, set `APP_DIR` to your **publish** folder containing `AasxServerBlazor.dll`, then `chmod +x run.sh`.

From your laptop, forward the port, e.g.:

```sh
ssh -L 50010:127.0.0.1:50010 user@remote-host
```

### Running with .NET (published build)

After publishing `AasxServerBlazor`, run the DLL (replace **YOURPORT** / **YOURURL**):

```sh
export DOTNET_gcServer=1
export Kestrel__Endpoints__Http__Url=http://*:YOURPORT
dotnet AasxServerBlazor.dll --no-security --data-path ./aasxs --external-blazor YOURURL
```

Default port in many configs is **5001**. [ASP.NET Core Runtime 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) is required.

### Running with Docker

Image: [`docker.io/adminshellio/aasx-server-blazor-for-demo`](https://hub.docker.com/r/adminshellio/aasx-server-blazor-for-demo)

**Tags** (there is **no** `latest` tag; always specify a tag):

| Tag | Meaning |
|-----|---------|
| `main` | Built automatically by the GitHub pipeline on each push to **`main`** |
| `develop` | Built **manually** when needed from the current development branch (e.g. [`newdb2-main`](https://github.com/eclipse-aaspe/server/tree/newdb2-main)) |
| Version tags | Use when you need a fixed release build |

Example:

```sh
docker run \
  -p 5001:5001 \
  --restart unless-stopped \
  -v ./aasxs:/AasxServerBlazor/aasxs \
  docker.io/adminshellio/aasx-server-blazor-for-demo:main
```

### Docker Compose (repository file)

The repository includes a Compose file you can use or adapt:

```sh
docker compose -f src/docker/docker-compose.yaml up --build
```

See [`src/docker/docker-compose.yaml`](src/docker/docker-compose.yaml) for ports, volumes (`./aasxs`), and default `command` line. There is also [`src/docker/docker-compose-demo.yaml`](src/docker/docker-compose-demo.yaml) for demo variants.

### Docker Hub variants (architectures)

| Variant | linux/amd64 |
|---------|----------------|
| Blazor | [aasx-server-blazor-for-demo](https://hub.docker.com/r/adminshellio/aasx-server-blazor-for-demo) |
| Blazor arm32 | [aasx-server-blazor-for-demo-arm32](https://hub.docker.com/r/adminshellio/aasx-server-blazor-for-demo-arm32) |
| Blazor arm64 | [aasx-server-blazor-for-demo-arm64](https://hub.docker.com/r/adminshellio/aasx-server-blazor-for-demo-arm64) |
| Core | [aasx-server-core-for-demo](https://hub.docker.com/r/adminshellio/aasx-server-core-for-demo) |

### Multi-architecture

If you want to help with multi-arch images or Raspberry Pi, [open an issue](https://github.com/eclipse-aaspe/server/issues/new).

### Build Docker images locally

Use [`src/BuildDockerImages.ps1`](src/BuildDockerImages.ps1) on Windows/PowerShell (see script for prerequisites).

### Persistence (database)

V3 can persist data with **Entity Framework** (SQLite is common; PostgreSQL has been used in tests).

| Flag | Purpose |
|------|---------|
| `--with-db` | Enable database storage |
| `--start-index 0` | First import: load AASX from `--data-path` into the DB |
| `--start-index N` | Later starts: use **N** greater than the number of AASX files (e.g. 1000) so files are not re-imported |
| `--save-temp SEC` | Periodically write API changes to the DB every **SEC** seconds |
| `--aasx-in-memory N` | Limit how many AAS appear in the Blazor tree (only the most recently changed may be shown when limited) |

Example server with DB (third-party): [Example Server](https://cloudrepo.h2894164.stratoserver.net) · [DB view](https://cloudrepo.h2894164.stratoserver.net/db) · [GraphQL](https://cloudrepo.h2894164.stratoserver.net/graphql/) (type `{` then space for the wizard).

Example GraphQL query:

```graphql
{
   searchSubmodels (semanticId: "https://admin-shell.io/zvei/nameplate/1/0/Nameplate")
   {
     submodelId
     url
   }
}
```

Registry / auto-POST topics: see [issue tracker](https://github.com/eclipse-aaspe/server/issues).

## REST API (V3)

- Prefer **OpenAPI**: **`GET /swagger`** (or `/swagger/index.html`) on your server base URL.
- The implementation follows the **Asset Administration Shell REST API** (Part 2) for V3; path layout differs from the old **V2** tables that used port **51310**.

**Legacy documentation** (V2 paths, `AasxServerCore`, Mono, port 51310, long path tables) is kept in [`docs/legacy-readme.md`](docs/legacy-readme.md) for reference only.

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

## Issues

If you want to request new features or report bugs, please
[create an issue](https://github.com/eclipse-aaspe/server/issues/new).

## Contributing

Please see [CONTRIBUTING](CONTRIBUTING.md) for instructions on joining the development and general contribution guidelines.
For a complete list of all contributing individuals and companies, please visit our [CONTRIBUTORS](CONTRIBUTORS.md) page.
