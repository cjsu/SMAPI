← [README](README.md)

# Release notes
<!--
## Future release
* For modders:
  * Migrated to Harmony 2.0 (see [_migrate to Harmony 2.0_](https://stardewvalleywiki.com/Modding:Migrate_to_Harmony_2.0) for more info).
-->

## 3.9.1
Released 25 January 2021 for Stardew Valley 1.5.4 or later.

* For players:
  * Fixed _tile contains an invalid TileSheet reference_ crash after mods change certain maps.
  * Fixed _patched game code_ issue shown for the bundled Error Handler mod.

## 3.9
Released 22 January 2021 for Stardew Valley 1.5.4 or later. See [release highlights](https://www.patreon.com/posts/46553874).

* For players:
  * Updated for Stardew Valley 1.5.4.
  * Improved game detection in the installer:
    * The installer now prefers paths registered by Steam or GOG Galaxy.
    * The installer now detects default manual GOG installs.
  * Added clearer error text for empty mod folders created by Vortex.
  * Fixed the game's map changes not always reapplied correctly after mods change certain maps, which caused issues like the community center resetting to its non-repaired texture.
  * Fixed compatibility for very old content packs which still load maps from `.xnb` files. These were broken by map loading changes in Stardew Valley 1.5, but SMAPI now corrects them automatically.
  * Fixed some broken mods incorrectly listed as XNB mods under 'skipped mods'.

* For modders:
  * Added new input APIs:
    * Added an [API for multi-key bindings](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Input#KeybindList).
    * Added a new [`Input.ButtonsChanged` event](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events#Input.ButtonsChanged).
    * Added a `buttonState.IsDown()` extension.
    * Added a `helper.Input.SuppressActiveKeybinds` method to suppress the active buttons in a keybind list.
  * Improved multiplayer APIs:
    * `PerScreen<T>` now lets you get/set the value for any screen, get all active values, or clear all values.
    * Peer data from the multiplayer API/events now includes `IsSplitScreen` and `ScreenID` fields.
    * Fixed network messages through the multiplayer API being sent to players who don't have SMAPI installed in some cases.
  * Improved asset propagation:
    * Updated map propagation for the changes in Stardew Valley 1.5.4.
    * Added propagation for some `Strings\StringsFromCSFiles` keys (mainly short day names).
    * Fixed quarry bridge not fixed if the mountain map was reloaded.
  * Added an option to disable rewriting mods for compatibility (thanks to Bpendragon!). This prevents older mods from loading, but bypasses a Visual Studio crash when debugging.
  * Game errors shown in the chatbox are now logged.
  * Moved vanilla error-handling into a new Error Handler mod. This simplifies the core SMAPI logic, and lets users disable it if needed.

* For the Console Commands mod:
  * Removed the `inf` option for `player_sethealth`, `player_setmoney`, and `player_setstamina`. You can use mods like [CJB Cheats Menu](https://www.nexusmods.com/stardewvalley/mods/4) instead for that.

* For the Error Handler mod:
  * Added a detailed message for the _Input string was not in a correct format_ error when the game fails to parse an item text description.

* For the web UI:
  * Fixed JSON validator incorrectly marking some manifest update keys as invalid.

## 3.8.4
Released 15 January 2021 for Stardew Valley 1.5.3 or later.

* For players:
  * Updated for Stardew Valley 1.5.3.
  * Fixed issue where title screen music didn't stop after loading a save.

* For modders:
  * Fixed `SemanticVersion` comparisons returning wrong value in rare cases.

## 3.8.3
Released 08 January 2021 for Stardew Valley 1.5.2 or later.

* For players:
  * Updated for Stardew Valley 1.5.2.
  * Reduced memory usage.
  * You can now enter console commands for a specific screen in split-screen mode by adding `screen=ID` to the command.
  * Typing `help` in the SMAPI console is now more helpful.

* For modders:
  * Simplified tilesheet order warning added in SMAPI 3.8.2.

* For the Console Commands mod:
  * Removed experimental `performance` command. Unfortunately this impacted SMAPI's memory usage and performance, and the data was often misinterpreted. This may be replaced with more automatic performance alerts in a future version.

## 3.8.2
Released 03 January 2021 for Stardew Valley 1.5.1 or later.

* For players:
  * SMAPI now blocks farm map replacements that would crash the game in Stardew Valley 1.5.
  * On Linux, the SMAPI installer now auto-detects Flatpak Steam paths.
  * Updated compatibility list.
  * Fixed errors when multiple players join in split-screen mode.
  * Fixed 'skipped mods' section repeating mods in some cases.
  * Fixed out-of-date error text.

* For modders:
  * Added warning when a map replacement changes the order/IDs of the original tilesheets, which may cause errors and crashes. Doing so for a farm map is blocked outright since that causes a consistent crash in Stardew Valley 1.5.
  * Message data from the `ModMessageReceived` event now uses the same serializer settings as the rest of SMAPI. That mainly adds support for sending crossplatform `Color`, `Point`, `Vector2`, `Rectangle`, and `SemanticVersion` fields through network messages.
  * When a mod is blocked by SMAPI's compatibility override list, the `TRACE` messages while loading it now say so and indicate why.
  * Fixed how the input API handles UI scaling. This mainly affects `ICursorPosition` values returned by the API; see [the wiki docs](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Input#ICursorPosition) for how to account for UI scaling.

## 3.8.1
Released 26 December 2020 for Stardew Valley 1.5.1 or later.

* For players:
  * Fixed broken community center bundles for non-English saves created in Stardew Valley 1.5. Affected saves will be fixed automatically on load.

* For modders:
  * World events are now raised for volcano dungeon levels.
  * Added `apply_save_fix` command to reapply a save migration in exceptional cases. This should be used very carefully. Type `help apply_save_fix` for details.
  * **Deprecation notice:** the `Helper.ConsoleCommands.Trigger` method is now deprecated and should no longer be used. See [integration APIs](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Integrations) for better mod integration options. It will eventually be removed in SMAPI 4.0.

For the web UI:
  * Fixed edge cases in SMAPI log parsing.

## 3.8
Released 21 December 2020 for Stardew Valley 1.5 or later. See [release highlights](https://www.patreon.com/posts/45294737).

* For players:
  * Updated for Stardew Valley 1.5, including split-screen support.
  * You can now run the installer from a subfolder of your game folder to auto-detect it. That simplifies installation if you have multiple copies of the game or it can't otherwise auto-detect the game path.
  * Clarified error when the SMAPI installer is in the `Mods` folder.

* For modders:
  * Added `PerScreen<T>` utility and new `Context` fields to simplify split-screen support in mods.
  * Added screen ID to log when playing in split-screen mode.

* For the Console Commands mod:
  * Added `furniture` option to `world_clear`.

* For the web UI:
  * Updated the JSON validator/schema for Content Patcher 1.19.

## 3.7.6
Released 21 November 2020 for Stardew Valley 1.4.1 or later.

* For players:
  * Fixed error when heuristically rewriting an outdated mod in rare cases.
  * Fixed rare 'collection was modified' error when using `harmony summary` console command.

* For modders:
  * Updated TMXTile to 1.5.8 to fix exported `.tmx` files losing tile index properties.

* For the Console Commands mod:
  * `player_add` can now spawn shirts normally only available during character customization.
  * `player_add` now applies fish pond rules for roe items. (That mainly adds Clam Roe, Sea Urchin Roe, and custom roe from mods.)

## 3.7.5
Released 16 October 2020 for Stardew Valley 1.4.1 or later.

* For modders:
  * Fixed changes to the town map asset not reapplying the game's community center, JojaMart, and Pam house changes.

## 3.7.4
Released 03 October 2020 for Stardew Valley 1.4.1 or later.

* For players:
  * Improved performance on some older computers (thanks to millerscout!).
  * Fixed update alerts for Chucklefish forum mods broken by a recent site change.

* For modders:
  * Updated dependencies (including Mono.Cecil 0.11.2 → 0.11.3 and Platonymous.TMXTile 1.3.8 → 1.5.6).
  * Fixed asset propagation for `Data\MoviesReactions`.
  * Fixed error in content pack path handling when you pass a null path.

* For the web UI:
  * Updated the JSON validator/schema for Content Patcher 1.18.

* For SMAPI developers:
  * Simplified preparing a mod build config package release.

## 3.7.3
Released 16 September 2020 for Stardew Valley 1.4.1 or later.

* For players:
  * Fixed errors on Linux/Mac due to content packs with incorrect filename case.
  * Fixed map rendering crash due to conflict between SMAPI and PyTK.
  * Fixed error in heuristically-rewritten mods in rare cases (thanks to collaboration with ZaneYork!).

* For modders:
  * File paths accessed through `IContentPack` are now case-insensitive (even on Linux).

* For the web UI:
  * You can now renew the expiry for an uploaded JSON/log file if you need it longer.

## 3.7.2
Released 08 September 2020 for Stardew Valley 1.4.1 or later.

* For players:
  * Fixed mod recipe changes not always applied in 3.7.

* For modders:
  * Renamed `PathUtilities.NormalizePathSeparators` to `NormalizePath`, and added normalization for more cases.

## 3.7.1
Released 08 September 2020 for Stardew Valley 1.4.1 or later.

* For players:
  * Fixed input-handling bugs in 3.7.

## 3.7
Released 07 September 2020 for Stardew Valley 1.4.1 or later. See [release highlights](https://www.patreon.com/posts/41341767).

* For players:
  * Added heuristic compatibility rewrites. (This improves mod compatibility with Android and future game updates.)
  * Tweaked the rules for showing update alerts (see _for SMAPI developers_ below for details).
  * Simplified the error shown for duplicate mods.
  * Fixed crossplatform compatibility for mods which use the `[HarmonyPatch(type)]` attribute (thanks to spacechase0!).
  * Fixed map tile rotation broken when you return to the title screen and reload a save.
  * Fixed broken URL in update alerts for unofficial versions.
  * Fixed rare error when a mod adds/removes event handlers asynchronously.
  * Fixed rare issue where the console showed incorrect colors when mods wrote to it asynchronously.
  * Fixed SMAPI not always detecting broken field references in mod code.
  * Removed the experimental `RewriteInParallel` option added in SMAPI 3.6 (it was already disabled by default). Unfortunately this caused intermittent and unpredictable errors when enabled.
  * Internal changes to prepare for upcoming game updates.

* For modders:
  * Added `PathUtilities` to simplify working with file/asset names.
  * You can now read/write `SDate` values to JSON (e.g. for `config.json`, network mod messages, etc).
  * Fixed asset propagation not updating title menu buttons immediately on Linux/Mac.

* For the web UI:
  * Updated the JSON validator/schema for Content Patcher 1.16 and 1.17.

* For SMAPI developers:
  * The web API now returns an update alert in two new cases: any newer unofficial update (previously only shown if the mod was incompatible), and a newer prerelease version if the installed non-prerelease version is broken (previously only shown if the installed version was prerelease).
  * Reorganised the SMAPI core to reduce coupling to game types like `Game1`, make it easier to navigate, and simplify future game updates.
  * SMAPI now automatically fixes code broken by these changes in game code, so manual rewriters are no longer needed:
    * reference to a method with new optional parameters;
    * reference to a field replaced by a property;
    * reference to a field replaced by a `const` field.
  * `FieldReplaceRewriter` now supports mapping to a different target type.

## 3.6.2
Released 02 August 2020 for Stardew Valley 1.4.1 or later.

* For players:
  * Improved compatibility with some Linux terminals (thanks to jlaw and Spatterjaaay!).
  * Fixed rare error when a mod adds/removes an event handler from an event handler.
  * Fixed string sorting/comparison for some special characters.

* For the Console Commands mod:
  * Fixed error opening menu when some item data is invalid.
  * Fixed spawned Floor TV not functional as a TV (thanks to Platonymous!).
  * Fixed spawned sturgeon roe having incorrect color.

* For modders:
  * Updated internal dependencies.
  * SMAPI now ignores more file types when scanning for mod folders (`.doc`, `.docx`, `.rar`, and `.zip`).
  * Added current GPU to trace logs to simplify troubleshooting.

## 3.6.1
Released 21 June 2020 for Stardew Valley 1.4.1 or later.

* Fixed event priority sorting.

## 3.6
Released 20 June 2020 for Stardew Valley 1.4.1 or later. See [release highlights](https://www.patreon.com/posts/38441800).

* For players:
  * Added crossplatform compatibility for mods which use the `[HarmonyPatch(type)]` attribute.
  * Added experimental option to reduce startup time when loading mod DLLs (thanks to ZaneYork!). Enable `RewriteInParallel` in the `smapi-internal/config.json` to try it.
  * Reduced processing time when a mod loads many unpacked images (thanks to Entoarox!).
  * Mod load warnings are now listed alphabetically.
  * MacOS files starting with `._` are now ignored and can no longer cause skipped mods.
  * Simplified paranoid warning logs and reduced their log level.
  * Fixed black maps on Android for mods which use `.tmx` files.
  * Fixed `BadImageFormatException` error detection.
  * Fixed `reload_i18n` command not reloading content pack translations.

* For the web UI:
  * Added GitHub licenses to mod compatibility list.
  * Improved JSON validator:
    * added SMAPI `i18n` schema;
    * editing an uploaded file now remembers the selected schema;
    * changed default schema to plain JSON.
  * Updated ModDrop URLs.
  * Internal changes to improve performance and reliability.

* For modders:
  * Added [event priorities](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events#Custom_priority) (thanks to spacechase0!).
  * Added [update subkeys](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Update_checks#Update_subkeys).
  * Added [a custom build of Harmony](https://github.com/Pathoschild/Harmony#readme) to provide more useful stack traces in error logs.
  * Added `harmony_summary` console command to list or search current Harmony patches.
  * Added `Multiplayer.PeerConnected` event.
  * Added support for overriding update keys from the wiki compatibility list.
  * Improved mod rewriting for compatibility to support more cases (e.g. custom attributes and generic types).
  * Fixed `helper.Reflection` blocking access to game methods/properties intercepted by SMAPI.
  * Fixed asset propagation for Gil's portraits.
  * Fixed `.pdb` files ignored for error stack traces when mods are rewritten by SMAPI.
  * Fixed `ModMessageReceived` event handlers not tracked for performance monitoring.

* For SMAPI developers:
  * Eliminated MongoDB storage in the web services, which complicated the code unnecessarily. The app still uses an abstract interface for storage, so we can wrap a distributed cache in the future if needed.
  * Overhauled update checks to simplify mod site integrations, centralize common logic, and enable upcoming features.
  * Merged the separate legacy redirects app on AWS into the main app on Azure.
  * Changed SMAPI's Harmony ID from `io.smapi` to `SMAPI` for readability in Harmony summaries.

## 3.5
Released 27 April 2020 for Stardew Valley 1.4.1 or later. See [release highlights](https://www.patreon.com/posts/36471055).

* For players:
  * SMAPI now prevents more game errors due to broken items, so you no longer need save editing to remove them.
  * Added option to disable console colors.
  * Updated compatibility list.
  * Improved translations.¹

* For the Console Commands mod:
  * Commands like `world_setday` now also affect the 'days played' stat, so in-game events/randomization match what you'd get if you played to that date normally (thanks to kdau!).

* For the web UI:
  * Updated the JSON validator/schema for Content Patcher 1.13.
  * Fixed rare intermittent "CGI application encountered an error" errors.

* For modders:
  * Added map patching to the content API (via `asset.AsMap()`).
  * Added support for using patch helpers with arbitrary data (via `helper.Content.GetPatchHelper`).
  * Added `SDate` fields/methods: `SeasonIndex`, `FromDaysSinceStart`, `FromWorldDate`, `ToWorldDate`, and `ToLocaleString` (thanks to kdau!).
  * Added `SDate` translations taken from the Lookup Anything mod.¹
  * Fixed asset propagation for certain maps loaded through temporary content managers. This notably fixes unreliable patches to the farmhouse and town maps.
  * Fixed asset propagation on Linux/Mac for monster sprites, NPC dialogue, and NPC schedules.
  * Fixed asset propagation for NPC dialogue sometimes causing a spouse to skip marriage dialogue or not allow kisses.

¹ Date format translations were taken from the Lookup Anything mod; thanks to translators FixThisPlz (improved Russian), LeecanIt (added Italian), pomepome (added Japanese), S2SKY (added Korean), Sasara (added German), SteaNN (added Russian), ThomasGabrielDelavault (added Spanish), VincentRoth (added French), Yllelder (improved Spanish), and yuwenlan (added Chinese). Some translations for Korean, Hungarian, and Turkish were derived from the game translations.

## 3.4.1
Released 24 March 2020 for Stardew Valley 1.4.1 or later.

* For modders:
  * Asset changes now propagate to NPCs in an event (e.g. wedding sprites).
  * Fixed mouse input suppression not working in SMAPI 3.4.

## 3.4
Released 22 March 2020 for Stardew Valley 1.4.1 or later. See [release highlights](https://www.patreon.com/posts/35161371).

* For players:
  * Fixed semi-transparency issues on Linux/Mac in recent versions of Mono (e.g. pink shadows).
  * Fixed `player_add` command error if you have broken XNB mods.
  * Removed invalid-location check now handled by the game.
  * Updated translations. Thanks to Annosz (added Hungarian)!

* For modders:
  * Added support for flipped and rotated map tiles (thanks to collaboration with Platonymous!).
  * Added support for `.tmx` maps using zlib compression (thanks to Platonymous!).
  * Added `this.Monitor.LogOnce` method.
  * Mods are no longer prevented from suppressing key presses in the chatbox.

* For the web UI:
  * Added option to upload files using a file picker.
  * Optimized log parser for very long multi-line log messages.
  * Fixed log parser not detecting folder path in recent versions of SMAPI.

* For SMAPI developers:
  * Added internal API to send custom input to the game/mods. This is mainly meant to support Virtual Keyboard on Android, but might be exposed as a public API in future versions.

## 3.3.2
Released 22 February 2020 for Stardew Valley 1.4.1 or later.

* Fixed mods receiving their own message broadcasts.

## 3.3.1
Released 22 February 2020 for Stardew Valley 1.4.1 or later.

* Fixed errors with custom spouse room mods in SMAPI 3.3.

## 3.3
Released 22 February 2020 for Stardew Valley 1.4.1 or later. See [release highlights](https://www.patreon.com/posts/34248719).

* For players:
  * Improved performance for mods which load many images.
  * Reduced network traffic for mod broadcasts to players who can't process them.
  * Fixed update-check errors for recent versions of SMAPI on Android.
  * Updated draw logic to match recent game updates.
  * Updated compatibility list.
  * Updated SMAPI/game version map.
  * Updated translations. Thanks to xCarloC (added Italian)!

* For the Save Backup mod:
  * Fixed warning on MacOS when you have no saves yet.
  * Reduced log messages.

* For the web UI:
  * Updated the JSON validator and Content Patcher schema for `.tmx` support.
  * The mod compatibility page now has a sticky table header.

* For modders:
  * Added support for [message sending](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Integrations#Message_sending) to mods on the current computer (in addition to remote computers).
  * Added `ExtendImage` method to content API when editing files to resize textures.
  * Added `helper.Input.GetState` to get the low-level state of a button.
  * **[Breaking change]** Map tilesheets are no loaded from `Content` if they can't be found in `Content/Maps`. This reflects an upcoming change in the game to delete duplicate map tilesheets under `Content`. Most mods should be unaffected.
  * Improved map tilesheet errors so they provide more info.
  * When mods load an asset using a more general type like `content.Load<object>`, SMAPI now calls `IAssetEditor` instances with the actual asset type instead of the specified one.
  * Updated dependencies (including Mono.Cecil 0.11.1 → 0.11.2).
  * Fixed dialogue propagation clearing marriage dialogue.

* For SMAPI/tool developers:
  * Improved support for four-part versions to support SMAPI on Android.
  * The SMAPI log now prefixes the OS name with `Android` on Android.

## 3.2
Released 01 February 2020 for Stardew Valley 1.4.1 or later. See [release highlights](https://www.patreon.com/posts/33659728).

* For players:
  * SMAPI now prevents crashes due to invalid schedule data.
  * SMAPI now prevents crashes due to invalid building types.
  * Added support for persistent `smapi-internal/config.json` overrides (see info in the file).
  * Updated minimum game version (1.4 → 1.4.1).
  * Fixed 'collection was modified' error when returning to title in rare cases.
  * Fixed error when update-checking a mod with a Chucklefish page that has no version.
  * Fixed rare error when building/demolishing buildings.
  * Fixed SMAPI beta versions not showing update alert on next launch (thanks to danvolchek!).

* For the Console Commands mod:
  * Added `performance` command to track mod performance metrics. This is an advanced experimental feature. (Thanks to Drachenkätzchen!)
  * Added `test_input` command to view button codes in the console.

* For the Save Backup mod:
  * Fixed extra files under `Saves` (e.g. manual backups) not being ignored.
  * Fixed Android issue where game files were backed up.

* For modders:
  * Added support for `.tmx` map files. (Thanks to [Platonymous for the underlying library](https://github.com/Platonymous/TMXTile)!)
  * Added special handling for `Vector2` values in `.json` files, so they work consistently crossplatform.
  * Reworked the order that asset editors/loaders are called between multiple mods to support some framework mods like Content Patcher and Json Assets. Note that the order is undefined and should not be depended on.
  * Fixed incorrect warning about mods adding invalid schedules in some cases. The validation was unreliable, and has been removed.
  * Fixed asset propagation not updating other players' sprites.
  * Fixed asset propagation for player sprites not updating recolor maps (e.g. sleeves).
  * Fixed asset propagation for marriage dialogue.
  * Fixed dialogue asset changes not correctly propagated until the next day.
  * Fixed `helper.Data.Read`/`WriteGlobalData` using the `Saves` folder instead of the game's appdata folder. The installer will move existing folders automatically.
  * Fixed issue where a mod which implemented `IAssetEditor`/`IAssetLoader` on its entry class could then remove itself from the editor/loader list.

* For SMAPI/tool developers:
  * Added internal performance monitoring (thanks to Drachenkätzchen!). This is disabled by default in the current version, but can be enabled using the `performance` console command.
  * Added internal support for four-part versions to support SMAPI on Android.
  * Rewrote `SemanticVersion` parsing.
  * Updated links for the new r/SMAPI subreddit.
  * The `/mods` web API endpoint now includes version mappings from the wiki.
  * Dropped API support for the pre-3.0 update-check format.

## 3.1
Released 05 January 2019 for Stardew Valley 1.4.1 or later. See [release highlights](https://www.patreon.com/posts/32904041).

* For players:
  * Added separate group in 'skipped mods' list for broken dependencies, so it's easier to see what to fix first.
  * Added friendly log message for save file-not-found errors.
  * Updated for gamepad modes in Stardew Valley 1.4.1.
  * Improved performance in some cases.
  * Fixed compatibility with Linux Mint 18 (thanks to techge!), Arch Linux, and Linux systems with libhybris-utils installed.
  * Fixed memory leak when repeatedly loading a save and returning to title.
  * Fixed memory leak when mods reload assets.
  * Updated translations. Thanks to L30Bola (added Portuguese), PlussRolf (added Spanish), and shirutan (added Japanese)!

* For the Console Commands mod:
  * Added new clothing items.
  * Fixed spawning new flooring and rings (thanks to Mizzion!).
  * Fixed spawning custom rings added by mods.
  * Fixed errors when some item data is invalid.

* For the web UI:
  * Added option to edit & reupload in the JSON validator.
  * File uploads are now stored in Azure storage instead of Pastebin, due to ongoing Pastebin perfomance issues.
  * File uploads now expire after one month.
  * Updated the JSON validator for Content Patcher 1.10 and 1.11.
  * Fixed JSON validator no longer letting you change format when viewing a file.
  * Fixed JSON validator for Content Patcher not requiring `Default` if `AllowBlank` was omitted.
  * Fixed log parser not correctly handling content packs with no author (thanks to danvolchek!).
  * Fixed main sidebar link pointing to wiki instead of home page.

* For modders:
  * Added `World.ChestInventoryChanged` event (thanks to collaboration with wartech0!).
  * Added asset propagation for...
    * grass textures;
    * winter flooring textures;
    * `Data\Bundles` changes (for added bundles only);
    * `Characters\Farmer\farmer_girl_base_bald`.
  * Added paranoid-mode warning for direct `Console` access.
  * Improved error messages for `TargetParameterCountException` when using the reflection API.
  * `helper.Read/WriteSaveData` can now be used while a save is being loaded (e.g. within a `Specialized.LoadStageChanged` event).
  * Removed `DumpMetadata` option. It was only for specific debugging cases, but players would sometimes enable it incorrectly and then report crashes.
  * Fixed private textures loaded from content packs not having their `Name` field set.

* For SMAPI developers:
  * You can now run local environments without configuring Amazon, Azure, MongoDB, and Pastebin accounts.

## 3.0.1
Released 02 December 2019 for Stardew Valley 1.4 or later.

* For players:
  * Updated for Stardew Valley 1.4.0.1.
  * Improved compatibility with some Linux terminals (thanks to archification and DanielHeath!).
  * Updated translations. Thanks to berkayylmao (added Turkish), feathershine (added Chinese), and Osiris901 (added Russian)!

* For the web UI:
  * Rebuilt web infrastructure to handle higher traffic.
  * If a log can't be uploaded to Pastebin (e.g. due to rate limits), it's now uploaded to Amazon S3 instead. Logs uploaded to S3 expire after one month.
  * Fixed JSON validator not letting you drag & drop a file.

* For modders:
  * `SemanticVersion` now supports [semver 2.0](https://semver.org/) build metadata.

## 3.0
Released 26 November 2019 for Stardew Valley 1.4.

### Release highlights
For players:
* **Updated for Stardew Valley 1.4.**  
  SMAPI 3.0 adds compatibility with the latest game version, and improves mod APIs for changes in
  the game code.

* **Improved performance.**  
  SMAPI should have less impact on game performance and startup time for some players.

* **Automatic save fixing and more error recovery.**  
  SMAPI now detects and prevents more crashes due to game/mod bugs, and automatically fixes your
  save if you remove some custom-content mods.

* **Improved mod scanning.**  
  SMAPI now supports some non-standard mod structures automatically, improves compatibility with
  the Vortex mod manager, and improves various error/skip messages related to mod loading.

* **Overhauled update checks.**  
  SMAPI update checks are now handled entirely on the web server and support community-defined
  version mappings. In particular, false update alerts due to author mistakes can now be solved by
  the community for all players.

* **Fixed many bugs and edge cases.**

For modders:
* **New event system.**  
  SMAPI 3.0 removes the deprecated static events in favor of the new `helper.Events` API. The event
  engine is rewritten to make events more efficient, add events that weren't possible before, make
  existing events more useful, and make event usage and behavior more consistent. When a mod makes
  changes in an event handler, those changes are now also reflected in the next event raise.

* **Improved mod build package.**  
  The [mod build package](https://www.nuget.org/packages/Pathoschild.Stardew.ModBuildConfig) now
  includes the `assets` folder by default if present, supports the new `.csproj` project format,
  enables mod `.pdb` files automatically (to provide line numbers in error messages), adds optional
  Harmony support, and fixes some bugs and edge cases. This also adds compatibility with SMAPI 3.0
  and Stardew Valley 1.4, and drops support for older versions.

* **Mods loaded earlier.**  
  SMAPI now loads mods much earlier, before the game is initialised. That lets mods do things that
  were difficult before, like intercepting some core assets.

* **Improved Android support.**  
  SMAPI now automatically detects when it's running on Android, and updates `Constants.TargetPlatform`
  so mods can adjust their logic if needed. The Save Backup mod is also now Android-compatible.

* **Improved asset propagation.**  
  SMAPI now automatically propagates asset changes for farm animal data, NPC default location data,
  critter textures, and `DayTimeMoneyBox` buttons. Every loaded texture now also has a `Name` field
  so mods can check which asset a texture was loaded for.

* **Breaking changes:**  
  See _[migrate to SMAPI 3.0](https://stardewvalleywiki.com/Modding:Migrate_to_SMAPI_3.0)_ and
  _[migrate to Stardew Valley 1.4](https://stardewvalleywiki.com/Modding:Migrate_to_Stardew_Valley_1.4)_
  for more info.

### For players
* Changes:
  * Updated for Stardew Valley 1.4.
  * Improved performance.
  * Reworked update checks and added community-defined version mapping, to reduce false update alerts due to author mistakes.
  * SMAPI now removes invalid locations/NPCs when loading a save to prevent crashes. A warning is shown in-game when this happens.
  * Added update checks for CurseForge mods.
  * Added support for editing console colors via `smapi-internal/config.json` (for players with unusual consoles).
  * Added support for setting SMAPI CLI arguments as environment variables for Linux/macOS compatibility.
  * Improved mod scanning:
    * Now ignores metadata files/folders (like `__MACOSX` and `__folder_managed_by_vortex`) and content files (like `.txt` or `.png`), which avoids missing-manifest errors in some cases.
    * Now detects XNB mods more accurately, and consolidates multi-folder XNB mods in logged messages.
  * Improved launch script compatibility on Linux (thanks to kurumushi and toastal!).
  * Made error messages more user-friendly in some cases.
  * Save Backup now works in the background, to avoid affecting startup time for players with a large number of saves.
  * The installer now recognises custom game paths stored in [`stardewvalley.targets`](http://smapi.io/package/custom-game-path).
  * Duplicate-mod errors now show the mod version in each folder.
  * Update checks are now faster in some cases.
  * Updated mod compatibility list.
  * Updated SMAPI/game version map.
  * Updated translations. Thanks to eren-kemer (added German)!
* Fixes:
  * Fixed some assets not updated when you switch language to English.
  * Fixed lag in some cases due to incorrect asset caching when playing in non-English.
  * Fixed lag when a mod invalidates many NPC portraits/sprites at once.
  * Fixed Console Commands not including upgraded tools in item commands.
  * Fixed Console Commands' item commands failing if a mod adds invalid item data.
  * Fixed Save Backup not pruning old backups if they're uncompressed.
  * Fixed issues when a farmhand reconnects before the game notices they're disconnected.
  * Fixed 'received message' logs shown in non-developer mode.
  * Fixed various error messages and inconsistent spelling.
  * Fixed update-check error if a Nexus mod is marked as adult content.
  * Fixed update-check error if the Chucklefish page for an update key doesn't exist.

### For the web UI
* Mod compatibility list:
  * Added support for CurseForge mods.
  * Added metadata links and dev notes (if any) to advanced info.
  * Now loads faster (since data is fetched in a background service).
  * Now continues working with cached data when the wiki is offline.
  * Clicking a mod link now automatically adds it to the visible mods if the list is filtered.

* JSON validator:
  * Added JSON validator at [smapi.io/json](https://smapi.io/json), which lets you validate a JSON file against predefined mod formats.
  * Added support for the `manifest.json` format.
  * Added support for the Content Patcher format (thanks to TehPers!).
  * Added support for referencing a schema in a JSON Schema-compatible text editor.

* For the log parser:
  * Added instructions for Android.
  * The page now detects your OS and preselects the right instructions (thanks to danvolchek!).

### For modders
* Breaking changes:
  * Mods are now loaded much earlier in the game launch. This lets mods intercept any content asset, but the game is not fully initialized when `Entry` is called; use the `GameLaunched` event if you need to run code when the game is initialized.
  * Removed all deprecated APIs.
  * Removed unused APIs: `Monitor.ExitGameImmediately`, `Translation.ModName`, and `Translation.Assert`.
  * Fixed `ICursorPosition.AbsolutePixels` not adjusted for zoom.
  * `SemanticVersion` no longer omits `.0` patch numbers when formatting versions, for better [semver](https://semver.org/) conformity (e.g. `3.0` is now formatted as `3.0.0`).
* Changes:
  * Added support for content pack translations.
  * Added `IContentPack.HasFile`, `Context.IsGameLaunched`, and `SemanticVersion.TryParse`.
  * Added separate `LogNetworkTraffic` option to make verbose logging less overwhelmingly verbose.
  * Added asset propagation for `Data\FarmAnimals`, critter textures, and `DayTimeMoneyBox` buttons.
  * Added `Texture2D.Name` values set to the asset key.
  * Added trace logs for skipped loose files in the `Mods` folder and custom SMAPI settings so it's easier to troubleshoot player logs.
  * `Constants.TargetPlatform` now returns `Android` when playing on an Android device.
  * Trace logs for a broken mod now list all detected issues (instead of the first one).
  * Trace logs when loading mods are now more clear.
  * Clarified update-check errors for mods with multiple update keys.
  * Updated dependencies (including Json.NET 11.0.2 → 12.0.3 and Mono.Cecil 0.10.1 → 0.11.1).
* Fixes:
  * Fixed map reloads resetting tilesheet seasons.
  * Fixed map reloads not updating door warps.
  * Fixed outdoor tilesheets being seasonalised when added to an indoor location.
  * Fixed mods needing to load custom `Map` assets before the game accesses them. SMAPI now does so automatically.
  * Fixed custom maps loaded from `.xnb` files not having their tilesheet paths automatically adjusted.
  * Fixed custom maps loaded from the mod folder with tilesheets in a subfolder not working crossplatform. All tilesheet paths are now normalized for the OS automatically.
  * Fixed issue where mod changes weren't tracked correctly for raising events in some cases. Events now reflect a frozen snapshot of the game state, and any mod changes are reflected in the next event tick.
  * Fixed issue where, when a mod's `IAssetEditor` uses `asset.ReplaceWith` on a texture asset while playing in non-English, any changes from that point forth wouldn't affect subsequent cached asset loads.
  * Fixed asset propagation for NPC portraits resetting any unique portraits (e.g. Maru's hospital portrait) to the default.
  * Fixed changes to `Data\NPCDispositions` not always propagated correctly to existing NPCs.
  * Fixed `Rendering`/`Rendered` events not raised during minigames.
  * Fixed `LoadStageChanged` event not raising correct flags in some cases when creating a new save.
  * Fixed `GetApi` without an interface not checking if all mods are loaded.

### For SMAPI maintainers
* Added support for core translation files.
* Migrated to new `.csproj` format.
* Internal refactoring.

## 2.11.3 and earlier
See [older release notes](release-notes-archived.md).
