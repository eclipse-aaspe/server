# Contributing

This document describes how you can build the solution from scratch, submit 
your code contributions and make releases.

In a nutshell, our development workflow is:

* Create a feature branch
* Make your changes locally
* Run checks locally
* Commit & push your changes
* Wait for the remote checks to pass
* Squash & merge

## Building Locally

We provide a powershell script to build the solution:

```
.\src\BuildForRelease.s1
```

This script will install all the solution dependencies before the build. Please
inspect the script if you would like to build only parts of the solution.

## Locally Packaging for Release

Once the solution has been built, you can build your own local release bundles
(compressed as zip archives) with:

```
.\src\PackageForRelease.ps1
```

Note that these local release bundles are only meant for testing.
We set up our continuous integration on GitHub to remotely build, package and
publish the releases in automatic manner.

## Local Pre-commit Checks

We run a couple of checks to ensure the quality of the code (*e.g.*, checking
that the code format is consistent).

GitHub will run these checks automatically on every push. However, this can take
some time, so you usually run these checks locally first to speed up the 
development.

Please format your code before running the checks with the following script:

```
.\src\FormatCode.ps1
```

Run the checks:

```
.\src\Check.ps1
```

## Submitting Your Code

### Branching

**If you are part of admin-shell-io GitHub organization:**
create a branch prefixed with your Github username using dashes to 
describe the change (*e.g.*, `mristin/Add-a-shiny-new-feature-B`).

**Otherwise:** Since only members of the organization can create branches,
you need to fork the repository and create your feature branch on the fork (see 
[GitHub documentation about forking][github-fork]).

[github-fork]: https://docs.github.com/en/github/getting-started-with-github/fork-a-repo

### Commit and Pull Request

Commit your local changes and push them to the remote. Make sure the commit 
message complies to https://chris.beams.io/posts/git-commit/ guidelines:

* Separate subject from body with a blank line
* Limit the subject line to 50 characters
* Capitalize the subject line
* Do not end the subject line with a period
* Use the imperative mood in the subject line
* Wrap the body at 72 characters
* Use the body to explain what and why (instead of how)

You can use special hints in the commit messages to disable checks or building
of a docker image:

* `The workflow build-and-publish-docker-images was intentionally skipped.` 
  (no docker images will be created)
  
* `The workflow check-release was intentionally skipped.`
  (no release check, in particular no build)
  
* `The workflow check-style was intentionally skipped.`
  (no checks for code style)
  
Here is an example commit message:
  
```
Add a shiny new feature B

Previously, the solution could not perform A and it was confusing for
the users how to use `SomeModule`. This change introduces a
new feature B which solves A as well as the problem with C.

This is important because D now runs much faster and has a clearer 
structure. Moreover, feature B is particularly practical for use cases 
such as E and F.
 
Finally, we update the documentation to reflect how to manage G
in these concrete use cases E and F.

The workflow build-and-publish-docker-images was intentionally skipped.
```

Once all the checks pass, push your commit and create a pull request (see 
[this GitHub documentation page about pull requests][pull-request]).

[pull-request]: https://docs.github.com/en/github/collaborating-with-issues-and-pull-requests/creating-a-pull-request

### Merging

We usually squash & merge the pull requests into master.

### Releasing

We publish a new version using [GitHub Releases][release] in the web 
browser. The workflow `./github/workflows/build-and-package-release.yml` will 
automatically build and package the release bundles and attach them to the 
GitHub release.

[release]: https://docs.github.com/en/github/administering-a-repository/managing-releases-in-a-repository

### Appendix: GitHub Workflows

We use [GitHub Workflows][workflows] to automatically perform tasks such as 
pre-commit checks, building, packaging and publishing the release bundles 
as well as building and publishing docker images for demonstration.

Please see the directory `./.github/workflows` for the source code of 
the work flows.

[workflows]: https://docs.github.com/en/actions/configuring-and-managing-workflows