verbot
======

    Tool for managing C# software version numbers using Semantic Versioning and
    Git


Synopsis
========

    verbot <command> [<arguments>]


Description
===========

    verbot works with Git repositories containing single Visual Studio
    solutions.

    Version numbers apply across entire Git repositories.

    verbot operates according to Semantic Versioning rules and version number
    formats.

    Operations affect the repository that the current working directory is in.


Commands
========

    help
        Display usage information

    get
        Get the current version number

        The version number is read from [AssemblyInformationalVersion]
        attributes in AssemblyInfo files.

        The operation fails if no [AssemblyInformationalVersion] attributes are
        found, or if they contain conflicting version numbers.

    set <version>
        Set the current version number

        The version number is set by adjusting various assembly version
        attributes:

            [AssemblyVersion]s are set to MAJOR.0.0.0

            [AssemblyFileVersion]s are set to MAJOR.MINOR.PATCH.0

            [AssemblyInformationalVersion]s are set to the full semantic version
            number

        The operation fails if at least one [AssemblyInformationalVersion]
        cannot be found to adjust.

    increment --major
    increment --minor
    increment [--patch]
        Increment to the next major, minor, or patch version

        If incrementing --major or --minor, creates a 'MAJOR.MINOR-master'
        branch for the current version before advancing (if on the master
        branch) or creates and advances on a new 'MAJOR-MINOR-master' branch
        for the new version (if on any other -master branch).

        The operation fails if there are uncommitted changes in the
        repository.

        The operation fails if the current version is not a release version
        or a -master version.

        The operation fails if any 'MAJOR.MINOR-master' branch is tracking a
        later version than 'master'.

        The operation fails if the current branch is not the appropriate
        'master' or 'MAJOR.MINOR-master' branch for the current version.

        The operation fails if the current version appears unreleased and
        incrementing it as directed would be unnecessary according to semver
        semantics.

        The operation fails if attempting to advance to the latest version
        from a branch other than 'master'.

        The operation fails if creating a new 'MAJOR.MINOR-master' branch is
        necessary but such a branch already exists.

    release
        Release the version being developed on a master or -master branch

        Specifically:

        -   Remove the prerelease and build components from the current
            version, and commit

        -   Tag with `MAJOR.MINOR.PATCH`

        -   Create or move the `MAJOR.MINOR-latest` branch

        -   Create or move the `MAJOR-latest` branch, if appropriate

        -   Create or move the `latest` branch, if appropriate

        -   Increment to the next patch version, add the -master prerelease
            version component, and commit


License
=======

    MIT License <https://github.com/macro187/verbot/blob/master/license.txt>


Copyright
=========

    Copyright (c) 2017
    Ron MacNeil <https://github.com/macro187>
