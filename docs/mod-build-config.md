The **mod build package** is an open-source NuGet package which automates the MSBuild configuration
for SMAPI mods.

The package...

* detects your game install path;
* adds the assembly references you need (with automatic support for Linux/Mac/Windows);
* packages the mod into your `Mods` folder when you rebuild the code (configurable);
* configures Visual Studio to enable debugging into the code when the game is running (_Windows only_);
* adds C# analyzers to warn for Stardew Valley-specific issues.

## Contents
* [Install](#install)
* [Configure](#configure)
* [Code analysis warnings](#code-analysis-warnings)
* [Troubleshoot](#troubleshoot)
* [Release notes](#release-notes)

## Install
**When creating a new mod:**

1. Create an empty library project.
2. Reference the [`Pathoschild.Stardew.ModBuildConfig` NuGet package](https://www.nuget.org/packages/Pathoschild.Stardew.ModBuildConfig).
3. [Write your code](https://stardewvalleywiki.com/Modding:Creating_a_SMAPI_mod).
4. Compile on any platform.

**When migrating an existing mod:**

1. Remove any project references to `Microsoft.Xna.*`, `MonoGame`, Stardew Valley,
   `StardewModdingAPI`, and `xTile`.
2. Reference the [`Pathoschild.Stardew.ModBuildConfig` NuGet package](https://www.nuget.org/packages/Pathoschild.Stardew.ModBuildConfig).
3. Compile on any platform.

## Configure
### Deploy files into the `Mods` folder
By default, your mod will be copied into the game's `Mods` folder (with a subfolder matching your
project name) when you rebuild the code. The package will automatically include your
`manifest.json`, any `i18n` files, and the build output.

To add custom files to the mod folder, just [add them to the build output](https://stackoverflow.com/a/10828462/262123).
(If your project references another mod, make sure the reference is [_not_ marked 'copy local'](https://msdn.microsoft.com/en-us/library/t1zz5y8c(v=vs.100).aspx).)

You can change the mod's folder name by adding this above the first `</PropertyGroup>` in your
`.csproj`:
```xml
<ModFolderName>YourModName</ModFolderName>
```

If you don't want to deploy the mod automatically, you can add this:
```xml
<EnableModDeploy>False</EnableModDeploy>
```

### Create release zip
By default, a zip file will be created in the build output when you rebuild the code. This zip file
contains all the files needed to share your mod in the recommended format for uploading to Nexus
Mods or other sites.

You can change the zipped folder name (and zip name) by adding this above the first
`</PropertyGroup>` in your `.csproj`:
```xml
<ModFolderName>YourModName</ModFolderName>
```

You can change the folder path where the zip is created like this:
```xml
<ModZipPath>$(SolutionDir)\_releases</ModZipPath>
```

Finally, you can disable the zip creation with this:
```xml
<EnableModZip>False</EnableModZip>
```

Or only create it in release builds with this:
```xml
<EnableModZip Condition="$(Configuration) != 'Release'">False</EnableModZip>
```

### Game path
The package usually detects where your game is installed automatically. If it can't find your game
or you have multiple installs, you can specify the path yourself. There's two ways to do that:

* **Option 1: global game path (recommended).**  
  _This will apply to every project that uses the package._

  1. Get the full folder path containing the Stardew Valley executable.
  2. Create this file:
  
     platform  | path
     --------- | ----
     Linux/Mac | `~/stardewvalley.targets`
     Windows   | `%USERPROFILE%\stardewvalley.targets`

  3. Save the file with this content:

     ```xml
     <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
        <PropertyGroup>
          <GamePath>PATH_HERE</GamePath>
        </PropertyGroup>
     </Project>
     ```

  4. Replace `PATH_HERE` with your game path.

* **Option 2: path in the project file.**  
  _You'll need to do this for each project that uses the package._

  1. Get the folder path containing the Stardew Valley `.exe` file.
  2. Add this to your `.csproj` file under the `<Project` line:

     ```xml
     <PropertyGroup>
       <GamePath>PATH_HERE</GamePath>
     </PropertyGroup>
     ```

  3. Replace `PATH_HERE` with your custom game install path.

The configuration will check your custom path first, then fall back to the default paths (so it'll
still compile on a different computer).

### Ignore files
If you don't want to include a file in the mod folder or release zip:
* Make sure it's not copied to the build output. For a DLL, make sure the reference is [not marked 'copy local'](https://msdn.microsoft.com/en-us/library/t1zz5y8c(v=vs.100).aspx).
* Or add this to your `.csproj` file under the `<Project` line:
  ```xml
  <IgnoreModFilePatterns>\.txt$, \.pdf$</IgnoreModFilePatterns>
  ```
  This is a comma-delimited list of regular expression patterns. If any pattern matches a file's
  relative path in your mod folder, that file won't be included.

### Non-mod projects
You can use the package in non-mod projects too (e.g. unit tests or framework DLLs). You'll need to
disable deploying the mod and creating a release zip:

```xml
<EnableModDeploy>False</EnableModDeploy>
<EnableModZip>False</EnableModZip>
```

If this is for unit tests, you may need to copy the referenced DLLs into your build output too:
```xml
<CopyModReferencesToBuildOutput>True</CopyModReferencesToBuildOutput>
```

## Code warnings
### Overview
The NuGet package adds code warnings in Visual Studio specific to Stardew Valley. For example:  
![](screenshots/code-analyzer-example.png)

You can hide the warnings using the warning ID (shown under 'code' in the Error List). See...
* [for specific code](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/preprocessor-directives/preprocessor-pragma-warning);
* for a method using this attribute:
  ```cs
  [System.Diagnostics.CodeAnalysis.SuppressMessage("SMAPI.CommonErrors", "AvoidNetField")]
  ```
* for an entire project:
  1. Expand the _References_ node for the project in Visual Studio.
  2. Right-click on _Analyzers_ and choose _Open Active Rule Set_.
  4. Expand _StardewModdingAPI.ModBuildConfig.Analyzer_ and uncheck the warnings you want to hide.

See below for help with each specific warning.

### Avoid implicit net field cast
Warning text:
> This implicitly converts '{{expression}}' from {{net type}} to {{other type}}, but
> {{net type}} has unintuitive implicit conversion rules. Consider comparing against the actual
> value instead to avoid bugs.

Stardew Valley uses net types (like `NetBool` and `NetInt`) to handle multiplayer sync. These types
can implicitly convert to their equivalent normal values (like `bool x = new NetBool()`), but their
conversion rules are unintuitive and error-prone. For example,
`item?.category == null && item?.category != null` can both be true at once, and
`building.indoors != null` can be true for a null value.

Suggested fix:
* Some net fields have an equivalent non-net property like `monster.Health` (`int`) instead of
  `monster.health` (`NetInt`). The package will add a separate [AvoidNetField](#avoid-net-field) warning for
  these. Use the suggested property instead.
* For a reference type (i.e. one that can contain `null`), you can use the `.Value` property:
  ```c#
  if (building.indoors.Value == null)
  ```
  Or convert the value before comparison:
  ```c#
  GameLocation indoors = building.indoors;
  if(indoors == null)
     // ...
  ```
* For a value type (i.e. one that can't contain `null`), check if the object is null (if applicable)
  and compare with `.Value`:
  ```cs
  if (item != null && item.category.Value == 0)
  ```

### Avoid net field
Warning text:
> '{{expression}}' is a {{net type}} field; consider using the {{property name}} property instead.

Your code accesses a net field, which has some unusual behavior (see [AvoidImplicitNetFieldCast](#avoid-implicit-net-field-cast)).
This field has an equivalent non-net property that avoids those issues.

Suggested fix: access the suggested property name instead.

### Avoid obsolete field
Warning text:
> The '{{old field}}' field is obsolete and should be replaced with '{{new field}}'.

Your code accesses a field which is obsolete or no longer works. Use the suggested field instead.

## Troubleshoot
### "Failed to find the game install path"
That error means the package couldn't find your game. You can specify the game path yourself; see
_[Game path](#game-path)_ above.

## Release notes
### Upcoming release
* Dropped support for very old versions of SMAPI and Visual Studio.

### 2.2
* Added support for SMAPI 2.8+ (still compatible with earlier versions).
* Added default game paths for 32-bit Windows.
* Fixed valid manifests marked invalid in some cases.

### 2.1
* Added support for Stardew Valley 1.3.
* Added support for non-mod projects.
* Added C# analyzers to warn about implicit conversions of Netcode fields in Stardew Valley 1.3.
* Added option to ignore files by regex pattern.
* Added reference to new SMAPI DLL.
* Fixed some game paths not detected by NuGet package.

### 2.0.2
* Fixed compatibility issue on Linux.

### 2.0.1
* Fixed mod deploy failing to create subfolders if they don't already exist.

### 2.0
* Added: mods are now copied into the `Mods` folder automatically (configurable).
* Added: release zips are now created automatically in your build output folder (configurable).
* Added: mod deploy and release zips now exclude Json.NET automatically, since it's provided by SMAPI.
* Added mod's version to release zip filename.
* Improved errors to simplify troubleshooting.
* Fixed release zip not having a mod folder.
* Fixed release zip failing if mod name contains characters that aren't valid in a filename.

### 1.7.1
* Fixed issue where i18n folders were flattened.
* The manifest/i18n files in the project now take precedence over those in the build output if both
  are present.

### 1.7
* Added option to create release zips on build.
* Added reference to XNA's XACT library for audio-related mods.

### 1.6
* Added support for deploying mod files into `Mods` automatically.
* Added a build error if a game folder is found, but doesn't contain Stardew Valley or SMAPI.

### 1.5
* Added support for setting a custom game path globally.
* Added default GOG path on Mac.

### 1.4
* Fixed detection of non-default game paths on 32-bit Windows.
* Removed support for SilVerPLuM (discontinued).
* Removed support for overriding the target platform (no longer needed since SMAPI crossplatforms
  mods automatically).

### 1.3
* Added support for non-default game paths on Windows.

### 1.2
* Exclude game binaries from mod build output.

### 1.1
* Added support for overriding the target platform.

### 1.0
* Initial release.
* Added support for detecting the game path automatically.
* Added support for injecting XNA/MonoGame references automatically based on the OS.
* Added support for mod builders like SilVerPLuM.
