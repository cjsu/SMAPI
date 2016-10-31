![](https://raw.githubusercontent.com/Gormogon/SMAPI/master/docs/imgs/SMAPI.png)

**SMAPI** is an [open-source](LICENSE) modding API for [Stardew Valley](http://stardewvalley.net/).
It takes care of loading mods into the game context, and exposes events they can use to interact
with the game. It's safely installed alongside the game's executable, and doesn't change any of
your game files.

## For players

* [How to install SMAPI & use mods](http://canimod.com/guides/using-mods#installing-smapi)
* [Support forums](http://community.playstarbound.com/threads/stardew-modding-api-0-40-1-1.108375/)
* [Stardew Valley Discord](https://discord.gg/KCJHWhX) (chat with players and developers)

## For mod developers

* [How to develop mods](http://canimod.com/guides/creating-a-smapi-mod)
* [SMAPI change log](CHANGELOG.md)
* [SMAPI/Farmhand Discord](https://discordapp.com/invite/0t3fh2xhHVc6Vdyx) (chat with SMAPI developers)

## For SMAPI developers

### Compiling from source
Using one of the SMAPI releases is recommended for most users.

If you'd like to compile SMAPI from source, you can do that on any platform. SMAPI uses build
configuration derived from the [crosswiki mod config](https://github.com/Pathoschild/Stardew.ModBuildConfig#readme)
to detect your current OS automatically and load the correct references.

### Preparing a release

1. Open the project in [Visual Studio](https://www.visualstudio.com/vs/community/) or [MonoDevelop](http://www.monodevelop.com/).
2. Switch to _Release_ build mode.
3. Update the version number in `AssemblyInfo.cs`.
4. Update the version number in `Constants::Version`. Add the minimum game version and target
   platform at the end of the version number (like `0.41.0 1.1 for Windows`).
5. Build the solution.
6. Copy the files for the target platform into the archive structure below.
7. Repeat for each platform.

The release should consist of three files like this:

```
SMAPI-1.0-Linux.tar.gz
   Mods/*
   Newtonsoft.Json.dll
   StardewModdingAPI
   StardewModdingAPI.exe
   StardewModdingAPI.mdb
   System.Numerics.dll

SMAPI-1.0-Mac.tar.gz
   Mods/*
   Newtonsoft.Json.dll
   StardewModdingAPI
   StardewModdingAPI.exe
   StardewModdingAPI.mdb
   System.Numerics.dll

SMAPI-1.0-Windows.zip
   Mods/*
   StardewModdingAPI.exe
   StardewModdingAPI.pdb
```
