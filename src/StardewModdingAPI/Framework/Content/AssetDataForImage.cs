using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StardewModdingAPI.Framework.Content
{
    /// <summary>Encapsulates access and changes to dictionary content being read from a data file.</summary>
    internal class AssetDataForImage : AssetData<Texture2D>, IAssetDataForImage
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="locale">The content's locale code, if the content is localised.</param>
        /// <param name="assetName">The normalised asset name being read.</param>
        /// <param name="data">The content data being read.</param>
        /// <param name="getNormalisedPath">Normalises an asset key to match the cache key.</param>
        public AssetDataForImage(string locale, string assetName, Texture2D data, Func<string, string> getNormalisedPath)
            : base(locale, assetName, data, getNormalisedPath) { }

        /// <summary>Overwrite part of the image.</summary>
        /// <param name="source">The image to patch into the content.</param>
        /// <param name="sourceArea">The part of the <paramref name="source"/> to copy (or <c>null</c> to take the whole texture). This must be within the bounds of the <paramref name="source"/> texture.</param>
        /// <param name="targetArea">The part of the content to patch (or <c>null</c> to patch the whole texture). The original content within this area will be erased. This must be within the bounds of the existing spritesheet.</param>
        /// <param name="patchMode">Indicates how an image should be patched.</param>
        /// <exception cref="ArgumentNullException">One of the arguments is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="targetArea"/> is outside the bounds of the spritesheet.</exception>
        public void PatchImage(Texture2D source, Rectangle? sourceArea = null, Rectangle? targetArea = null, PatchMode patchMode = PatchMode.Replace)
        {
            // get texture
            Texture2D target = this.Data;

            // get areas
            sourceArea = sourceArea ?? new Rectangle(0, 0, source.Width, source.Height);
            targetArea = targetArea ?? new Rectangle(0, 0, Math.Min(sourceArea.Value.Width, target.Width), Math.Min(sourceArea.Value.Height, target.Height));

            // validate
            if (source == null)
                throw new ArgumentNullException(nameof(source), "Can't patch from a null source texture.");
            if (sourceArea.Value.X < 0 || sourceArea.Value.Y < 0 || sourceArea.Value.Right > source.Width || sourceArea.Value.Bottom > source.Height)
                throw new ArgumentOutOfRangeException(nameof(sourceArea), "The source area is outside the bounds of the source texture.");
            if (targetArea.Value.X < 0 || targetArea.Value.Y < 0 || targetArea.Value.Right > target.Width || targetArea.Value.Bottom > target.Height)
                throw new ArgumentOutOfRangeException(nameof(targetArea), "The target area is outside the bounds of the target texture.");
            if (sourceArea.Value.Width != targetArea.Value.Width || sourceArea.Value.Height != targetArea.Value.Height)
                throw new InvalidOperationException("The source and target areas must be the same size.");

            // get source data
            int pixelCount = sourceArea.Value.Width * sourceArea.Value.Height;
            Color[] sourceData = new Color[pixelCount];
            source.GetData(0, sourceArea, sourceData, 0, pixelCount);

            // merge data in overlay mode
            if (patchMode == PatchMode.Overlay)
            {
                Color[] newData = new Color[targetArea.Value.Width * targetArea.Value.Height];
                target.GetData(0, targetArea, newData, 0, newData.Length);
                for (int i = 0; i < sourceData.Length; i++)
                {
                    Color pixel = sourceData[i];
                    if (pixel.A != 0) // not transparent
                        newData[i] = pixel;
                }
                sourceData = newData;
            }

            // patch target texture
            target.SetData(0, targetArea, sourceData, 0, pixelCount);
        }
    }
}
