using StardewTree = StardewValley.TerrainFeatures.Tree;

namespace Pathoschild.Stardew.LookupAnything.Framework.Constants
{
    /// <summary>Indicates a tree's growth stage.</summary>
    internal enum WildTreeGrowthStage
    {
        Seed = StardewTree.seedStage,
        Sprout = StardewTree.sproutStage,
        Sapling = StardewTree.saplingStage,
        Bush = StardewTree.bushStage,
        SmallTree = StardewTree.treeStage - 1, // an intermediate stage between bush and tree, no constant
        Tree = StardewTree.treeStage
    }
}
