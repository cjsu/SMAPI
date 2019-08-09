&larr; [README](../README.md)

This file provides more technical documentation about SMAPI. If you only want to use or create
mods, this section isn't relevant to you; see the main README to use or create mods.

This document is about SMAPI itself; see also [mod build package](mod-package.md) and
[web services](web.md).

# Contents
* [Customisation](#customisation)
  * [Configuration file](#configuration-file)
  * [Command-line arguments](#command-line-arguments)
  * [Compile flags](#compile-flags)
* [For SMAPI developers](#for-smapi-developers)
  * [Compiling from source](#compiling-from-source)
  * [Debugging a local build](#debugging-a-local-build)
  * [Preparing a release](#preparing-a-release)
* [Release notes](#release-notes)

## Customisation
### Configuration file
You can customise the SMAPI behaviour by editing the `smapi-internal/config.json` file in your game
folder.

Basic fields:

field             | purpose
----------------- | -------
`DeveloperMode`   | Default `false` (except in _SMAPI for developers_ releases). Whether to enable features intended for mod developers (mainly more detailed console logging).
`CheckForUpdates` | Default `true`. Whether SMAPI should check for a newer version when you load the game. If a new version is available, a small message will appear in the console. This doesn't affect the load time even if your connection is offline or slow, because it happens in the background.
`VerboseLogging`  | Default `false`. Whether SMAPI should log more information about the game context.
`ModData`         | Internal metadata about SMAPI mods. Changing this isn't recommended and may destabilise your game. See documentation in the file.

### Command-line arguments
The SMAPI installer recognises three command-line arguments:

argument | purpose
-------- | -------
`--install` | Preselects the install action, skipping the prompt asking what the user wants to do.
`--uninstall` | Preselects the uninstall action, skipping the prompt asking what the user wants to do.
`--game-path "path"` | Specifies the full path to the folder containing the Stardew Valley executable, skipping automatic detection and any prompt to choose a path. If the path is not valid, the installer displays an error.

SMAPI itself recognises two arguments, but these are intended for internal use or testing and may
change without warning.

argument | purpose
-------- | -------
`--no-terminal` | SMAPI won't write anything to the console window. (Messages will still be written to the log file.)
`--mods-path` | The path to search for mods, if not the standard `Mods` folder. This can be a path relative to the game folder (like `--mods-path "Mods (test)"`) or an absolute path.

### Compile flags
SMAPI uses a small number of conditional compilation constants, which you can set by editing the
`<DefineConstants>` element in `SMAPI.csproj`. Supported constants:

flag | purpose
---- | -------
`SMAPI_FOR_WINDOWS` | Whether SMAPI is being compiled on Windows for players on Windows. Set automatically in `crossplatform.targets`.

## For SMAPI developers
### Compiling from source
Using an official SMAPI release is recommended for most users.

SMAPI uses some C# 7 code, so you'll need at least
[Visual Studio 2017](https://www.visualstudio.com/vs/community/) on Windows,
[MonoDevelop 7.0](https://www.monodevelop.com/) on Linux,
[Visual Studio 2017 for Mac](https://www.visualstudio.com/vs/visual-studio-mac/), or an equivalent
IDE to compile it. It uses build configuration derived from the
[crossplatform mod config](https://github.com/Pathoschild/Stardew.ModBuildConfig#readme) to detect
your current OS automatically and load the correct references. Compile output will be placed in a
`bin` folder at the root of the git repository.

### Debugging a local build
Rebuilding the solution in debug mode will copy the SMAPI files into your game folder. Starting
the `SMAPI` project with debugging from Visual Studio (on Mac or Windows) will launch SMAPI with
the debugger attached, so you can intercept errors and step through the code being executed. This
doesn't work in MonoDevelop on Linux, unfortunately.

### Preparing a release
To prepare a crossplatform SMAPI release, you'll need to compile it on two platforms. See
[crossplatforming info](https://stardewvalleywiki.com/Modding:Modder_Guide/Test_and_Troubleshoot#Testing_on_all_platforms)
on the wiki for the first-time setup.

1. Update the version number in `.root/build/common.targets` and `Constants::Version`. Make sure
  you use a [semantic version](https://semver.org). Recommended format:

   build type | format                   | example
   :--------- | :----------------------- | :------
   dev build  | `<version>-alpha.<date>` | `3.0-alpha.20171230`
   prerelease | `<version>-beta.<count>` | `3.0-beta.2`
   release    | `<version>`              | `3.0`

2. In Windows:
   1. Rebuild the solution in Release mode.
   2. Copy `windows-install.*` from `bin/SMAPI installer` and `bin/SMAPI installer for developers` to
      Linux/Mac.

3. In Linux/Mac:
   1. Rebuild the solution in Release mode.
   2. Add the `windows-install.*` files to the `bin/SMAPI installer` and
      `bin/SMAPI installer for developers` folders.
   3. Rename the folders to `SMAPI <version> installer` and `SMAPI <version> installer for developers`.
   4. Zip the two folders.

## Release notes
See [release notes](../release-notes.md).
