# Eclipse AASPE Server

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
>
> ### Dotnet Version
> We currently use Dotnet Version ![.NET Version](https://img.shields.io/badge/dotnet-9.0-blue)

AASPE Server is a companion app for the [AASPE Package Explorer](https://github.com/eclipse-aaspe/package-explorer). Some source code is shared, especially AasCore.  
AASPE Server is a AAS Repository, a Submodel Repository and a Concept Description Repository.  
It can also import and export AASX packages by the AASX File Server API and supports the Serialization API.  
AAS and Submodel Queries can be made by the Query Repository API.  
A readonly AAS Registry can be provided automatically by the AAS Repository.  

AASPE Server uses Entity Framework and stores data in the SQL database SQLite. (Other SQL databases like PostgreSQL may be supported later.)  
Performance has been tested with up to 100K AAS, 500K SM, 50M SME and 100M Values, where AASQL queries can be made in less than 10 seconds.
Many optimizations have been made by testing with SQLite.

The Core variant exposes REST endpoints and provides changed data by MQTT.  
The **Blazor** variant offers the same functionality and uses Blazor for a browser-based UI. The **Blazor** variant is also able to provide changed data by REST API.

## ⚠️ Important Notice: Breaking Changes in `main`

The `main` branch contains **breaking changes** compared to previous versions of the system.

These changes may require adjustments in existing integrations, configurations, or deployments. Please review the changes carefully before upgrading.

### What This Means
- Existing setups may no longer work without modification
- Migration effort may be required  
- Db Schema has changed 
- -NET 9.0 is used now

### Legacy Support

If you need to continue working with the previous version, please use the dedicated legacy branch:

➡️ **`main-db1`**

This branch is maintained to support the older version and ensure compatibility with existing systems. Docker images for main-db1 are tagged legacy.

### Recommendation

- Use `main` for all **new development** and future-ready integrations  
- Use `main-db1` only if you depend on the **legacy implementation**

## Quick Facts

### Reference Demo 
>
> The current reference demo (large dataset, **security and row-level filtering enabled**) is [`https://big.aas-voyager.com/`](https://big.aas-voyager.com/).  
> Explore the API at [`/swagger`](https://big.aas-voyager.com/swagger/index.htm), the live access rules at [`/access`](https://big.aas-voyager.com/access), and the DB browser at [`/db-chunked`](https://big.aas-voyager.com/db-chunked).  
> See [`docs/security.md`](docs/security.md) for how the roles, rule language and `FILTER` blocks used by that server are wired up.

> Older demo endpoints such as `v3.admin-shell-io.com` and `v3security.admin-shell-io.com` are outdated; prefer `big.aas-voyager.com` for all new tests and screenshots.

### Quick reference (V3)

| Topic | Detail |
|--------|--------|
| **Repository** | [eclipse-aaspe/server](https://github.com/eclipse-aaspe/server) |
| **Default HTTP port** | **5001** (Kestrel; configure with `Kestrel__Endpoints__Http__Url` or `ASPNETCORE_URLS`) |
| **API exploration** | Open **`/swagger`** on the same base URL as the UI (e.g. `http://localhost:5001/swagger`) |
| **AASX files** | Place packages under **`./aasxs`** (or the path you pass to `--data-path`) |

The legacy **V2** REST surface (`--rest`, `--host`, `--port`, fixed port **51310**) is **no longer** the primary API. Ignore the “**Connect to REST by:**” message in old tooling.

## Setting Up the AASX Server

The recommended approach is Docker, since ready-made, automatically built images are available. Alternatively, you can run the server directly with .NET. Currently, **AasxServerBlazor** is the primary entry point; **AasxServerCore** may still be used in some deployments. **AasxServerWindows** is not actively developed; use **.NET 9** on Windows with the Blazor variant.

### Prerequisites 

- A current Docker installation (Docker Engine or Docker Desktop). 
- Optionally, a directory containing your `.aasx` files. 
- For the .NET variant only: the 
  [.NET 9 Runtime / SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0). 

### Build with Docker
The recommended approach to run the server is Docker.
 
#### Available Docker Images 

Images are published to Docker Hub under the `adminshellio` organisation. 

| Variant | Image | 
| --- | --- | 
| Blazor (with GUI) | [`adminshellio/aasx-server-blazor-for-demo`](https://hub.docker.com/r/adminshellio/aasx-server-blazor-for-demo) | 
| Blazor arm32 | [`adminshellio/aasx-server-blazor-for-demo-arm32`](https://hub.docker.com/r/adminshellio/aasx-server-blazor-for-demo-arm32) | 
| Blazor arm64 | [`adminshellio/aasx-server-blazor-for-demo-arm64`](https://hub.docker.com/r/adminshellio/aasx-server-blazor-for-demo-arm64) | 
| Core (without GUI) | [`adminshellio/aasx-server-core-for-demo`](https://hub.docker.com/r/adminshellio/aasx-server-core-for-demo) | 

#### Tags 

> **There is no `latest` tag — always specify a tag explicitly.** 

| Tag | Meaning | 
| --- | --- | 
| `main` | Built automatically by the CI pipeline on each push to `main`. | 
| `develop` | Built **manually** when needed from the current development branch (e.g. `events-api-supplier`). | 
| `legacy` | Built from the `main-db1` legacy branch (previous version). | 
| `<version>` | A fixed release build (see [Releases](https://github.com/eclipse-aaspe/server/releases)). | 

The same tags apply to the other `adminshellio/aasx-server-…-for-demo` images 
(Core, arm32, arm64) when published. 

#### Build Docker images locally

Use [`src/BuildDockerImages.ps1`](src/BuildDockerImages.ps1) on Windows/PowerShell (see script for prerequisites).

#### Quick Start

> **Important:** As of V3, running **with a database** is the intended mode of 
> operation. The former pure in-memory mode (without `--with-db`) is no longer 
> actively maintained or tested. The examples below enable the database. See 
> [Persistence](#persistence-database) for details. 

The following command starts the Blazor variant, mounts a local directory 
`./aasxs` containing your AASX files, and imports them into the database on the 
**first** start: 

```bash 
docker run \ 
  -p 5001:5001 \ 
  --restart unless-stopped \ 
  -v ./aasxs:/AasxServerBlazor/aasxs \ 
  docker.io/adminshellio/aasx-server-blazor-for-demo:main \ 
  --with-db --start-index 0 --data-path /AasxServerBlazor/aasxs 
``` 

The user interface is then available at <http://localhost:5001>. 

> **On subsequent starts**, the already-imported content should **not** be read 
> in again. Set `--start-index` to a value greater than your number of AASX files 
> (e.g. `--start-index 1000`). See [Persistence](#persistence-database). 

To use the development build instead, pull and run the `:develop` tag: 

```bash 
docker pull docker.io/adminshellio/aasx-server-blazor-for-demo:develop 
``` 

#### Running with Docker Compose 

The repository includes a Compose file you can use or adapt: 

```bash 
docker compose -f src/docker/docker-compose.yaml up --build 
``` 

See 
[`src/docker/docker-compose.yaml`](https://github.com/eclipse-aaspe/server/blob/main/src/docker/docker-compose.yaml) 
for ports, volumes (`./aasxs`), and the default `command` line. A 
[`src/docker/docker-compose-demo.yaml`](https://github.com/eclipse-aaspe/server/blob/main/src/docker/docker-compose-demo.yaml) 
is also provided for demo variants.

#### Loading AASX Files 

Provide AASX packages in one of two ways: 

1. **Via a mounted directory** (recommended): place your `.aasx` files into the 
   directory you mount with `-v` and reference via `--data-path`. 
2. **Copy into a running container:** 

   ```bash 
   docker cp /path/to/aasx/samples/ <container-id>:/AasxServerBlazor/aasxs/ 
   ``` 

   For the Core variant, use `/AasxServerCore/aasxs/` analogously. 

### Alternative methods for setting up the server
If you choose not to use Docker, the server can be run using several alternative methods.

#### Running from source
Solution file: [`src/AasxServer.sln`](src/AasxServer.sln). Typical local run (from repository root):

```sh
dotnet restore src/AasxServer.sln
dotnet run --project src/AasxServerBlazor/AasxServerBlazor.csproj -- --no-security --data-path ./aasxs --external-blazor http://localhost:5001
```

Arguments after `--` are passed to the server. Adjust `--data-path` and `--external-blazor` to match your environment. See also [`src/AasxServerBlazor/Properties/launchSettings.json`](src/AasxServerBlazor/Properties/launchSettings.json) for examples.

#### Remote debugging over SSH (Linux)

For a **non-Docker** run on a remote machine (bind `http://*:PORT`, optional Kestrel debug), start from an example script:

- Copy [`scripts/run-dev-ssh.sh.example`](scripts/run-dev-ssh.sh.example) to `run.sh`, set `APP_DIR` to your **publish** folder containing `AasxServerBlazor.dll`, then `chmod +x run.sh`.

From your laptop, forward the port, e.g.:

```sh
ssh -L 50010:127.0.0.1:50010 user@remote-host
```

##### Running with .NET (published build)

After publishing `AasxServerBlazor`, run the DLL (replace **YOURPORT** / **YOURURL**):

```sh
export DOTNET_gcServer=1
export Kestrel__Endpoints__Http__Url=http://*:YOURPORT
dotnet AasxServerBlazor.dll --no-security --data-path ./aasxs --external-blazor YOURURL
```

Default port in many configs is **5001**. [.NET 9 Runtime / SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) is required to build and run.

### Multi-architecture

If you want to help with multi-arch images or Raspberry Pi, [open an issue](https://github.com/eclipse-aaspe/server/issues/new).

### Persistence (database)

V3 can persist data with **Entity Framework** (SQLite is common; PostgreSQL has been used in tests).

For a walkthrough of the relational schema (entity model, `IdShortPath` flattening)
and how AASQL is translated to SQL (the `CombineTablesLEFT` pipeline with
`$$path{n}$$` / `$$match{n}$$` / `EXISTS` placeholders and the recursive
`smePath{k}` / `Path{k}` / `Part1_{..}_{..}` aliases), see
[`docs/database-and-query.md`](docs/database-and-query.md).


| Flag | Purpose |
|------|---------|
| `--with-db` | Enable database storage |
| `--start-index 0` | First import: load AASX from `--data-path` into the DB |
| `--start-index N` | Later starts: use **N** greater than the number of AASX files (e.g. 1000) so files are not re-imported |
| `--save-temp SEC` | Periodically write API changes to the DB every **SEC** seconds |
| `--aasx-in-memory N` | Limit how many AAS appear in the Blazor tree (only the most recently changed may be shown when limited) |

Reference demo with DB enabled: [`big.aas-voyager.com`](https://big.aas-voyager.com/) · [DB view](https://big.aas-voyager.com/db-chunked) · [access rules](https://big.aas-voyager.com/access) · [Swagger](https://big.aas-voyager.com/swagger/index.htm).

For a walkthrough of the relational schema and how AASQL is translated to SQL, 
see 
[`docs/database-and-query.md`](https://github.com/eclipse-aaspe/server/blob/main/docs/database-and-query.md). 

### Security (authentication, roles, access rules)

Security is configured through AAS submodels (`SecuritySettingsForServer`,
`SecurityMetamodelForServer`) plus a JSON rule file served at `/Access`.
The reference deployment [`big.aas-voyager.com`](https://big.aas-voyager.com/)
has role-based authentication **and** row-level `FILTER` rules enabled.

The rule system defines a role catalogue (`isNotAuthenticated`, `isReaderOnly`, 
`isAuthenticatedUser`, `isSuperDuperUser`, …) and an `AllAccessPermissionRules` 
rule language exposed at `/Access`. `FORMULA` and `FILTER` expressions are merged 
into the AASQL query pipeline, allowing fine-grained, row-level access control. 

For the full role catalogue, the rule language, and how the expressions are 
wired up, see 
[`docs/security.md`](https://github.com/eclipse-aaspe/server/blob/main/docs/security.md). 

> The `--no-security` flag disables authentication entirely and is intended for 
> local tests and demos only — never for production. 

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

## Issues

If you want to request new features or report bugs, please
[create an issue](https://github.com/eclipse-aaspe/server/issues/new).

## Contributing

Please see [CONTRIBUTING](CONTRIBUTING.md) for instructions on joining the development and general contribution guidelines.
For a complete list of all contributing individuals and companies, please visit our [CONTRIBUTORS](CONTRIBUTORS.md) page.
