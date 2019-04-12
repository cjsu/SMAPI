using StardewModdingAPI;

namespace UIInfoSuite
{
    class ModConfig
    {
        public string[] KeysForBarrelAndCropTimes { get; set; } = new string[]
        {
            SButton.LeftShift.ToString()
        };

        public bool CanRightClickForBarrelAndCropTimes { get; set; } = true;

        public int[][] Sprinkler { get; set; } = new int[][]
        {
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 1, 0, 1, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }
        };

        public int[][] QualitySprinkler { get; set; } = new int[][]
        {
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 1, 0, 1, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }
        };

        public int[][] IridiumSprinkler { get; set; } = new int[][]
        {
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0 },
            new int[] { 0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0 },
            new int[] { 0, 0, 0, 1, 1, 0, 1, 1, 0, 0, 0 },
            new int[] { 0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0 },
            new int[] { 0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }
        };
		
		public int[][] PrismaticSprinkler { get; set; } = new int[][]
        {
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0 },
            new int[] { 0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0 },
            new int[] { 0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0 },
            new int[] { 0, 0, 1, 1, 1, 0, 1, 1, 1, 0, 0 },
            new int[] { 0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0 },
            new int[] { 0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0 },
            new int[] { 0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }
        };

        public int[][] Beehouse { get; set; } = new int[][]
        {
            new int[] { 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0 },
            new int[] { 0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0 },
            new int[] { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0 },
            new int[] { 0, 1, 1, 1, 1, 0, 1, 1, 1, 1, 0 },
            new int[] { 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0 },
            new int[] { 0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0 },
            new int[] { 0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0 },
            new int[] { 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 }
        };
    }
}
