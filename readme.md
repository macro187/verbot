verbot
======

    A tool for managing version numbers using Semantic Versioning and Git



Synopsis
========

    verbot [--verbose] <command> [options]

		--verbose
			Output informative details as operations are performed.



Commands
========

    help
        Display usage information

    calc --release
    calc --prerelease
        Calculate and output the current version number

        Details on how version numbers are calculated can be found in the
        "Version Calculation" section.

        --release
            Always output a release version number.  If the current commit
            hasn't been tagged as a release, output a release version as if it
            was.

        --prerelease
            Always output a pre-release version number.  If the current commit
            has been tagged as a release, output a pre-release version as if
            it wasn't.

    write
    write --release
    write --prerelease
        Record the current version number in source code files, and then
        output it

        Details on how version numbers are calculated can be found in the
        "Version Calculation" section.

        Details on where version numbers are recorded in source code files can
        be found in the "Versions in Source Code" section.

        --release
            Always output a release version number.  If the current commit
            hasn't been tagged as a release, output a release version as if it
            was.

        --prerelease
            Always output a pre-release version number.  If the current commit
            has been tagged as a release, output a pre-release version as if
            it wasn't.

    reset
        Record a default "9999.0.0-alpha" version number in source code files

        Details on where version numbers are recorded in source code files can
        be found in the "Versions in Source Code" section.

    read
        Output the version number currently recorded in source code files

        Details on where version numbers are recorded in source code files can
        be found in the "Versions in Source Code" section.

    release
        Release the version being developed on a master or -master branch

        Specifically:

            Remove the prerelease and build components from the current
            version, and commit

            Tag with `MAJOR.MINOR.PATCH`

            Create or move the `MAJOR.MINOR-latest` branch

            Create or move the `MAJOR-latest` branch, if appropriate

            Create or move the `latest` branch, if appropriate

            Increment to the next patch version, add the -master prerelease
            version component, and commit

    push [--dry-run]
        `git push` any changed or missing version-related branches and tags.

        --dry-run
            Outputs what branches or tags would be pushed, but doesn't
            actually do it.

        The operation fails if there are uncommitted changes in the
        repository.

    check
        Check basic assumptions about the local repository required for verbot
        to operate.

        Specifically:
            There is at least one place in the repository to record the
            current version.

            Different versions are not recorded in different places.

    check-remote
        Check basic assumptions about the remote repsitory required for verbot
        to operate.

        Specifically:
            There are no remote release tags that differ from their local
            equivalents.



Version Calculation
===================

    Commits tagged as releases simply take their version numbers directly from
    the release tags.

    All other commits need to have their version numbers calculated.

    Verbot calculates versions by working forward from the previous tagged
    release, taking into account whether there have been any major or minor
    changes, the number of commits, the current commit date, and the current
    commit hash.

    The resulting version numbers sort according to topological order in the
    Git commit graph relative to the previous release, followed by commit
    date, followed by short commit hashes to guarantee uniqueness.

    Major, minor, and patch version number components advance according to the
    SemVer specification based on the presence of major and/or minor changes
    in intervening commits.

    Whether commits represent major or minor changes is determined by special
    "+semver" lines in commit messages.  A "+semver: major" or "+semver:
    breaking" line indicates a major, breaking change.  A "+semver: minor" or
    "+semver: feature" line indicates a minor, backwards-compatible feature
    change.



Versions in Source Code
=======================

    .NET Projects
        Repositories are assumed to contain a single Visual Studio solution.

        Version numbers are recorded in properties in `.csproj` files.

        `<Version>` properties contain full semantic version numbers.

        `<AssemblyVersion>` properties contain four-part MAJOR.0.0.0 .NET
        version numbers consisting of the current major version plus three
        zeroes e.g. "1.0.0.0", reflecting backward-compatibility within major
        versions.

        `<AssemblyFileVersion>` properties contain four-part
        MAJOR.MINOR.PATCH.0 .NET version numbers consisting of the current
        major, minor, and patch versions plus a zero e.g. "1.1.1.0", which is
        the closest .NET version numbers can get to representing semantic
        versions.



Master Branches
===============

    Development proceeds toward releases on master branches.  There are master
    branches for all major-minor release series, enabling concurrent
    development and maintenance of any number of them.

    Master branches are named according to a "MAJOR.MINOR-master" pattern,
    e.g. "1.1-master".  The one exception is the master branch for the latest
    release series, which is just "master".



License
=======

    MIT License <https://github.com/macro187/verbot/blob/master/license.txt>



Copyright
=========

    Copyright (c) 2017-2020
    Ron MacNeil <https://github.com/macro187>

