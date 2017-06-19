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

    increment [--patch]
    increment --minor
    increment --major
        Increment to the next patch, minor, or major version


License
=======

    MIT License <https://github.com/macro187/verbot/blob/master/license.txt>


Copyright
=========

    Copyright (c) 2017
    Ron MacNeil <https://github.com/macro187>

