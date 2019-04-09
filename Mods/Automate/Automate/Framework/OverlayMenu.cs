using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathoschild.Stardew.Common;
using Pathoschild.Stardew.Common.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Pathoschild.Stardew.Automate.Framework
{
    /// <summary>The overlay which highlights automatable machines.</summary>
    internal class OverlayMenu : BaseOverlay
    {
        /*********
        ** Fields
        *********/
        /// <summary>The padding to apply to tile backgrounds to make the grid visible.</summary>
        private readonly int TileGap = 1;

        /// <summary>A machine group lookup by tile coordinate.</summary>
        private readonly IDictionary<Vector2, MachineGroup> GroupTiles;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="events">The SMAPI events available for mods.</param>
        /// <param name="inputHelper">An API for checking and changing input state.</param>
        /// <param name="machineGroups">The machine groups to display.</param>
        public OverlayMenu(IModEvents events, IInputHelper inputHelper, IEnumerable<MachineGroup> machineGroups)
            : base(events, inputHelper)
        {
            // init machine groups
            machineGroups = machineGroups.ToArray();
            this.GroupTiles =
                (
                    from machineGroup in machineGroups
                    from tile in machineGroup.Tiles
                    select new { tile, machineGroup }
                )
                .ToDictionary(p => p.tile, p => p.machineGroup);
        }


        /*********
        ** Protected
        *********/
        /// <summary>Draw the overlay to the screen.</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        [SuppressMessage("ReSharper", "PossibleLossOfFraction", Justification = "Deliberate discarded for conversion to tile coordinates.")]
        protected override void Draw(SpriteBatch spriteBatch)
        {
            if (!Context.IsPlayerFree)
                return;

            // draw each tile
            foreach (Vector2 tile in TileHelper.GetVisibleTiles())
            {
                // get tile's screen coordinates
                float screenX = tile.X * Game1.tileSize - Game1.viewport.X;
                float screenY = tile.Y * Game1.tileSize - Game1.viewport.Y;
                int tileSize = Game1.tileSize;

                // get machine group
                this.GroupTiles.TryGetValue(tile, out MachineGroup group);
                bool isGrouped = group != null;
                bool isActive = isGrouped && group.HasInternalAutomation;

                // draw background
                {
                    Color color = Color.Black * 0.5f;
                    if (isActive)
                        color = Color.Green * 0.2f;
                    else if (isGrouped)
                        color = Color.Red * 0.2f;

                    spriteBatch.DrawLine(screenX + this.TileGap, screenY + this.TileGap, new Vector2(tileSize - this.TileGap * 2, tileSize - this.TileGap * 2), color);
                }

                // draw group edge borders
                if (group != null)
                    this.DrawEdgeBorders(spriteBatch, group, tile, group.HasInternalAutomation ? Color.Green : Color.Red);
            }

            // draw cursor
            this.DrawCursor();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Draw borders for each unconnected edge of a tile.</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        /// <param name="group">The machine group.</param>
        /// <param name="tile">The group tile.</param>
        /// <param name="color">The border color.</param>
        private void DrawEdgeBorders(SpriteBatch spriteBatch, MachineGroup group, Vector2 tile, Color color)
        {
            int borderSize = 3;
            float screenX = tile.X * Game1.tileSize - Game1.viewport.X;
            float screenY = tile.Y * Game1.tileSize - Game1.viewport.Y;
            float tileSize = Game1.tileSize;

            // top
            if (!group.Tiles.Contains(new Vector2(tile.X, tile.Y - 1)))
                spriteBatch.DrawLine(screenX, screenY, new Vector2(tileSize, borderSize), color); // top

            // bottom
            if (!group.Tiles.Contains(new Vector2(tile.X, tile.Y + 1)))
                spriteBatch.DrawLine(screenX, screenY + tileSize, new Vector2(tileSize, borderSize), color); // bottom

            // left
            if (!group.Tiles.Contains(new Vector2(tile.X - 1, tile.Y)))
                spriteBatch.DrawLine(screenX, screenY, new Vector2(borderSize, tileSize), color); // left

            // right
            if (!group.Tiles.Contains(new Vector2(tile.X + 1, tile.Y)))
                spriteBatch.DrawLine(screenX + tileSize, screenY, new Vector2(borderSize, tileSize), color); // right
        }
    }
}
