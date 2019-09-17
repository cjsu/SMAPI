# Release notes
## 2.11.3
Released 13 September 2019 for Stardew Valley 1.3.36.

* For players:
  * SMAPI now prevents invalid items from breaking menus on hover.
  * SMAPI now prevents invalid event preconditions from crashing the game (thanks to berkayylmao!).
  * SMAPI now prevents more invalid dialogue from crashing the game.
  * Fixed errors during early startup not shown before exit.
  * Fixed various error messages and inconsistent spelling.

* For the web UI:
  * When filtering the mod list, clicking a mod link now automatically adds it to the visible mods.
  * Added log parser instructions for Android.
  * Fixed log parser failing in some cases due to time format localisation.

* For modders:
  * `this.Monitor.Log` now defaults to the `Trace` log level instead of `Debug`. The change will only take effect when you recompile the mod.
  * Fixed 'location list changed' verbose log not correctly listing changes.

## 2.11.2
Released 23 April 2019 for Stardew Valley 1.3.36.

* For players:
  * Fixed error when a custom map references certain vanilla tilesheets on Linux/Mac.
  * Fixed compatibility with some Linux distros.

## 2.11.1
Released 17 March 2019 for Stardew Valley 1.3.36.

* For players:
  * Added crops option to `world_clear` console command.
  * Prepared compatibility check for Stardew Valley 1.4.
  * Updated mod compatibility list.
  * Fixed `world_clear` console command removing chests edited to have a debris name.

* For modders:
  * Added support for suppressing false-positive warnings in rare cases.

* For the web UI:
  * The log parser now collapses redundant sections by default.
  * Fixed log parser column resize bug.

## 2.11
Released 01 March 2019 for Stardew Valley 1.3.36.

* For players:
  * Updated for Stardew Valley 1.3.36.

* For modders:
  * Bumped all deprecation levels to _pending removal_.

* For the web UI:
  * The log parser now shows available updates in a section at the top.
  * The mod compatibility page now crosses out mod links if they're outdated to avoid confusion.
  * Fixed smapi.io linking to an archived download in rare cases.

## 2.10.2
Released 09 January 2019 for Stardew Valley 1.3.32–33.

* For players:
  * SMAPI now keeps the first save backup created for the day, instead of the last one.
  * Fixed save backup for some Linux/Mac players. (When compression isn't available, SMAPI will now create uncompressed backups instead.)
  * Fixed some common dependencies not linking to the mod page in 'missing mod' errors.
  * Fixed 'unknown mod' deprecation warnings showing a stack trace when developers mode not enabled.
  * Fixed 'unknown mod' deprecation warnings when they occur in the Mod constructor.
  * Fixed confusing error message when using SMAPI 2.10._x_ with Stardew Valley 1.3.35+.
  * Tweaked XNB mod message for clarity.
  * Updated compatibility list.

* For the web UI:
  * Added beta status filter to compatibility list.
  * Fixed broken ModDrop links in the compatibility list.

* For modders:
  * Asset changes are now propagated into the parsed save being loaded if applicable.
  * Added locale to context trace logs.
  * Fixed error loading custom map tilesheets in some cases.
  * Fixed error when swapping maps mid-session for a location with interior doors.
  * Fixed `Constants.SaveFolderName` and `CurrentSavePath` not available during early load stages when using `Specialised.LoadStageChanged` event.
  * Fixed `LoadStage.SaveParsed` raised before the parsed save data is available.
  * Fixed 'unknown mod' deprecation warnings showing the wrong stack trace.
  * Fixed `e.Cursor` in input events showing wrong grab tile when player using a controller moves without moving the viewpoint.
  * Fixed incorrect 'bypassed safety checks' warning for mods using the new `Specialised.LoadStageChanged` event in 2.10.
  * Deprecated `EntryDll` values whose capitalisation don't match the actual file. (This works on Windows, but causes errors for Linux/Mac players.)

## 2.10.1
Released 30 December 2018 for Stardew Valley 1.3.32–33.

* For players:
  * Fixed some mod integrations not working correctly in SMAPI 2.10.

## 2.10
Released 29 December 2018 for Stardew Valley 1.3.32–33.

* For players:
  * Added `world_clear` console command to remove spawned or placed entities.
  * Minor performance improvements.
  * Tweaked installer to reduce antivirus false positives.

* For modders:
  * Added [events](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events): `GameLoop.OneSecondUpdateTicking`, `GameLoop.OneSecondUpdateTicked`, and `Specialised.LoadStageChanged`.
  * Added `e.IsCurrentLocation` event arg to `World` events.
  * You can now use `helper.Data.Read/WriteSaveData` as soon as the save is loaded (instead of once the world is initialised).
  * Increased deprecation levels to _info_ for the upcoming SMAPI 3.0.

* For the web UI:
  * Reduced mod compatibility list's cache time.

## 2.9.3
Released 16 December 2018 for Stardew Valley 1.3.32.

* For players:
  * Fixed errors hovering items in some cases with SMAPI 2.9.2.
  * Fixed some multiplayer features broken when a farmhand returns to title and rejoins.

## 2.9.2
Released 16 December 2018 for Stardew Valley 1.3.32.

* For players:
  * SMAPI now prevents invalid items from crashing the game on hover.
  * Fixed some multiplayer features broken when connecting via Steam friends.
  * Fixed cryptic error message when the game isn't installed correctly.
  * Fixed error when a mod makes invalid changes to an NPC schedule.
  * Fixed game launch errors logged as `SMAPI` instead of `game`.
  * Fixed Windows installer adding unneeded Unix launcher to game folder.

* For modders:
  * Moved content pack methods into a new [content pack API](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Content_Packs).
  * Fixed invalid NPC data propagated when a mod changes NPC dispositions.
  * Fixed `Display.RenderedWorld` event broken in SMAPI 2.9.1.
  * **Deprecations:**
    * The `assetData.AsDictionary<TKey, TValue>().Set` methods are now deprecated. Mods should access the `Data` property directly instead.
    * The content pack methods directly on `helper` are now deprecated. Mods should use `helper.ContentPacks` instead.

* For SMAPI developers:
  * Added SMAPI 3.0 readiness to mod API data.

## 2.9.1
Released 07 December 2018 for Stardew Valley 1.3.32.

* For players:
  * Fixed crash in SMAPI 2.9 when constructing certain buildings.
  * Fixed error when a map asset is reloaded in rare cases.

## 2.9
Released 07 December 2018 for Stardew Valley 1.3.32.

* For players:
  * Added support for ModDrop in update checks and the mod compatibility list.
  * Added friendly error for Steam players when Steam isn't loaded.
  * Fixed cryptic error when running the installer from inside a zip file on Windows.
  * Fixed error when leaving and rejoining a multiplayer server in the same session.
  * Fixed empty "mods with warnings" list in some cases due to hidden warnings.
  * Fixed Console Commands' handling of tool upgrade levels for item commands.

* For modders:
  * Added ModDrop update keys (see [docs](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Manifest#Update_checks)).
  * Added `IsLocalPlayer` to new player events.
  * Added `helper.CreateTemporaryContentPack` to replace the deprecated `CreateTransitionalContentPack`.
  * Reloading a map asset will now update affected locations.
  * Reloading the `Data\NPCDispositions` asset will now update affected NPCs.
  * Disabled trace messages related to paranoid mode when it's disabled.
  * Fixed world events like `ObjectListChanged` not working in the mines.
  * Fixed some map tilesheets not editable if not playing in English.
  * Fixed newlines in manifest fields not being ignored.
  * Fixed `Display.RenderedWorld` event invoked after overlays are rendered.
  * **Deprecations:**
    * All static events are deprecated and will be removed in SMAPI 3.0. Mods should use the new event system available through `helper.Events` instead; see [_migrate to SMAPI 3.0_](https://stardewvalleywiki.com/Modding:Migrate_to_SMAPI_3.0) for details.

* For the web UI:
  * Added stats to compatibility list.
  * Fixed compatibility list showing beta header when there's no beta in progress.

## 2.8.2
Released 19 November 2018 for Stardew Valley 1.3.32.

* Fixed game crash in MacOS with SMAPI 2.8.

## 2.8.1
Released 19 November 2018 for Stardew Valley 1.3.32.

* Fixed installer error on Windows with SMAPI 2.8.

## 2.8
Released 19 November 2018 for Stardew Valley 1.3.32.

* For players:
  * Reorganised SMAPI files:
    * Moved most SMAPI files into a `smapi-internal` subfolder (so your game folder is less messy).
    * Moved save backups into a `save-backups` subfolder (so they're easier to find).
    * Simplified the installer files to avoid confusion.
  * Added support for organising mods into subfolders.
  * Added support for [ignoring mod folders](https://stardewvalleywiki.com/Modding:Player_Guide/Getting_Started#Install_mods).
  * Update checks now work even for mods without update keys in most cases.
  * SMAPI now prevents a crash caused by mods adding dialogue the game can't parse.
  * SMAPI now recommends a compatible SMAPI version if you have an older game version.
  * Improved various error messages to be more clear and intuitive.
  * Improved compatibility with various Linux shells (thanks to lqdev!), and prefer xterm when available.
  * Fixed transparency issues on Linux/Mac for some mod images.
  * Fixed error when a mod manifest is corrupted.
  * Fixed error when a mod adds an unnamed location.
  * Fixed friendly error no longer shown when SMAPI isn't run from the game folder.
  * Fixed some Windows install paths not detected.
  * Fixed installer duplicating bundled mods if you moved them after the last install.
  * Fixed installer allowing custom mods to be bundled with the install.
  * Fixed some translation issues not shown as warnings.
  * Fixed dependencies not correctly enforced if the dependency is installed but failed to load.
  * Fixed some errors logged as SMAPI instead of the affected mod.
  * Fixed crash log deleted immediately when you relaunch the game.
  * Updated compatibility list.

* For the web UI:
  * Added a [mod compatibility page](https://mods.smapi.io) and [privacy page](https://smapi.io/privacy).
  * The log parser now has a separate filter for game messages.
  * The log parser now shows content pack authors (thanks to danvolchek!).
  * Tweaked log parser UI (thanks to danvolchek!).
  * Fixed log parser instructions for Mac.

* For modders:
  * Added [data API](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Data) to store mod data in the save file or app data.
  * Added [multiplayer API](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Multiplayer) and [events](https://stardewvalleywiki.com/Modding:Modder_Guide/Apis/Events#Multiplayer_2) to send/receive messages and get connected player info.
  * Added [verbose logging](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Logging#Verbose_logging) feature.
  * Added `IContentPack.WriteJsonFile` method.
  * Added IntelliSense documentation for the non-developers version of SMAPI.
  * Added more events to the prototype `helper.Events` for SMAPI 3.0.
  * Added `SkillType` enum constant.
  * Improved content API:
    * added support for overlaying image assets with semi-transparency;
    * mods can now load PNGs even if the game is currently drawing.
  * When comparing mod versions, SMAPI now assigns the lowest precedence to `-unofficial` (e.g. `1.0-beta > 1.0-unofficial`).
  * Fixed content packs' `ReadJsonFile` allowing non-relative paths.
  * Fixed content packs always failing to load if they declare a dependency on a SMAPI mod.
  * Fixed trace logs not showing path for invalid mods.
  * Fixed 'no update keys' warning not shown for mods with only invalid update keys.
  * Fixed `Context.IsPlayerFree` being true while the player is mid-warp in multiplayer.
  * Fixed update-check errors sometimes being overwritten with a generic message.
  * Suppressed the game's 'added crickets' debug output.
  * Updated dependencies (Harmony 1.0.9.1 → 1.2.0.1, Mono.Cecil 0.10 → 0.10.1).
  * **Deprecations:**
    * Non-string manifest versions are now deprecated and will stop working in SMAPI 3.0. Affected mods should use a string version, like `"Version": "1.0.0"`.
    * `ISemanticVersion.Build` is now deprecated and will be removed in SMAPI 3.0. Affected mods should use `ISemanticVersion.PrereleaseTag` instead.
  * **Breaking changes:**
    * `helper.ModRegistry` now returns `IModInfo` instead of `IManifest` directly. This lets SMAPI return more metadata about mods. This doesn't affect any mods that didn't already break in Stardew Valley 1.3.32.
    * Most SMAPI files have been moved into a `smapi-internal` subfolder. This won't affect compiled mod releases, but you'll need to update the build config NuGet package.

* For SMAPI developers:
  * Added support for parallel stable/beta unofficial updates in update checks.
  * Added a 'paranoid warnings' option which reports mods using potentially sensitive .NET APIs (like file or shell access) in the mod issues list.
  * Adjusted `SaveBackup` mod to make it easier to account for custom mod subfolders in the installer.
  * Installer no longer special-cases Omegasis' older `SaveBackup` mod (now named `AdvancedSaveBackup`).
  * Fixed mod web API returning a concatenated name for mods with alternate names.

## 2.7
Released 14 August 2018 for Stardew Valley 1.3.28.

* For players:
  * Updated for Stardew Valley 1.3.28.
  * Improved how mod issues are listed in the console and log.
  * Revamped installer. It now...
    * uses a new format that should be more intuitive;
    * lets players on Linux/Mac choose the console color scheme (SMAPI will auto-detect it on Windows);
    * and validates requirements earlier.
  * Fixed custom festival maps always using spring tilesheets.
  * Fixed `player_add` command not recognising return scepter.
  * Fixed `player_add` command showing fish twice.
  * Fixed some SMAPI logs not deleted when starting a new session.
  * Updated compatibility list.

* For modders:
  * Added support for `.json` data files in the content API (including Content Patcher).
  * Added propagation for asset changes through the content API for...
    * child sprites;
    * dialogue;
    * map tilesheets.
  * Added `--mods-path` CLI command-line argument to switch between mod folders.
  * All enums are now JSON-serialised by name instead of numeric value. (Previously only a few enums were serialised that way. JSON files which already have numeric enum values will still be parsed fine.)
  * Fixed false compatibility error when constructing multidimensional arrays.
  * Fixed `.ToSButton()` methods not being public.

* For SMAPI developers:
  * Dropped support for pre-SMAPI-2.6 update checks in the web API.  
    _These are no longer useful, even if the player still has earlier versions of SMAPI. Older versions of SMAPI won't launch in Stardew Valley 1.3 (so they won't check for updates), and newer versions of SMAPI/mods won't work with older versions of the game._

## 2.6
Released 01 August 2018 for Stardew Valley 1.3.27.

* For players:
  * Updated for Stardew Valley 1.3.
  * Added automatic save backups.
  * Improved update checks:
    * added beta update channel;
    * added update alerts for incompatible mods with an unofficial update on the wiki;
    * added update alerts for optional files on Nexus;
    * added console warning for mods which don't have update checks configured;
    * added more visible prompt in beta channel for SMAPI updates;
    * fixed mod update checks failing if a mod only has prerelease versions on GitHub;
    * fixed Nexus mod update alerts not showing HTTPS links.
  * Improved mod warnings in the console.
  * Improved error when game can't start audio.
  * Improved the Console Commands mod:
    * Added `player_add name`, which adds items to your inventory by name instead of ID.
    * Fixed `world_setseason` not running season-change logic.
    * Fixed `world_setseason` not normalising the season value.
    * Fixed `world_settime` sometimes breaking NPC schedules (e.g. so they stay in bed).
    * Removed the `player_setlevel` and `player_setspeed` commands, which weren't implemented in a useful way. Use a mod like CJB Cheats Menu if you need those.
  * Fixed `SEHException` errors for some players.
  * Fixed performance issues for some players.
  * Fixed default color scheme on Mac or in PowerShell (configurable via `StardewModdingAPI.config.json`).
  * Fixed installer error on Linux/Mac in some cases.
  * Fixed installer not finding some game paths or showing duplicate paths.
  * Fixed installer not removing some SMAPI files.
  * Fixed launch issue for Linux players with some terminals. (Thanks to HanFox and kurumushi!)
  * Fixed abort-retry loop if a mod crashed when intercepting assets during startup.
  * Fixed some mods failing if the player name is blank.
  * Fixed errors when a mod references a missing assembly.
  * Fixed `AssemblyResolutionException` errors in rare cases.
  * Renamed `install.exe` to `install on Windows.exe` to avoid confusion.
  * Updated compatibility list.

* For the web UI:
  * Added option to download SMAPI from Nexus.
  * Added log parser redesign that should be more intuitive.
  * Added log parser option to view raw log.
  * Changed log parser filters to show `DEBUG` messages by default.
  * Fixed design on smaller screens.
  * Fixed log parser issue when content packs have no description.
  * Fixed log parser mangling crossplatform paths in some cases.
  * Fixed `smapi.io/install` not linking to a useful page.

* For modders:
  * Added [input API](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Input) for reading and suppressing keyboard, controller, and mouse input.
  * Added code analysis in the NuGet package to flag common issues as warnings.
  * Replaced `LocationEvents` to support multiplayer:
    * now raised for all locations;
    * now includes added/removed building interiors;
    * each event now provides a list of added/removed values;
    * added buildings-changed event.
  * Added `Context.IsMultiplayer` and `Context.IsMainPlayer` flags.
  * Added `Constants.TargetPlatform` which says whether the game is running on Linux, Mac, or Windows.
  * Added `semanticVersion.IsPrerelease()` method.
  * Added support for launching multiple instances transparently. This removes the former `--log-path` command-line argument.
  * Added support for custom seasonal tilesheets when loading an unpacked `.tbin` map.
  * Added Harmony DLL for internal use by SMAPI. (Mods should still include their own copy for backwards compatibility, and in case it's removed later. SMAPI will always load its own version though.)
  * Added option to suppress update checks for a specific mod in `StardewModdingAPI.config.json`.
  * Added absolute pixels to `ICursorPosition`.
  * Added support for reading/writing `ISemanticVersion` to JSON.
  * Added support for reloading NPC schedules through the content API.
  * Reimplemented the content API so it works more reliably in many edge cases.
  * Reimplemented input suppression to work more consistently in many cases.
  * The order of update keys now affects which URL players see in update alerts.
  * Fixed assets loaded by temporary content managers not being editable by mods.
  * Fixed assets not reloaded consistently when the player switches language.
  * Fixed error if a mod loads a PNG while the game is loading (e.g. custom map tilesheets via `IAssetLoader`).
  * Fixed error if a mod translation file is empty.
  * Fixed input suppression not working consistently for clicks.
  * Fixed console input not saved to the log.
  * Fixed `Context.IsPlayerFree` being false during festivals.
  * Fixed `helper.ModRegistry.GetApi` errors not always mentioning which interface caused the issue.
  * Fixed console commands being invoked asynchronously.
  * Fixed mods able to intercept other mods' assets via the internal asset keys.
  * Fixed mods able to indirectly change other mods' data through shared content caches.
  * Fixed `SemanticVersion` allowing invalid versions in some cases.
  * **Breaking changes** (see [migration guide](https://stardewvalleywiki.com/Modding:Migrate_to_Stardew_Valley_1.3)):
     * Dropped some deprecated APIs.
     * `LocationEvents` have been rewritten.
     * Mods can't intercept chatbox input.
     * Mod IDs should only contain letters, numbers, hyphens, dots, and underscores. That allows their use in many contexts like URLs. This restriction is now enforced. (In regex form: `^[a-zA-Z0-9_.-]+$`.)

* For SMAPI developers:
  * Added more consistent crossplatform handling, including MacOS detection.
  * Added beta update channel.
  * Added optional mod metadata to the web API (including Nexus info, wiki metadata, etc).
  * Added early prototype of SMAPI 3.0 events via `helper.Events`.
  * Added early prototype of mod handler toolkit.
  * Added Harmony for SMAPI's internal use.
  * Added metadata dump option in `StardewModdingAPI.config.json` for troubleshooting some cases.
  * Added more stylish pufferchick on the home page.
  * Rewrote update checks:
    * Moved most logic into the web API.
    * Changed web API to require mod IDs.
    * Changed web API to also fetch metadata from SMAPI's internal mod DB and the wiki.
  * Rewrote world/player state tracking. The new implementation is much more efficient than previous method, uses net field events where available, and lays the groundwork for more advanced events in SMAPI 3.0.
  * Split mod DB out of `StardewModdingAPI.config.json` into its own file.
  * Updated to Mono.Cecil 0.10.

## 2.5.5
Released 11 April 2018 for Stardew Valley 1.2.30–1.2.33.

* For players:
  * Fixed mod not loaded if it has an optional dependency that's loaded but skipped.
  * Fixed mod update alerts not shown if one mod has an invalid remote version.
  * Fixed SMAPI update alerts linking to the GitHub repository instead of [smapi.io](https://smapi.io).
  * Fixed SMAPI update alerts for draft releases.
  * Fixed error when two content packs use different capitalisation for the same required mod ID.
  * Fixed rare crash if the game duplicates an item.

* For the [log parser](https://log.smapi.io):
  * Tweaked UI.

## 2.5.4
Released 26 March 2018 for Stardew Valley 1.2.30–1.2.33.

* For players:
  * Fixed some textures not updated when a mod changes them.
  * Fixed visual bug on Linux/Mac when mods overlay textures.
  * Fixed error when mods remove an asset editor/loader.
  * Fixed minimum game version incorrectly increased in SMAPI 2.5.3.

* For the [log parser](https://log.smapi.io):
  * Fixed error when log text contains certain tokens.

* For modders:
  * Updated to Json.NET 11.0.2.

* For SMAPI developers:
  * Added support for beta update track to support upcoming Stardew Valley 1.3 beta.

## 2.5.3
Released 13 March 2018 for Stardew Valley ~~1.2.30~~–1.2.33.

* For players:
  * Simplified and improved skipped-mod messages.
  * Fixed rare crash with some combinations of manifest fields and internal mod data.
  * Fixed update checks failing for Nexus Mods due to a change in their API.
  * Fixed update checks failing for some older mods with non-standard versions.
  * Fixed failed update checks being cached for an hour (now cached 5 minutes).
  * Fixed error when a content pack needs a mod that couldn't be loaded.
  * Fixed Linux ["magic number is wrong" errors](https://github.com/mono/mono/issues/6752) by changing default terminal order.
  * Updated compatibility list and added update checks for more mods.

* For the [log parser](https://log.smapi.io):
  * Fixed incorrect filtering in some cases.
  * Fixed error if mods have duplicate names.
  * Fixed parse bugs if a mod has no author name.

* For SMAPI developers:
  * Internal changes to support the upcoming Stardew Valley 1.3 update.

## 2.5.2
Released 25 February 2018 for Stardew Valley 1.2.30–1.2.33.

* For modders:
  * Fixed issue where replacing an asset through `asset.AsImage()` or `asset.AsDictionary()` didn't take effect.

* For the [log parser](https://log.smapi.io):
  * Fixed blank page after uploading a log in some cases.

## 2.5.1
Released 24 February 2018 for Stardew Valley 1.2.30–1.2.33.

* For players:
  * Fixed event error in rare cases.

## 2.5
Released 24 February 2018 for Stardew Valley 1.2.30–1.2.33.

* For players:
  * **Added support for [content packs](https://stardewvalleywiki.com/Modding:Content_packs)**.  
    <small>_Content packs are collections of files for a SMAPI mod to load. These can be installed directly under `Mods` like a normal SMAPI mod, get automatic update and compatibility checks, and provide convenient APIs to the mods that read them._</small>
  * Added mod detection for unhandled errors (so most errors now mention which mod caused them).
  * Added install scripts for Linux/Mac (no more manual terminal commands!).
  * Added the missing mod's name and URL to dependency errors.
  * Fixed uninstall script not reporting when done on Linux/Mac.
  * Updated compatibility list and enabled update checks for more mods.

* For modders:
  * Added support for content packs and new APIs to read them.
  * Added support for `ISemanticVersion` in JSON models.
  * Added `SpecialisedEvents.UnvalidatedUpdateTick` event for specialised use cases.
  * Added path normalising to `ReadJsonFile` and `WriteJsonFile` helpers (so no longer need `Path.Combine` with those).
  * Fixed deadlock in rare cases with asset loaders.
  * Fixed unhelpful error when a mod exposes a non-public API.
  * Fixed unhelpful error when a translation file has duplicate keys due to case-insensitivity.
  * Fixed some JSON field names being case-sensitive.

* For the [log parser](https://log.smapi.io):
  * Added support for SMAPI 2.5 content packs.
  * Reduced download size when viewing a parsed log with repeated errors.
  * Improved parse error handling.
  * Fixed 'log started' field showing incorrect date.

* For SMAPI developers:
  * Overhauled mod DB format to be more concise, reduce the memory footprint, and support versioning/defaulting more fields.
  * Reimplemented log parser with serverside parsing and vue.js on the frontend.

## 2.4
Released 24 January 2018 for Stardew Valley 1.2.30–1.2.33.

* For players:
  * Fixed visual map glitch in rare cases.
  * Fixed error parsing JSON files which have curly quotes.
  * Fixed error parsing some JSON files generated on another system.
  * Fixed error parsing some JSON files after mods reload core assemblies, which is no longer allowed.
  * Fixed intermittent errors (e.g. 'collection has been modified') with some mods when loading a save.
  * Fixed compatibility with Linux Terminator terminal.

* For the [log parser](https://log.smapi.io):
  * Fixed error parsing logs with zero installed mods.

* For modders:
  * Added `SaveEvents.BeforeCreate` and `AfterCreate` events.
  * Added `SButton` `IsActionButton()` and `IsUseToolButton()` extensions.
  * Improved JSON parse errors to provide more useful info for troubleshooting.
  * Fixed events being raised while the game is loading a save file.
  * Fixed input events not recognising controller input as an action or use-tool button.
  * Fixed input events setting the same `IsActionButton` and `IsUseToolButton` values for all buttons pressed in an update tick.
  * Fixed semantic versions ignoring `-0` as a prerelease tag.
  * Updated Json.NET to 11.0.1-beta3 (needed to avoid a parser edge case).

* For SMAPI developers:
  * Overhauled input handling to support future input events.

## 2.3
Released 26 December 2017 for Stardew Valley 1.2.30–1.2.33.

* For players:
  * Added a user-friendly [download page](https://smapi.io).
  * Improved cryptic libgdiplus errors on Mac when Mono isn't installed.
  * Fixed mod UIs hidden when menu backgrounds are enabled.

* For modders:
  * **Added mod-provided APIs** to allow simple integrations between mods, even without direct assembly references.
  * Added `GameEvents.FirstUpdateTick` event (called once after all mods are initialised).
  * Added `IsSuppressed` to input events so mods can optionally avoid handling keys another mod has already handled.
  * Added trace message for mods with no update keys.
  * Adjusted reflection API to match actual usage (e.g. renamed `GetPrivate*` to `Get*`), and deprecated previous methods.
  * Fixed `GraphicsEvents.OnPostRenderEvent` not being raised in some specialised cases.
  * Fixed reflection API error for properties missing a `get` and `set`.
  * Fixed issue where a mod could change the cursor position reported to other mods.
  * Updated compatibility list.

* For the [log parser](https://log.smapi.io):
  * Fixed broken favicon.

## 2.2
Released 02 December 2017 for Stardew Valley 1.2.30–1.2.33.

* For players:
  * Fixed error when a mod loads custom assets on Linux/Mac.
  * Fixed error when checking for updates on Linux/Mac due to API HTTPS redirect.
  * Fixed error when Mac adds an `mcs` symlink to the installer package.
  * Fixed `player_add` command not handling tool upgrade levels.
  * Improved error when a mod has an invalid `EntryDLL` filename format.
  * Updated compatibility list.

* For the [log parser](https://log.smapi.io):
  * Logs no longer expire after a week.
  * Fixed error when uploading very large logs.
  * Slightly improved the UI.

* For modders:
  * Added `helper.Content.NormaliseAssetName` method.
  * Added `SDate.DaysSinceStart` property.
  * Fixed input events' `e.SuppressButton(button)` method ignoring specified button.
  * Fixed input events' `e.SuppressButton()` method not working with mouse buttons.

## 2.1
Released 01 November 2017 for Stardew Valley 1.2.30–1.2.33.

* For players:
  * Added a [log parser](https://log.smapi.io) site.
  * Added better Steam instructions to the SMAPI installer.
  * Renamed the bundled _TrainerMod_ to _ConsoleCommands_ to make its purpose clearer.
  * Removed the game's test messages from the console log.
  * Improved update-check errors when playing offline.
  * Fixed compatibility check for players with Stardew Valley 1.08.
  * Fixed `player_setlevel` command not setting XP too.

* For modders:
  * The reflection API now works with public code to simplify mod integrations.
  * The content API now lets you invalidated multiple assets at once.
  * The `InputEvents` have been improved:
    * Added `e.IsActionButton` and `e.IsUseToolButton`.
    * Added `ToSButton()` extension for the game's `Game1.options` button type.
    * Deprecated `e.IsClick`, which is limited and unclear. Use `IsActionButton` or `IsUseToolButton` instead.
    * Fixed `e.SuppressButton()` not correctly suppressing keyboard buttons.
    * Fixed `e.IsClick` (now `e.IsActionButton`) ignoring custom key bindings.
  * `SemanticVersion` can now be constructed from a `System.Version`.
  * Fixed reflection API blocking access to some non-SMAPI members.
  * Fixed content API allowing absolute paths as asset keys.
  * Fixed content API failing to load custom map tilesheets that aren't preloaded.
  * Fixed content API incorrectly detecting duplicate loaders when a mod implements `IAssetLoader` directly.

* For SMAPI developers:
  * Added the installer version and platform to the installer window title to simplify troubleshooting.

## 2.0
Released 14 October 2017 for Stardew Valley 1.2.30–1.2.33.

### Release highlights
* **Mod update checks**  
  SMAPI now checks if your mods have updates available, and will alert you in the console with a convenient link to the
  mod page. This works with mods from the Chucklefish mod site, GitHub, or Nexus Mods. SMAPI 2.0 launches with
  update-check support for over 250 existing mods, and more will be added as modders enable the feature.

* **Mod stability warnings**  
  SMAPI now detects when a mod contains code which can destabilise your game or corrupt your save, and shows a warning
  in the console.

* **Simpler console**  
   The console is now simpler and easier to read, some commands have been streamlined, and the colors now adjust to fit
   your terminal background color.

* **New features for modders**  
  SMAPI 2.0 adds several features to enable new kinds of mods (see
  [API documentation](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs)).

  The **content API** lets you edit, inject, and reload XNB data loaded by the game at any time. This lets SMAPI mods do
  anything previously only possible with XNB mods, and enables new mod scenarios not possible with XNB mods (e.g.
  seasonal textures, NPC clothing that depend on the weather or location, etc).

  The **input events** unify controller + keyboard + mouse input into one event and constant for easy handling, and add
  metadata like the cursor position and grab tile to support click handling. They also let you prevent the game from
  receiving input, to enable new scenarios like action highjacking and UI overlays.

  The mod manifest has a few changes too:
  * The **`UpdateKeys` field** lets you specify your Chucklefish, GitHub, or Nexus mod IDs. SMAPI will automatically
    check for newer versions and notify the player.
  * The **version field** is now a semantic string like `"1.0-alpha"`. (Mods which still use the version structure will
    still work fine.)
  * The **dependencies field** now lets you add optional dependencies which should be loaded first if available.

  Finally, the `SDate` utility now has a `DayOfWeek` field for more convenient date calculations, and `ISemanticVersion`
  now implements `IEquatable<ISemanticVersion>`.

* **Goodbye deprecated code**  
  SMAPI 2.0 removes all deprecated code to unshackle future development. That includes...
  * removed all code marked obsolete;
  * removed TrainerMod's `save` and `load` commands;
  * removed support for mods with no `Name`, `Version`, or `UniqueID` in their manifest;
  * removed support for multiple mods having the same `UniqueID` value;
  * removed access to SMAPI internals through the reflection helper.

* **Command-line install**
  For power users and mod managers, the SMAPI installer can now be scripted using command-line arguments
  (see [technical docs](technical-docs.md#command-line-arguments)).

### Change log
For players:
* SMAPI now alerts you when mods have new versions available.
* SMAPI now warns you about mods which may impact game stability or compatibility.
* The console is now simpler and easier to read, and adjusts its colors to fit your terminal background color.
* Renamed installer folder to avoid confusion.
* Updated compatibility list.
* Fixed update check errors on Linux/Mac.
* Fixed collection-changed errors during startup for some players.

For mod developers:
* Added support for editing, injecting, and reloading XNB data loaded by the game at any time.
* Added support for automatic mod update checks.
* Added unified input events.
* Added support for suppressing input.
* Added support for optional dependencies.
* Added support for specifying the mod version as a string (like `"1.0-alpha"`) in `manifest.json`.
* Added day of week to `SDate` instances.
* Added `IEquatable<ISemanticVersion>` to `ISemanticVersion`.
* Updated Json.NET from 8.0.3 to 10.0.3.
* Removed the TrainerMod's `save` and `load` commands.
* Removed all deprecated code.
* Removed support for mods with no `Name`, `Version`, or `UniqueID` in their manifest.
* Removed support for mods with a non-unique `UniqueID` value in their manifest.
* Removed access to SMAPI internals through the reflection helper, to discourage fragile mods.
* Fixed `SDate.Now()` crashing when called during the new-game intro.
* Fixed `TimeEvents.AfterDayStarted` being raised during the new-game intro.
* Fixed SMAPI allowing map tilesheets with absolute or directory-climbing paths. These are now rejected even if the path exists, to avoid problems when players install the mod.

For power users:
* Added command-line arguments to the SMAPI installer so it can be scripted.

For SMAPI developers:
* Significantly refactored SMAPI to support changes in 2.0 and upcoming releases.
* Overhauled `StardewModdingAPI.config.json` format to support mod data like update keys.
* Removed SMAPI 1._x_ compatibility mode.

## 1.15.4
Released 09 September 2017 for Stardew Valley 1.2.30–1.2.33.

For players:
* Fixed errors when loading some custom maps on Linux/Mac or using XNB Loader.
* Fixed errors in rare cases when a mod calculates an in-game date.

For modders:
* Added UTC timestamp to log file.

For SMAPI developers:
* Internal changes to support the upcoming SMAPI 2.0 release.

## 1.15.3
Released 23 August 2017 for Stardew Valley 1.2.30–1.2.33.

For players:
* Fixed mods being wrongly marked as duplicate in some cases.

## 1.15.2
Released 23 August 2017 for Stardew Valley 1.2.30–1.2.33.

For players:
* Improved errors when a mod DLL can't be loaded.
* Improved errors when using very old versions of Stardew Valley.
* Updated compatibility list.

For mod developers:
* Added `Context.CanPlayerMove` property for mod convenience.
* Added content helper properties for the game's current language.
* Fixed `Context.IsPlayerFree` being false if the player is performing an action.
* Fixed `GraphicsEvents.Resize` being raised before the game updates its window data.
* Fixed `SemanticVersion` not being deserialisable through Json.NET.
* Fixed terminal not launching on Xfce Linux.

For SMAPI developers:
* Internal changes to support the upcoming SMAPI 2.0 release.

## 1.15.1
Released 10 July 2017 for Stardew Valley 1.2.30–1.2.33.

For players:
* Fixed controller mod input broken in 1.15.
* Fixed TrainerMod packaging unneeded files.

For modders:
* Fixed mod registry lookups by unique ID not being case-insensitive.

## 1.15
Released 08 July 2017 for Stardew Valley 1.2.30–1.2.31.

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
* Added `SDate` utility for in-game date calculations (see [API reference](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Utilities#Dates)).
* Added support for minimum dependency versions in `manifest.json` (see [API reference](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Manifest)).
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
Released 02 July 2017 for Stardew Valley 1.2.30.

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
* You can now add dependencies to `manifest.json` (see [API reference](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Manifest)).
* You can now translate your mod (see [API reference](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Translation)).
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
Released 19 May 2017 for Stardew Valley 1.2.26–1.2.29.

For players:
* Fixed errors when loading a mod with no name or version.
* Fixed mods with no manifest `Name` field having no name (SMAPI will now shows their filename).

## 1.13
Released 19 May 2017 for Stardew Valley 1.2.26–1.2.29.

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
Released 03 May 2017 for Stardew Valley 1.2.26–1.2.29.

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
Released 30 April 2017 for Stardew Valley 1.2.26.

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
Released 24 April 2017 for Stardew Valley 1.2.26.

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
Released 05 April 2017 for Stardew Valley 1.1–1.11.

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
* Several obsolete APIs have been removed (see [migration guides](https://stardewvalleywiki.com/Modding:Index#Migration_guides)),
  and all _notice_-level deprecations have been increased to _info_.
* Removed the experimental `IConfigFile`.

For SMAPI developers:
* Added support for debugging SMAPI on Linux/Mac if supported by the editor.

## 1.8
Released 04 February 2017 for Stardew Valley 1.1–1.11.

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
Released 19 January 2017 for Stardew Valley 1.1–1.11.

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
Released 16 January 2017 for Stardew Valley 1.1–1.11.

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
Released 27 December 2016 for Stardew Valley 1.1–1.11.

For players:
  * Added an option to disable update checks.
  * SMAPI will now show a friendly error with update links when you try to use a known incompatible mod version.
  * Fixed an error when a mod uses the new reflection API on a missing field or method.
  * Fixed an issue where mods weren't notified of a menu change if it changed while SMAPI was still notifying mods of the previous change.

For developers:
  * Deprecated `Version` in favour of `SemanticVersion`.  
    _This new implementation is [semver 2.0](https://semver.org/)-compliant, introduces `NewerThan(version)` and `OlderThan(version)` convenience methods, adds support for parsing a version string into a `SemanticVersion`, and fixes various bugs with the former implementation. This also replaces `Manifest` with `IManifest`._
  * Increased deprecation levels for `SObject`, `Extensions`, `LogWriter` (not `Log`), `SPlayer`, and `Mod.Entry(ModHelper)` (not `Mod.Entry(IModHelper)`).

## 1.4
Released 12 December 2016 for Stardew Valley 1.1–1.11.

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
Released 04 December 2016 for Stardew Valley 1.1–1.11.

For players:
  * You can now run most mods on any platform (e.g. run Windows mods on Linux/Mac).
  * Fixed the normal uninstaller not removing files added by the 'SMAPI for developers' installer.

## 1.2
Released 25 November 2016 for Stardew Valley 1.1–1.11.

For players:
  * Fixed compatibility with some older mods.
  * Fixed mod errors in most event handlers crashing the game.
  * Fixed mod errors in some event handlers preventing other mods from receiving the same event.
  * Fixed game crashing on startup with an audio error for some players.

For developers:
  * Improved logging to show `ReflectionTypeLoadException` details when it's caught by SMAPI.

## 1.1.1
Released 19 November 2016 for Stardew Valley 1.1–1.11.

For players:
  * Fixed compatibility with some older mods.
  * Fixed race condition where some mods would sometimes crash because the game wasn't ready yet.

For developers:
  * Fixed deprecation warnings being repeated if the mod can't be identified.

## 1.1
Released 17 November 2016 for Stardew Valley 1.1–1.11.

For players:
  * Fixed console exiting immediately when some exceptions occur.
  * Fixed an error in 1.0 when mod uses `config.json` but the file doesn't exist.
  * Fixed critical errors being saved to a separate log file.

For developers:
  * Added new logging interface:
    * easier to use;
    * supports trace logs (written to the log file, but hidden in the console by default);
    * messages are now listed in order;
    * messages now show which mod logged them;
    * more consistent and intuitive console color scheme.
  * Added optional `MinimumApiVersion` to `manifest.json`.
  * Added emergency interrupt feature for dangerous mods.

## 1.0
Released 11 November 2016 for Stardew Valley 1.1–1.11.

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
  * Switched to [semantic versioning](https://semver.org).
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
* 0.40.1.1 (30 September 2016)
  * Added support for Stardew Valley 1.1.
* 0.40.0 (05 April 2016)
  * Fixed an error that ocurred during minigames.
* 0.39.7 (04 April 2016)
  * Added 'no check' graphics events that are triggered regardless of game's if checks.
* 0.39.6 (01 April 2016)
  * Added game & SMAPI versions to log.
  * Fixed conflict in graphics tick events.
  * Bug fixes.
* 0.39.5 (30 March 2016)
* 0.39.4 (29 March 2016)
* 0.39.3 (28 March 2016)
* 0.39.2 (23 March 2016)
* 0.39.1 (23 March 2016)
* 0.38.8 (23 March 2016)
* 0.38.7 (23 March 2016)
* 0.38.6 (22 March 2016)
* 0.38.5 (22 March 2016)
* 0.38.4 (21 March 2016)
* 0.38.3 (21 March 2016)
* 0.38.2 (21 March 2016)
* 0.38.0 (20 March 2016)
* 0.38.1 (20 March 2016)
* 0.37.3 (08 March 2016)
* 0.37.2 (07 March 2016)
* 0.37.1 (06 March 2016)
* 0.36 (04 March 2016)
* 0.37 (04 March 2016)
* 0.35 (02 March 2016)
* 0.34 (02 March 2016)
* 0.33 (02 March 2016)
* 0.32 (02 March 2016)
* 0.31 (02 March 2016)
* 0.3 (01 March 2016)
* 0.2 (29 February 2016)
* 0.1 (28 February 2016)
