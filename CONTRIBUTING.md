# Contributing Guidelines

Thank you for considering contributing to our project! This document provides an overview of our development process and best practices for contributing code and making releases.

## Development Workflow

Our development workflow is structured as follows:

1. **Create a Feature Branch:** Start by creating a new branch for your feature or fix.
2. **Make Local Changes:** Develop and test your changes locally.
3. **Run Local Checks:** Ensure your code passes all checks locally before pushing.
4. **Commit & Push:** Commit changes to your branch and push them to the remote repository.
5. **Remote Checks:** Wait for automated checks on the remote repository to pass.
6. **Create Pull Request:** Create a new PR and fill out the template to give good information on your changes.
7. **Await Review:** Request a review from one of the project maintainers.
8. **Merge:** Once approved, squash and merge your changes into the main branch.

## Building Locally

To build the project locally, execute the following PowerShell script:

```powershell
.\src\BuildForRelease.ps1
```

This script installs dependencies and builds the solution. Modify the script for specific build targets if needed.

## Packaging for Release Locally

After building, create local release bundles using:

```powershell
.\src\PackageForRelease.ps1
```

These bundles are for testing purposes. Official releases are automated through our CI system.

For accessing release bundles created by the CI pipeline, check [here](https://github.com/eclipse-aaspe/server/actions/workflows/check-release.yml).

## Local Pre-commit Checks

Ensure code quality locally with these checks:

### Code Formatting

Align your code using our .editorconfig. Learn more about configuring your IDE:
* [Visual Studio](https://learn.microsoft.com/en-us/visualstudio/ide/create-portable-custom-editor-options?view=vs-2022).
* [VSCode](https://marketplace.visualstudio.com/items?itemName=EditorConfig.EditorConfig)
* [Rider](https://www.jetbrains.com/help/rider/Using_EditorConfig.html)

Format code using:
>**!WARNING:** this will format the whole solution and may include changes to files you did not touch yet.
```powershell
.\src\FormatCode.ps1
```

### Running Checks

Run additional checks using:

```powershell
.\src\Check.ps1
```

## Submitting Your Code

### Branching Strategy

#### For Eclipse AASX Package Explorer and Server Committers

1. **Integration Branch:** Create a new integration branch in the main repository, e.g., "integration/freezor".
   - Merge unique PRs here after consultation and regular updates from the main branch.
2. **Feature Branch:** Use your GitHub username as a prefix, followed by a descriptive branch name (e.g., `mristin/Add-a-shiny-new-feature-B`).
   - Keep these branches in the repository or your fork.

#### For Community Contributors

1. **Forking Repository:** If not part of the organization, fork the repository and create feature branches there.
   - Refer to GitHub's [fork documentation](https://docs.github.com/en/github/getting-started-with-github/fork-a-repo).

### Commit and Pull Request

Commit changes adhering to [Chris Beams' Git Commit Guidelines](https://chris.beams.io/posts/git-commit/).
Use special hints in messages to disable specific workflows:

- `The workflow build-and-publish-docker-images was intentionally skipped.`
- `The workflow check-release was intentionally skipped.`
- `The workflow check-style was intentionally skipped.`

#### Example Commit Message

```
Add a shiny new feature B

This change adds feature B, improving performance and clarity for users, especially in use cases E and F.

- Update documentation for managing G in use cases E and F.

The workflow build-and-publish-docker-images was intentionally skipped.
```

### Merging

We squash and merge pull requests into the main branch.

### Releasing

We use [GitHub Releases](https://docs.github.com/en/github/administering-a-repository/managing-releases-in-a-repository) to publish new versions.

### Contributors

For a complete list, see our [CONTRIBUTORS](CONTRIBUTORS.md) page.

### Appendix: GitHub Workflows

We automate tasks using [GitHub Workflows](https://docs.github.com/en/actions/configuring-and-managing-workflows). View them in `./.github/workflows`.

