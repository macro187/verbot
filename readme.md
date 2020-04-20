verbot
======

A tool for managing version numbers for C# projects using Semantic Versioning
and Git



Synopsis
========

    verbot <command> [<arguments>]



Description
===========

    verbot operates according to a strict view of how version numbering should
    work.

    Version number formats and rules are according to the Semantic Versioning
    specification <http://semver.org/>.

    >   This overrules everyone else including Microsoft

    Version numbers apply across entire Git repositories.

    Git repositories contain single Visual Studio solutions.

    Version information is stored in properties in projects that are both in
    the solution and in the repository.  If more than one such project exists,
    they all contain the same version.

    `Version` properties contain the full version number in Semantic
    Versioning format.

    `AssemblyVersion` properties contain a four-part .NET version number
    consisting of the current major version plus three zeroes e.g. "1.0.0.0"

    >   This reflects backward-compatibility within major versions

    `AssemblyFileVersion` properties contain a four-part .NET version number
    consisting of the current major, minor, and patch versions plus a zero
    e.g.  "1.1.1.0"

    >   This is the closest a .NET version number can get to a full Semantic
    >   Version

    Development proceeds toward releases on master branches.  There are master
    branches for all possible future major-minor releases.  The master branch
    proceeding towards the highest precedence (or "latest") release is named
    "master".  Master branches proceeding towards all other releases are named
    according to the MAJOR.MINOR version plus a -master suffix e.g.
    "1.1-master"

    >   This enables concurrent development and maintenance of any number of
    >   MAJOR.MINOR versions of the software

    During development, the current version number is the version of the
    release being worked toward plus a -master pre-release suffix e.g.
    "1.2.3-master"

    TODO Releases

    verbot operations affect the repository that the current working directory
    is in.



Commands
========

    help
        Display usage information

    calc [--verbose]
    calc --release [--verbose]
    calc --prerelease [--verbose]
        Calculate the current commit's version number.

        If the current commit is tagged with a release version, output that.
        Otherwise, compute and output a unique pre-release version as
        described in the "Version Computation Scheme" section.

        --release
            Output a release version number.  If the current commit hasn't
            been tagged as a release, output the release version it would be
            if it were.

        --prerelease
            Output a pre-release version number.  If the current commit has
            been tagged as a release, output the pre-release version it would
            be if it wasn't.

        --verbose
            Output diagnostic information about how the version number is
            calculated.

    get
        Get the current version number

        The version number is read from `Version` properties in project files.

        The operation fails if no `Version` properties are found, or if they
        contain conflicting version numbers.

    set <version>
        Set the current version number

        The version number is set by adjusting various project version
        properties:

            `Version`s are set to the full semantic version number

            `AssemblyFileVersion`s are set to MAJOR.MINOR.PATCH.0

            `AssemblyVersion`s are set to MAJOR.0.0.0

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

        The operation fails if the current version is not a release version or
        a -master version.

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



Version Computation Scheme
==========================

    Verbot computes versions by working forward from the previous tagged
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



License
=======

MIT License <https://github.com/macro187/verbot/blob/master/license.txt>



Copyright
=========

Copyright (c) 2017-2020  
Ron MacNeil <https://github.com/macro187>

