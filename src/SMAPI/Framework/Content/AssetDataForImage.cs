using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StardewModdingAPI.Framework.Content
{
    /// <summary>Encapsulates access and changes to image content being read from a data file.</summary>
    internal class AssetDataForImage : AssetData<Texture2D>, IAssetDataForImage
    {
        /*********
        ** Fields
        *********/
        /// <summary>The minimum value to consider non-transparent.</summary>
        /// <remarks>On Linux/Mac, fully transparent pixels may have an alpha up to 4 for some reason.</remarks>
        private const byte MinOpacity = 5;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="locale">The content's locale code, if the content is localized.</param>
        /// <param name="assetName">The normalized asset name being read.</param>
        /// <param name="data">The content data being read.</param>
        /// <param name="getNormalizedPath">Normalizes an asset key to match the cache key.</param>
        /// <param name="onDataReplaced">A callback to invoke when the data is replaced (if any).</param>
        public AssetDataForImage(string locale, string assetName, Texture2D data, Func<string, string> getNormalizedPath, Action<Texture2D> onDataReplaced)
            : base(locale, assetName, data, getNormalizedPath, onDataReplaced) { }

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
            if (source == null)
                throw new ArgumentNullException(nameof(source), "Can't patch from a null source texture.");
            Texture2D target = this.Data;

            // get areas
            sourceArea = sourceArea ?? new Rectangle(0, 0, source.Width, source.Height);
            targetArea = targetArea ?? new Rectangle(0, 0, Math.Min(sourceArea.Value.Width, target.Width), Math.Min(sourceArea.Value.Height, target.Height));

            // validate
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
                // get target data
                Color[] targetData = new Color[pixelCount];
                target.GetData(0, targetArea, targetData, 0, pixelCount);

                // merge pixels
                Color[] newData = new Color[targetArea.Value.Width * targetArea.Value.Height];
                target.GetData(0, targetArea, newData, 0, newData.Length);
                for (int i = 0; i < sourceData.Length; i++)
                {
                    Color above = sourceData[i];
                    Color below = targetData[i];

                    // shortcut transparency
                    if (above.A < AssetDataForImage.MinOpacity)
                        continue;
                    if (below.A < AssetDataForImage.MinOpacity)
                    {
                        newData[i] = above;
                        continue;
                    }

                    // merge pixels
                    // This performs a conventional alpha blend for the pixels, which are already
                    // premultiplied by the content pipeline. The formula is derived from
                    // https://blogs.msdn.microsoft.com/shawnhar/2009/11/06/premultiplied-alpha/.
                    // Note: don't use named arguments here since they're different between
                    // Linux/Mac and Windows.
                    float alphaBelow = 1 - (above.A / 255f);
                    newData[i] = new Color(
                        (int)(above.R + (below.R * alphaBelow)), // r
                        (int)(above.G + (below.G * alphaBelow)), // g
                        (int)(above.B + (below.B * alphaBelow)), // b
                        Math.Max(above.A, below.A) // a
                    );
                }
                sourceData = newData;
            }

            // patch target texture
            target.SetData(0, targetArea, sourceData, 0, pixelCount);
        }
    }
}
