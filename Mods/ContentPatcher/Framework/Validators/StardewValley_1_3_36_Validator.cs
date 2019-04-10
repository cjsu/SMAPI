using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using ContentPatcher.Framework.Patches;
using StardewModdingAPI;
using xTile;
using xTile.Tiles;

namespace ContentPatcher.Framework.Validators
{
    /// <summary>Validate content packs for compatibility with Stardew Valley 1.3.36.</summary>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Named for clarity.")]
    internal class StardewValley_1_3_36_Validator : BaseValidator
    {
        /*********
        ** Fields
        *********/
        /// <summary>A map of tilesheets removed in Stardew Valley 1.3.36 and the new tilesheets that should be referenced instead.</summary>
        private IDictionary<string, string> ObsoleteTilesheets = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            ["mine"] = "Mines/mine",
            ["mine_dark"] = "Mines/mine_dark",
            ["mine_lava"] = "Mines/mine_lava"
        };


        /*********
        ** Public methods
        *********/
        /// <summary>Validate a content pack.</summary>
        /// <param name="asset">The asset being loaded.</param>
        /// <param name="data">The loaded asset data to validate.</param>
        /// <param name="patch">The patch which loaded the asset.</param>
        /// <param name="error">An error message which indicates why validation failed.</param>
        /// <returns>Returns whether validation succeeded.</returns>
        public override bool TryValidate<T>(IAssetInfo asset, T data, IPatch patch, out string error)
        {
            // detect vanilla tilesheets removed in SDV 1.3.36
            if (data is Map map)
            {
                string mapFolderPath = Path.GetDirectoryName(patch.FromLocalAsset.Value);
                foreach (TileSheet tilesheet in map.TileSheets)
                {
                    string curKey = tilesheet.ImageSource;

                    // skip if tilesheet exists relative to the content pack
                    string mapRelativeSource = Path.Combine(mapFolderPath, curKey);
                    if (patch.ContentPack.HasFile(mapRelativeSource))
                        continue;

                    // detect obsolete tilesheet references
                    if (this.IsObsoleteTilesheet(curKey, out string newKey))
                    {
                        error = $"references vanilla tilesheet '{curKey}' removed in Stardew Valley 1.3.36, should use '{newKey}' instead";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get whether a given tilesheet image source is obsolete.</summary>
        /// <param name="curKey">The tilesheet image source.</param>
        /// <param name="newKey">The key that <paramref name="curKey"/> should be replaced with, if it's obsolete.</param>
        private bool IsObsoleteTilesheet(string curKey, out string newKey)
        {
            if (curKey == null)
            {
                newKey = null;
                return false;
            }

            // exact match
            if (this.ObsoleteTilesheets.TryGetValue(curKey, out newKey))
                return true;

            // strip .png
            if (curKey.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase) && this.ObsoleteTilesheets.TryGetValue(curKey.Substring(0, curKey.Length - 4), out newKey))
                return true;

            return false;
        }
    }
}
