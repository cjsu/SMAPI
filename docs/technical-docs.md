&larr; [README](README.md)

This file provides more technical documentation about SMAPI. If you only want to use or create
mods, this section isn't relevant to you; see the main README to use or create mods.

# Contents
* [SMAPI](#smapi)
  * [Development](#development)
    * [Compiling from source](#compiling-from-source)
    * [Debugging a local build](#debugging-a-local-build)
    * [Preparing a release](#preparing-a-release)
  * [Customisation](#customisation)
    * [Configuration file](#configuration-file)
    * [Command-line arguments](#command-line-arguments)
    * [Compile flags](#compile-flags)
* [SMAPI web services](#smapi-web-services)
  * [Overview](#overview)
    * [Log parser](#log-parser)
    * [Web API](#web-api)
  * [Development](#development-2)
    * [Local development](#local-development)
    * [Deploying to Amazon Beanstalk](#deploying-to-amazon-beanstalk)
* [Mod build config package](#mod-build-config-package)

# SMAPI
## Development
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
the `StardewModdingAPI` project with debugging from Visual Studio (on Mac or Windows) will launch
SMAPI with the debugger attached, so you can intercept errors and step through the code being
executed. This doesn't work in MonoDevelop on Linux, unfortunately.

### Preparing a release
To prepare a crossplatform SMAPI release, you'll need to compile it on two platforms. See
[crossplatforming info](https://stardewvalleywiki.com/Modding:Modder_Guide/Test_and_Troubleshoot#Testing_on_all_platforms)
on the wiki for the first-time setup.

1. Update the version number in `GlobalAssemblyInfo.cs` and `Constants::Version`. Make sure you use a
   [semantic version](https://semver.org). Recommended format:

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

## Customisation
### Configuration file
You can customise the SMAPI behaviour by editing the `smapi-internal/StardewModdingAPI.config.json`
file in your game folder.

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
`<DefineConstants>` element in `StardewModdingAPI.csproj`. Supported constants:

flag | purpose
---- | -------
`SMAPI_FOR_WINDOWS` | Whether SMAPI is being compiled on Windows for players on Windows. Set automatically in `crossplatform.targets`.

# SMAPI web services
## Overview
The `StardewModdingAPI.Web` project provides an API and web UI hosted at `*.smapi.io`.

### Log parser
The log parser provides a web UI for uploading, parsing, and sharing SMAPI logs. The logs are
persisted in a compressed form to Pastebin.

The log parser lives at https://log.smapi.io.

### Web API
SMAPI provides a web API at `api.smapi.io` for use by SMAPI and external tools. The URL includes a
`{version}` token, which is the SMAPI version for backwards compatibility. This API is publicly
accessible but not officially released; it may change at any time.

The API has one `/mods` endpoint. This provides mod info, including official versions and URLs
(from Chucklefish, GitHub, or Nexus), unofficial versions from the wiki, and optional mod metadata
from the wiki and SMAPI's internal data. This is used by SMAPI to perform update checks, and by
external tools to fetch mod data.

The API accepts a `POST` request with the mods to match, each of which **must** specify an ID and
may _optionally_ specify [update keys](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Manifest#Update_checks).
The API will automatically try to fetch known update keys from the wiki and internal data based on
the given ID.

```
POST https://api.smapi.io/v2.0/mods
{
   "mods": [
      {
         "id": "Pathoschild.LookupAnything",
         "updateKeys": [ "nexus:541", "chucklefish:4250" ]
      }
   ],
   "includeExtendedMetadata": true
}
```

The API will automatically aggregate versions and errors. Each mod will include...
* an `id` (matching what you passed in);
* up to three versions: `main` (e.g. 'latest version' field on Nexus), `optional` if newer (e.g.
  optional files on Nexus), and `unofficial` if newer (from the wiki);
* `metadata` with mod info crossreferenced from the wiki and internal data (only if you specified
  `includeExtendedMetadata: true`);
* and `errors` containing any error messages that occurred while fetching data.

For example:
```
[
   {
      "id": "Pathoschild.LookupAnything",
      "main": {
         "version": "1.19",
         "url": "https://www.nexusmods.com/stardewvalley/mods/541"
      },
      "metadata": {
         "id": [
            "Pathoschild.LookupAnything",
            "LookupAnything"
         ],
         "name": "Lookup Anything",
         "nexusID": 541,
         "gitHubRepo": "Pathoschild/StardewMods",
         "compatibilityStatus": "Ok",
         "compatibilitySummary": "✓ use latest version."
      },
      "errors": []
   }
]
```

## Development
### Local development
`StardewModdingAPI.Web` is a regular ASP.NET MVC Core app, so you can just launch it from within
Visual Studio to run a local version.

There are two differences when it's run locally: all endpoints use HTTP instead of HTTPS, and the
subdomain portion becomes a route (e.g. `log.smapi.io` &rarr; `localhost:59482/log`).

Before running it locally, you need to enter your credentials in the `appsettings.Development.json`
file. See the next section for a description of each setting. This file is listed in `.gitignore`
to prevent accidentally committing credentials.

### Deploying to Amazon Beanstalk
The app can be deployed to a standard Amazon Beanstalk IIS environment. When creating the
environment, make sure to specify the following environment properties:

property name                   | description
------------------------------- | -----------------
`LogParser:PastebinDevKey`      | The [Pastebin developer key](https://pastebin.com/api#1) used to authenticate with the Pastebin API.
`LogParser:PastebinUserKey`     | The [Pastebin user key](https://pastebin.com/api#8) used to authenticate with the Pastebin API. Can be left blank to post anonymously.
`LogParser:SectionUrl`          | The root URL of the log page, like `https://log.smapi.io/`.
`ModUpdateCheck:GitHubPassword` | The password with which to authenticate to GitHub when fetching release info.
`ModUpdateCheck:GitHubUsername` | The username with which to authenticate to GitHub when fetching release info.

## Mod build config package
### Overview
The mod build config package is a NuGet package that mods reference to automatically set up
references, configure the build, and add analyzers specific to Stardew Valley mods.

This involves three projects:

project                                           | purpose
------------------------------------------------- | ----------------
`StardewModdingAPI.ModBuildConfig`                | Configures the build (references, deploying the mod files, setting up debugging, etc).
`StardewModdingAPI.ModBuildConfig.Analyzer`       | Adds C# analyzers which show code warnings in Visual Studio.
`StardewModdingAPI.ModBuildConfig.Analyzer.Tests` | Unit tests for the C# analyzers.

When the projects are built, the relevant files are copied into `bin/Pathoschild.Stardew.ModBuildConfig`.

### Preparing a build
To prepare a build of the NuGet package:
1. Install the [NuGet CLI](https://docs.microsoft.com/en-us/nuget/install-nuget-client-tools#nugetexe-cli).
1. Change the version and release notes in `package.nuspec`.
2. Rebuild the solution in _Release_ mode.
3. Open a terminal in the `bin/Pathoschild.Stardew.ModBuildConfig` package and run this command:
   ```bash
   nuget.exe pack
   ```

That will create a `Pathoschild.Stardew.ModBuildConfig-<version>.nupkg` file in the same directory
which can be uploaded to NuGet or referenced directly.
