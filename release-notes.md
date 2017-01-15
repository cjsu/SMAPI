# Release notes

## 1.6 (upcoming)
See [log](https://github.com/Pathoschild/SMAPI/compare/stable...develop).

For players:
* Added console commands to open the game & data folders in the system's file browser.
* Fixed issue where the installer couldn't find the game for some players on 32-bit Windows.
* Fixed issue where SMAPI couldn't be launched from Steam for some Linux players.
* Fixed issue where values in `config.json` were duplicated in some cases.
* Fixed error loading mods that were released with a `.cache` folder from a different platform.
* Fixed error when the console doesn't support colour.
* Updated list of incompatible mods.

For mod developers:
* Fixed error when reading a custom JSON file from a directory that doesn't exist.

For SMAPI developers:
  * Added support for specifying a lower bound in mod incompatibility data.
  * Added support for custom incompatible-mod-version error text.
  * Fixed issue where `TrainerMod` used older logic to detect the game path.

## 1.5
See [log](https://github.com/Pathoschild/SMAPI/compare/1.4...1.5).

For players:
  * Added an option to disable update checks.
  * SMAPI will now show a friendly error with update links when you try to use a known incompatible mod version.
  * Fixed an error when a mod uses the new reflection API on a missing field or method.
  * Fixed an issue where mods weren't notified of a menu change if it changed while SMAPI was still notifying mods of the previous change.

For developers:
  * Deprecated `Version` in favour of `SemanticVersion`.  
    _This new implementation is [semver 2.0](http://semver.org/)-compliant, introduces `NewerThan(version)` and `OlderThan(version)` convenience methods, adds support for parsing a version string into a `SemanticVersion`, and fixes various bugs with the former implementation. This also replaces `Manifest` with `IManifest`._
  * Increased deprecation levels for `SObject`, `Extensions`, `LogWriter` (not `Log`), `SPlayer`, and `Mod.Entry(ModHelper)` (not `Mod.Entry(IModHelper)`).

## 1.4
See [log](https://github.com/Pathoschild/SMAPI/compare/1.3...1.4).

For players:
  * SMAPI will now prevent mods from crashing your game with menu errors.
  * The installer will now automatically detect most custom install paths.
  * The installer will now automatically clean up old SMAPI files.
  * Each mod now has its own `.cache` folder, so removing the mod won't leave orphaned cache files behind.
  * Improved installer wording to reduce confusion.
  * Fixed the installer not removing TrainerMod from appdata if it's already in the game mods directory.
  * Fixed the installer not moving mods out of appdata if the game isn't installed on the same Windows partition.
  * Fixed the SMAPI console not being shown on Linux and Mac.

For developers:
  * Added a reflection API (via `helper.Reflection`) that simplifies robust access to the game's private fields and methods.
  * Added a searchable `list_items` console command to replace the `out_items`, `out_melee`, and `out_rings` commands.
  * Added `TypeLoadException` details when intercepted by SMAPI.
  * Fixed an issue where you couldn't debug into an assembly because it was copied into the `.cache` directory. That will now only happen if necessary.

## 1.3
See [log](https://github.com/Pathoschild/SMAPI/compare/1.2...1.3).

For players:
  * You can now run most mods on any platform (e.g. run Windows mods on Linux/Mac).
  * Fixed the normal uninstaller not removing files added by the 'SMAPI for developers' installer.

## 1.2
See [log](https://github.com/Pathoschild/SMAPI/compare/1.1.1...1.2).

For players:
  * Fixed compatibility with some older mods.
  * Fixed mod errors in most event handlers crashing the game.
  * Fixed mod errors in some event handlers preventing other mods from receiving the same event.
  * Fixed game crashing on startup with an audio error for some players.

For developers:
  * Improved logging to show `ReflectionTypeLoadException` details when it's caught by SMAPI.

## 1.1
See [log](https://github.com/Pathoschild/SMAPI/compare/1.0...1.1.1).

For players:
  * Fixed console exiting immediately when some exceptions occur.
  * Fixed an error in 1.0 when mod uses `config.json` but the file doesn't exist.
  * Fixed critical errors being saved to a separate log file.
  * Fixed compatibility with some older mods.<sup>1.1.1</sup>
  * Fixed race condition where some mods would sometimes crash because the game wasn't ready yet.<sup>1.1.1</sup>

For developers:
  * Added new logging interface:
    * easier to use;
    * supports trace logs (written to the log file, but hidden in the console by default);
    * messages are now listed in order;
    * messages now show which mod logged them;
    * more consistent and intuitive console color scheme.
  * Added optional `MinimumApiVersion` to `manifest.json`.
  * Added emergency interrupt feature for dangerous mods.
  * Fixed deprecation warnings being repeated if the mod can't be identified.<sup>1.1.1</sup>

## 1.0
See [log](https://github.com/Pathoschild/SMAPI/compare/0.40.1.1-3...1.0).

For players:
  * Added support for Linux and Mac.
  * Added installer to automate adding & removing SMAPI.
  * Added background update check on launch.
  * Fixed missing `steam_appid.txt` file.
  * Fixed some mod UIs disappearing at a non-default zoom level for some users.
  * Removed undocumented support for mods in AppData folder **(breaking change)**.
  * Removed `F2` debug mode.

For mod developers:
  * Added deprecation warnings.
  * Added OS version to log.
  * Added zoom-adjusted mouse position to mouse-changed event arguments.
  * Added SMAPI code documentation.
  * Switched to [semantic versioning](http://semver.org).
  * Fixed mod versions not shown correctly in the log.
  * Fixed misspelled field in `manifest.json` schema.
  * Fixed some events getting wrong data.
  * Simplified log output.

For SMAPI developers:
  * Simplified compiling from source.
  * Formalised release process and added automated build packaging.
  * Removed obsolete and unfinished code.
  * Internal cleanup & refactoring.

## 0.x
* 0.40.1.1 (2016-09-30, [log](https://github.com/Pathoschild/SMAPI/compare/0.40.0...0.40.1.1-3))
  * Added support for Stardew Valley 1.1.

* 0.40.0 (2016-04-05, [log](https://github.com/Pathoschild/SMAPI/compare/0.39.7...0.40.0))
  * Fixed an error that ocurred during minigames.

* 0.39.7 (2016-04-04, [log](https://github.com/Pathoschild/SMAPI/compare/0.39.6...0.39.7))
  * Added 'no check' graphics events that are triggered regardless of game's if checks.

* 0.39.6 (2016-04-01, [log](https://github.com/Pathoschild/SMAPI/compare/0.39.5...0.39.6))
  * Added game & SMAPI versions to log.
  * Fixed conflict in graphics tick events.
  * Bug fixes.

* 0.39.5 (2016-03-30, [log](https://github.com/Pathoschild/SMAPI/compare/0.39.4...0.39.5))
* 0.39.4 (2016-03-29, [log](https://github.com/Pathoschild/SMAPI/compare/0.39.3...0.39.4))
* 0.39.3 (2016-03-28, [log](https://github.com/Pathoschild/SMAPI/compare/0.39.2...0.39.3))
* 0.39.2 (2016-03-23, [log](https://github.com/Pathoschild/SMAPI/compare/0.39.1...0.39.2))
* 0.39.1 (2016-03-23, [log](https://github.com/Pathoschild/SMAPI/compare/0.38.8...0.39.1))
* 0.38.8 (2016-03-23, [log](https://github.com/Pathoschild/SMAPI/compare/0.38.7...0.38.8))
* 0.38.7 (2016-03-23, [log](https://github.com/Pathoschild/SMAPI/compare/0.38.6...0.38.7))
* 0.38.6 (2016-03-22, [log](https://github.com/Pathoschild/SMAPI/compare/0.38.5...0.38.6))
* 0.38.5 (2016-03-22, [log](https://github.com/Pathoschild/SMAPI/compare/0.38.4...0.38.5))
* 0.38.4 (2016-03-21, [log](https://github.com/Pathoschild/SMAPI/compare/0.38.3...0.38.4))
* 0.38.3 (2016-03-21, [log](https://github.com/Pathoschild/SMAPI/compare/0.38.2...0.38.3))
* 0.38.2 (2016-03-21, [log](https://github.com/Pathoschild/SMAPI/compare/0.38.0...0.38.2))
* 0.38.0 (2016-03-20, [log](https://github.com/Pathoschild/SMAPI/compare/0.38.1...0.38.0))
* 0.38.1 (2016-03-20, [log](https://github.com/Pathoschild/SMAPI/compare/0.37.3...0.38.1))
* 0.37.3 (2016-03-08, [log](https://github.com/Pathoschild/SMAPI/compare/0.37.2...0.37.3))
* 0.37.2 (2016-03-07, [log](https://github.com/Pathoschild/SMAPI/compare/0.37.1...0.37.2))
* 0.37.1 (2016-03-06, [log](https://github.com/Pathoschild/SMAPI/compare/0.36...0.37.1))
* 0.36 (2016-03-04, [log](https://github.com/Pathoschild/SMAPI/compare/0.37...0.36))
* 0.37 (2016-03-04, [log](https://github.com/Pathoschild/SMAPI/compare/0.35...0.37))
* 0.35 (2016-03-02, [log](https://github.com/Pathoschild/SMAPI/compare/0.34...0.35))
* 0.34 (2016-03-02, [log](https://github.com/Pathoschild/SMAPI/compare/0.33...0.34))
* 0.33 (2016-03-02, [log](https://github.com/Pathoschild/SMAPI/compare/0.32...0.33))
* 0.32 (2016-03-02, [log](https://github.com/Pathoschild/SMAPI/compare/0.31...0.32))
* 0.31 (2016-03-02, [log](https://github.com/Pathoschild/SMAPI/compare/0.3...0.31))
* 0.3 (2016-03-01, [log](https://github.com/Pathoschild/SMAPI/compare/Alpha0.2...0.3))
* 0.2 (2016-02-29, [log](https://github.com/Pathoschild/SMAPI/compare/Alpha0.1...Alpha0.2)
* 0.1 (2016-02-28)
