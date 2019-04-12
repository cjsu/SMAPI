using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathoschild.Stardew.LookupAnything.Framework.Constants;
using Pathoschild.Stardew.LookupAnything.Framework.DebugFields;
using Pathoschild.Stardew.LookupAnything.Framework.Fields;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace Pathoschild.Stardew.LookupAnything.Framework.Subjects
{
    /// <summary>Describes a non-fruit tree.</summary>
    internal class TreeSubject : BaseSubject
    {
        /*********
        ** Fields
        *********/
        /// <summary>The underlying target.</summary>
        private readonly Tree Target;

        /// <summary>The tree's tile position.</summary>
        private readonly Vector2 Tile;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="gameHelper">Provides utility methods for interacting with the game code.</param>
        /// <param name="tree">The lookup target.</param>
        /// <param name="tile">The tree's tile position.</param>
        /// <param name="translations">Provides translations stored in the mod folder.</param>
        public TreeSubject(GameHelper gameHelper, Tree tree, Vector2 tile, ITranslationHelper translations)
            : base(gameHelper, TreeSubject.GetName(translations, tree), null, translations.Get(L10n.Types.Tree), translations)
        {
            this.Target = tree;
            this.Tile = tile;
        }

        /// <summary>Get the data to display for this subject.</summary>
        /// <param name="metadata">Provides metadata that's not available from the game data directly.</param>
        /// <remarks>Tree growth algorithm reverse engineered from <see cref="StardewValley.TerrainFeatures.Tree.dayUpdate"/>.</remarks>
        public override IEnumerable<ICustomField> GetData(Metadata metadata)
        {
            Tree tree = this.Target;

            // get growth stage
            WildTreeGrowthStage stage = (WildTreeGrowthStage)Math.Min(tree.growthStage.Value, (int)WildTreeGrowthStage.Tree);
            bool isFullyGrown = stage == WildTreeGrowthStage.Tree;
            yield return new GenericField(this.GameHelper, this.Translate(L10n.Tree.Stage), isFullyGrown
                ? this.Translate(L10n.Tree.StageDone)
                : this.Translate(L10n.Tree.StagePartial, new { stageName = this.Translate(L10n.For(stage)), step = (int)stage, max = (int)WildTreeGrowthStage.Tree })
            );

            // get growth scheduler
            if (!isFullyGrown)
            {
                string label = this.Translate(L10n.Tree.NextGrowth);
                if (Game1.IsWinter && !Game1.currentLocation.IsGreenhouse)
                    yield return new GenericField(this.GameHelper, label, this.Translate(L10n.Tree.NextGrowthWinter));
                else if (stage == WildTreeGrowthStage.SmallTree && this.HasAdjacentTrees(this.Tile))
                    yield return new GenericField(this.GameHelper, label, this.Translate(L10n.Tree.NextGrowthAdjacentTrees));
                else
                    yield return new GenericField(this.GameHelper, label, this.Translate(L10n.Tree.NextGrowthRandom, new { stage = this.Translate(L10n.For(stage + 1)) }));
            }

            // get seed
            if (isFullyGrown)
                yield return new GenericField(this.GameHelper, this.Translate(L10n.Tree.HasSeed), this.Stringify(tree.hasSeed.Value));
        }

        /// <summary>Get the data to display for this subject.</summary>
        /// <param name="metadata">Provides metadata that's not available from the game data directly.</param>
        public override IEnumerable<IDebugField> GetDebugFields(Metadata metadata)
        {
            Tree target = this.Target;

            // pinned fields
            yield return new GenericDebugField("has seed", this.Stringify(target.hasSeed.Value), pinned: true);
            yield return new GenericDebugField("growth stage", target.growthStage.Value, pinned: true);
            yield return new GenericDebugField("health", target.health.Value, pinned: true);

            // raw fields
            foreach (IDebugField field in this.GetDebugFieldsFrom(target))
                yield return field;
        }

        /// <summary>Draw the subject portrait (if available).</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        /// <param name="position">The position at which to draw.</param>
        /// <param name="size">The size of the portrait to draw.</param>
        /// <returns>Returns <c>true</c> if a portrait was drawn, else <c>false</c>.</returns>
        public override bool DrawPortrait(SpriteBatch spriteBatch, Vector2 position, Vector2 size)
        {
            this.Target.drawInMenu(spriteBatch, position, Vector2.Zero, 1, 1);
            return true;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get a display name for the tree.</summary>
        /// <param name="translations">Provides translations stored in the mod folder.</param>
        /// <param name="tree">The tree object.</param>
        private static string GetName(ITranslationHelper translations, Tree tree)
        {
            TreeType type = (TreeType)tree.treeType.Value;
            switch (type)
            {
                case TreeType.Maple:
                    return translations.Get(L10n.Tree.NameMaple);
                case TreeType.Oak:
                    return translations.Get(L10n.Tree.NameOak);
                case TreeType.Pine:
                    return translations.Get(L10n.Tree.NamePine);
                case TreeType.Palm:
                    return translations.Get(L10n.Tree.NamePalm);
                case TreeType.BigMushroom:
                    return translations.Get(L10n.Tree.NameBigMushroom);
                default:
                    return translations.Get(L10n.Tree.NameUnknown);
            }
        }

        /// <summary>Whether there are adjacent trees that prevent growth.</summary>
        /// <param name="position">The tree's position in the current location.</param>
        private bool HasAdjacentTrees(Vector2 position)
        {
            GameLocation location = Game1.currentLocation;
            return (
                from adjacentTile in Utility.getSurroundingTileLocationsArray(position)
                let otherTree = location.terrainFeatures.ContainsKey(adjacentTile)
                    ? location.terrainFeatures[adjacentTile] as Tree
                    : null
                select otherTree != null && otherTree.growthStage.Value >= (int)WildTreeGrowthStage.SmallTree
            ).Any(p => p);
        }
    }
}
