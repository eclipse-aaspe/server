# Contributing

Thank you for your interest in contributing to our project! This document outlines the steps for building the solution from scratch, submitting code
contributions, and making releases.

## Development Workflow

Our development workflow follows these steps:

1. Create a feature branch
2. Make your changes locally
3. Run checks locally
4. Commit & push your changes
5. Wait for the remote checks to pass
6. Squash & merge

## Building Locally

To build the solution locally, we provide a PowerShell script:

```powershell
.\src\BuildForRelease.ps1
```

This script will install all necessary dependencies before building. If you prefer to build only parts of the solution, please inspect the script for specific
commands.

## Packaging for Release Locally

After building the solution, you can create local release bundles (compressed as zip archives) with the following script:

```powershell
.\src\PackageForRelease.ps1
```

Please note that these local release bundles are meant for testing purposes only. Our continuous integration system on GitHub automatically builds, packages,
and publishes official releases.

## Local Pre-commit Checks

To ensure code quality (e.g., consistent code formatting), we run several checks. GitHub will automatically run these checks on every push, but it’s faster to
run them locally first during development.

### Formatting Code

Format your code with the following script:

```powershell
.\src\FormatCode.ps1
```

### Running Checks

Run the checks with:

```powershell
.\src\Check.ps1
```

## Submitting Your Code

### Branching

**If you are part of the `Eclipse AASX Package Explorer and Server`GitHub organization:**
Create a branch prefixed with your GitHub username using dashes to describe the change (e.g., `mristin/Add-a-shiny-new-feature-B`).

**Otherwise:** If you are not a member of the organization, you need to fork the repository and create your feature branch on the fork. 
See the [GitHub documentation about forking][github-fork].

[github-fork]: https://docs.github.com/en/github/getting-started-with-github/fork-a-repo

### Commit and Pull Request

Commit your local changes and push them to the remote repository. Ensure your commit message adheres to the guidelines outlined at 
[Chris Beams' Git Commit Guidelines](https://chris.beams.io/posts/git-commit/):

* Separate subject from body with a blank line.
* Limit the subject line to 50 characters.
* Capitalize the subject line.
* Do not end the subject line with a period.
* Use the imperative mood in the subject line.
* Wrap the body at 72 characters.
* Use the body to explain what and why (instead of how).

You can use special hints in the commit messages to disable checks or the building of a Docker image:

* `The workflow build-and-publish-docker-images was intentionally skipped.` (No Docker images will be created)
* `The workflow check-release was intentionally skipped.` (No release check, particularly no build)
* `The workflow check-style was intentionally skipped.` (No checks for code style)

#### Example Commit Message

```
Add a shiny new feature B

Previously, the solution could not perform A, and it was confusing for
the users how to use `SomeModule`. This change introduces a
new feature B, which solves A as well as the problem with C.

This is important because D now runs much faster and has a clearer 
structure. Moreover, feature B is particularly practical for use cases 
such as E and F.
 
Finally, we update the documentation to reflect how to manage G
in these concrete use cases E and F.

The workflow build-and-publish-docker-images was intentionally skipped.
```

Once all checks pass, push your commit and create a pull request. Refer to [GitHub's documentation about pull requests][pull-request] for guidance.

[pull-request]: https://docs.github.com/en/github/collaborating-with-issues-and-pull-requests/creating-a-pull-request

### Merging

We typically squash and merge pull requests into our 'main' branch.

### Releasing

We publish new versions using [GitHub Releases][release] via the web browser. The workflow `./github/workflows/build-and-package-release.yml` will 
automatically build, package, and attach the release bundles to the GitHub release.

[release]: https://docs.github.com/en/github/administering-a-repository/managing-releases-in-a-repository

### Contributors

For a complete list of all contributing individuals and companies, please visit our [CONTRIBUTORS](CONTRIBUTORS.md) page.

### Appendix: GitHub Workflows

We use [GitHub Workflows][workflows] to automate tasks such as pre-commit checks, building, packaging, publishing release bundles, and building and 
publishing Docker images for demonstration.

Please see the `./.github/workflows` directory for the source code of our workflows.

[workflows]: https://docs.github.com/en/actions/configuring-and-managing-workflows