verbot
======

    A tool for managing Semantic Versioning releases from Git repositories



Synopsis
========

    verbot [--verbose] <command> [options]

        --verbose
            Output informative details as operations are performed.



Commands
========

    check
        Check for correctness of commits, releases, and branches

    repair
        Check for correctness of commits, releases, and branches and, where
        possible, automatically correct problems

    release
        Tag the current commit as a release, adjusting master and latest
        branches accordingly

    write
    write --release
    write --prerelease
        Calculate the version number of the current commit, write it to
        appropriate locations in source code files, and output it

        Details on how version numbers are calculated can be found in the
        "Version Calculation" section.

        Details on where version numbers are recorded in source code files can
        be found in the "Versions in Source Code" section.

        --release
            Always output a release version number.  If the current commit
            hasn't been tagged as a release, output a release version number
            as if it was.

        --prerelease
            Always output a pre-release version number.  If the current commit
            has been tagged as a release, output a pre-release version number
            as if it wasn't.

    reset
        Record a default "9999.0.0-alpha" version number in source code files

        Details on where version numbers are recorded in source code files can
        be found in the "Versions in Source Code" section.

    calc --release
    calc --prerelease
        Calculate the version number of the current commit and output it

        Details on how version numbers are calculated can be found in the
        "Version Calculation" section.

        --release
            Always output a release version number.  If the current commit
            hasn't been tagged as a release, output a release version number
            as if it was.

        --prerelease
            Always output a pre-release version number.  If the current commit
            has been tagged as a release, output a pre-release version number
            as if it wasn't.

    read
        Read the version number currently recorded in source code files and
        output it

        Details on where version numbers are recorded in source code files can
        be found in the "Versions in Source Code" section.

    help
        Display usage information



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



Latest Branches
===============

    Latest branches track the latest releases overall and in each major and
    minor release series.

    The latest overall release is tracked by the "latest" branch.

    The latest release in each major release series is tracked by a branch
    following a "MAJOR-latest" pattern e.g. "2-latest".

    The latest release in each minor release series is tracked by a branch
    following a "MAJOR.MINOR-latest" pattern e.g. "2.3-latest".



License
=======

    MIT License <https://github.com/macro187/verbot/blob/master/license.txt>



Copyright
=========

    Copyright (c) 2017-2020
    Ron MacNeil <https://github.com/macro187>

