# Release notes
## 2.0 (upcoming)
<!--See [log](https://github.com/Pathoschild/SMAPI/compare/1.10...2.0).-->

For players:
* The SMAPI console is now much simpler and easier-to-read.
* The SMAPI console now uses more readable colors in terminals with a light background.

For mod developers:
* Added APIs to edit or inject XNBs loaded by the game at runtime, which let mods do anything previously only possible with XNB mods.
* Added `InputEvents` which combine keyboard + mouse + controller input for simpler input handling; add metadata like the cursor position and grab tile for simpler click handling; and add an option to suppress input from the game.
* Added support for optional dependencies.
* Added support for string versions (like `"1.0-alpha"`) in `manifest.json`.
* Removed all deprecated code.
* Removed support for mods with no `Name`, `Version`, or `UniqueID` in their manifest.
* Removed support for mods with a non-unique `UniqueID` value in their manifest.
* Updated compatibility list.

## 1.15.1
For players:
* Fixed controller mod input broken in 1.15.
* Fixed TrainerMod packaging unneeded files.

For modders:
* Fixed mod registry lookups by unique ID not being case-insensitive.

## 1.15
For players:
* Cleaned up SMAPI console a bit.
* Revamped TrainerMod's item commands:
  * `player_add` is a new command to add any item to your inventory (including tools, weapons, equipment, craftables, wallpaper, etc). This replaces the former `player_additem`, `player_addring`, and `player_addweapon`.
  * `list_items` now shows all items in the game. You can search by item type like `list_items weapon`, or search by item name like `list_items galaxy sword`.
  * `list_items` now also matches translated item names when playing in another language.
  * `list_item_types` is a new command to see a list of item types.
* Fixed unhelpful error when a `config.json` is invalid.
* Fixed rare crash when window loses focus for a few players (further to fix in 1.14).
* Fixed invalid `ObjectInformation.xnb` causing a flood of warnings; SMAPI now shows one error instead.
* Updated mod compatibility list.

For modders:
* Added `SDate` utility for in-game date calculations (see [API reference](http://stardewvalleywiki.com/Modding:SMAPI_APIs#Dates)).
* Added support for minimum dependency versions in `manifest.json` (see [API reference](http://stardewvalleywiki.com/Modding:SMAPI_APIs#Manifest)).
* Added more useful logging when loading mods.
* Added a `ModID` property to all mod helpers for extension methods.
* Changed `manifest.MinimumApiVersion` from string to `ISemanticVersion`. This shouldn't affect mods unless they referenced that field in code.
* Fixed `SemanticVersion` parsing some invalid versions into close approximations (like `1.apple` &rarr; `1.0-apple`).
* Fixed `SemanticVersion` not treating hyphens as separators when comparing prerelease tags.  
  <small>_(While that was technically correct, it leads to unintuitive behaviour like sorting `-alpha-2` _after_ `-alpha-10`, even though `-alpha.2` sorts before `-alpha.10`.)_</small>
* Fixed corrupted state exceptions not being logged by SMAPI.
* Increased all deprecations to _pending removal_.

For SMAPI developers:
* Added SMAPI 2.0 compile mode, for testing how mods will work with SMAPI 2.0.
* Added prototype SMAPI 2.0 feature to override XNB files (not enabled for mods yet).
* Added prototype SMAPI 2.0 support for version strings in `manifest.json` (not recommended for mods yet).
* Compiling SMAPI now uses your `~/stardewvalley.targets` file if present.

## 1.14
See [log](https://github.com/Pathoschild/SMAPI/compare/1.13...1.14).

For players:
* SMAPI now shows friendly errors when...
  * it can't detect the game;
  * a mod dependency is missing (if it's listed in the mod manifest);
  * you have Stardew Valley 1.11 or earlier (which aren't compatible);
  * you run `install.exe` from within the downloaded zip file.
* Fixed "unknown mod" deprecation warnings by improving how SMAPI detects the mod using the event.
* Fixed `libgdiplus.dylib` errors for some players on Mac.
* Fixed rare crash when window loses focus for a few players.
* Bumped minimum game version to 1.2.30.
* Updated mod compatibility list.

For modders:
* You can now add dependencies to `manifest.json` (see [API reference](http://stardewvalleywiki.com/Modding:SMAPI_APIs#Manifest)).
* You can now translate your mod (see [API reference](http://stardewvalleywiki.com/Modding:SMAPI_APIs#Translation)).
* You can now load unpacked `.tbin` files from your mod folder through the content API.  
* SMAPI now automatically fixes tilesheet references for maps loaded from the mod folder.  
  <small>_When loading a map from the mod folder, SMAPI will automatically use tilesheets relative to the map file if they exists. Otherwise it will default to tilesheets in the game content._</small>
* Added `Context.IsPlayerFree` for mods that need to check if the player can act (i.e. save is loaded, no menu is displayed, no cutscene is in progress, etc).
* Added `Context.IsInDrawLoop` for specialised mods.
* Fixed `smapi-crash.txt` being copied from the default log even if a different path is specified with `--log-path`.
* Fixed the content API not matching XNB filenames with two dots (like `a.b.xnb`) if you don't specify the `.xnb` extension.
* Fixed `debug` command output not printed to console.
* Deprecated `TimeEvents.DayOfMonthChanged`, `SeasonOfYearChanged`, and `YearOfGameChanged`. These don't do what most modders think they do and aren't very reliable, since they depend on the SMAPI/game lifecycle which can change. You should use `TimeEvents.AfterDayStarted` or `SaveEvents.BeforeSave` instead.

## 1.13.1
For players:
* Fixed errors when loading a mod with no name or version.
* Fixed mods with no manifest `Name` field having no name (SMAPI will now shows their filename).

## 1.13
See [log](https://github.com/Pathoschild/SMAPI/compare/1.12...1.13).

For players:
* SMAPI now recovers better from mod draw errors and detects when the error is irrecoverable.
* SMAPI now recovers automatically from errors in the game loop when possible.
* SMAPI now remembers if your game crashed and offers help next time you launch it.
* Fixed installer sometimes finding redundant game paths.
* Fixed save events not being raised after the first day on Linux/Mac.
* Fixed error on Linux/Mac when a mod loads a PNG immediately after the save is loaded.
* Updated mod compatibility list for Stardew Valley 1.2.

For mod developers:
* Added a `Context.IsWorldReady` flag for mods to use.  
  <small>_This indicates whether a save is loaded and the world is finished initialising, which starts at the same point that `SaveEvents.AfterLoad` and `TimeEvents.AfterDayStarted` are raised. This is mainly useful for events which can be raised before the world is loaded (like update tick)._</small>
* Added a `debug` console command which lets you run the game's debug commands (e.g. `debug warp FarmHouse 1 1` warps you to the farmhouse).
* Added basic context info to logs to simplify troubleshooting.
* Added a `Mod.Dispose` method which can be overriden to clean up before exit. This method isn't guaranteed to be called on every exit.
* Deprecated mods that don't have a `Name`, `Version`, or `UniqueID` in their manifest. These will be required in SMAPI 2.0.
* Deprecated `GameEvents.GameLoaded` and `GameEvents.FirstUpdateTick`. You can move any affected code into your mod's `Entry` method.
* Fixed maps not recognising custom tilesheets added through the SMAPI content API.
* Internal refactoring for upcoming features.

## 1.12
See [log](https://github.com/Pathoschild/SMAPI/compare/1.11...1.12).

For players:
* The installer now lets you choose the install path if you have multiple copies of the game, instead of using the first path found.
* Fixed mod draw errors breaking the game.
* Fixed mods on Linux/Mac no longer working after the game saves.
* Fixed `libgdiplus.dylib` errors on Mac when mods read PNG files.
* Adopted pufferchick.

For mod developers:
* Unknown mod manifest fields are now stored in `IManifest::ExtraFields`.
* The content API now defaults to `ContentSource.ModFolder`.
* Fixed content API error when loading a PNG during early game init (e.g. in mod's `Entry`).
* Fixed content API error when loading an XNB from the mod folder on Mac.

## 1.11
See [log](https://github.com/Pathoschild/SMAPI/compare/1.10...1.11).

For players:
* SMAPI now detects issues in `ObjectInformation.xnb` files caused by outdated XNB mods.
* Errors when loading a save are now shown in the SMAPI console.
* Improved console logging performance.
* Fixed errors during game update causing the game to hang.
* Fixed errors due to mod events triggering during game save in Stardew Valley 1.2.

For mod developers:
* Added a content API which loads custom textures/maps/data from the mod's folder (`.xnb` or `.png` format) or game content.
* `Console.Out` messages are now written to the log file.
* `Monitor.ExitGameImmediately` now aborts SMAPI initialisation and events more quickly.
* Fixed value-changed events being raised when the player loads a save due to values being initialised.

## 1.10
See [log](https://github.com/Pathoschild/SMAPI/compare/1.9...1.10).

For players:
* Updated to Stardew Valley 1.2.
* Added logic to rewrite many mods for compatibility with game updates, though some mods may still need an update.
* Fixed `SEHException` errors affecting some players.
* Fixed issue where SMAPI didn't unlock some files on exit.
* Fixed rare issue where the installer would crash trying to delete a bundled mod from `%appdata%`.
* Improved TrainerMod commands:
  * Added `world_setyear` to change the current year.
  * Replaced `player_addmelee` with `player_addweapon` with support for non-melee weapons.

For mod developers:
* Mods are now initialised after the `Initialize`/`LoadContent` phase, which means the `GameEvents.Initialize` and `GameEvents.LoadContent` events are deprecated. You can move any logic in those methods to your mod's `Entry` method.
* Added `IsBetween` and string overloads to the `ISemanticVersion` methods.
* Fixed mouse-changed event never updating prior mouse position.
* Fixed `monitor.ExitGameImmediately` not working correctly.
* Fixed `Constants.SaveFolderName` not set for a new game until the save is created.

## 1.9
See [log](https://github.com/Pathoschild/SMAPI/compare/1.8...1.9).

For players:
* SMAPI now detects incompatible mods and disables them before they cause problems.
* SMAPI now allows mods nested into an otherwise empty parent folder (like `Mods\ModName-1.0\ModName\manifest.json`), since that's a common default behaviour when unpacking mods.
* The installer now detects if you need to update .NET Framework before installing SMAPI.
* The installer now detects if you need to run the game at least once (to let it perform first-time setup) before installing SMAPI.
* The installer on Linux now finds games installed to `~/.steam/steam/steamapps/common/Stardew Valley` too.
* The installer now removes old SMAPI logs to prevent confusion.
* The console now has simpler error messages.
* The console now has improved command handling & feedback.
* The console no longer shows the game's debug output (unless you use a _SMAPI for developers_ build).
* Fixed the game-needs-an-update error not pausing before exit.
* Fixed installer errors for some players when deleting files.
* Fixed installer not ignoring potential game folders that don't contain a Stardew Valley exe.
* Fixed installer not recognising Linux/Mac paths starting with `~/` or containing an escaped space.
* Fixed TrainerMod letting you add invalid items which may crash the game.
* Fixed TrainerMod's `world_downminelevel` command not working.
* Fixed rare issue where mod dependencies would override SMAPI dependencies and cause unpredictable bugs.
* Fixed errors in mods' console command handlers crashing the game.

For mod developers:
* Added a simpler API for console commands (see `helper.ConsoleCommands`).
* Added `TimeEvents.AfterDayStarted` event triggered when a day starts. This happens no matter how the day started (including new game, loaded save, or player went to bed).
* Added `ContentEvents.AfterLocaleChanged` event triggered when the player changes the content language (for the upcoming Stardew Valley 1.2).
* Added `SaveEvents.AfterReturnToTitle` event triggered when the player returns to the title screen (for the upcoming Stardew Valley 1.2).
* Added `helper.Reflection.GetPrivateProperty` method.
* Added a `--log-path` argument to specify the SMAPI log path during testing.
* SMAPI now writes XNA input enums (`Buttons` and `Keys`) to JSON as strings automatically, so mods no longer need to add a `StringEnumConverter` themselves for those.
* The SMAPI log now has a simpler filename.
* The SMAPI log now shows the OS caption (like "Windows 10") instead of its internal version when available.
* The SMAPI log now always uses `\r\n` line endings to simplify crossplatform viewing.
* Fixed `SaveEvents.AfterLoad` being raised during the new-game intro before the player is initialised.
* Fixed SMAPI not recognising `Mod` instances that don't subclass `Mod` directly.
* Several obsolete APIs have been removed (see [deprecation guide](http://canimod.com/guides/updating-a-smapi-mod)),
  and all _notice_-level deprecations have been increased to _info_.
* Removed the experimental `IConfigFile`.

For SMAPI developers:
* Added support for debugging SMAPI on Linux/Mac if supported by the editor.

## 1.8
See [log](https://github.com/Pathoschild/SMAPI/compare/1.7...1.8).

For players:
* Mods no longer generate `.cache` subfolders.
* Fixed multiple issues where mods failed during assembly loading.
* Tweaked install package to reduce confusion.

For mod developers:
* The `SemanticVersion` constructor now accepts a string version.
* Increased deprecation level for `Extensions` to _pending removal_.
* **Warning:** `Assembly.GetExecutingAssembly().Location` will no longer reliably
  return a valid path, because mod assemblies are loaded from memory when rewritten for
  compatibility. This approach has been discouraged since SMAPI 1.3; use `helper.DirectoryPath`
  instead.

For SMAPI developers:
* Rewrote assembly loading from the ground up. The new implementation...
  * is much simpler;
  * eliminates the `.cache` folders by loading rewritten assemblies from memory;
  * ensures DLLs are loaded in leaf-to-root order (i.e. dependencies first);
  * improves dependent assembly resolution;
  * no longer loads DLLs if they're not referenced;
  * reduces log verbosity.

## 1.7
See [log](https://github.com/Pathoschild/SMAPI/compare/1.6...1.7).

For players:
* The console now shows the folder path where mods should be added.
* The console now shows deprecation warnings after the list of loaded mods (instead of intermingled).

For mod developers:
* Added a mod registry which provides metadata about loaded mods.
* The `Entry(…)` method is now deferred until all mods are loaded.
* Fixed `SaveEvents.BeforeSave` and `.AfterSave` not triggering on days when the player shipped something.
* Fixed `PlayerEvents.LoadedGame` and `SaveEvents.AfterLoad` being fired before the world finishes initialising.
* Fixed some `LocationEvents`, `PlayerEvents`, and `TimeEvents` being fired during game startup.
* Increased deprecation levels for `SObject`, `LogWriter` (not `Log`), and `Mod.Entry(ModHelper)` (not `Mod.Entry(IModHelper)`) to _pending removal_. Increased deprecation levels for `Mod.PerSaveConfigFolder`, `Mod.PerSaveConfigPath`, and `Version.VersionString` to _info_.

## 1.6
See [log](https://github.com/Pathoschild/SMAPI/compare/1.5...1.6).

For players:
* Added console commands to open the game/data folders.
* Updated list of incompatible mods.
* Fixed `config.json` values being duplicated in some cases.
* Fixed some Linux users not being able to launch SMAPI from Steam.
* Fixed the installer not finding custom install paths on 32-bit Windows.
* Fixed error when loading a mod which was released with a `.cache` folder for a different platform.
* Fixed error when the console doesn't support colour.
* Fixed error when a mod reads a custom JSON file from a directory that doesn't exist.

For mod developers:
* Added three events: `SaveEvents.BeforeSave`, `SaveEvents.AfterSave`, and `SaveEvents.AfterLoad`.
* Deprecated three events:
  * `TimeEvents.OnNewDay` is unreliable; use `TimeEvents.DayOfMonthChanged` or `SaveEvents` instead.
  * `PlayerEvents.LoadedGame` is replaced by `SaveEvents.AfterLoad`.
  * `PlayerEvents.FarmerChanged` serves no purpose.

For SMAPI developers:
  * Added support for specifying a lower bound in mod incompatibility data.
  * Added support for custom incompatible-mod error text.
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
