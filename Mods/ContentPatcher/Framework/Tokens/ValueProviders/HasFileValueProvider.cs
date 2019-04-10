using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ContentPatcher.Framework.Conditions;
using Pathoschild.Stardew.Common;
using Pathoschild.Stardew.Common.Utilities;

namespace ContentPatcher.Framework.Tokens.ValueProviders
{
    /// <summary>A value provider which checks whether a file exists in the content pack's folder.</summary>
    internal class HasFileValueProvider : BaseValueProvider
    {
        /*********
        ** Fields
        *********/
        /// <summary>The mod folder from which to load assets.</summary>
        private readonly string ModFolder;

        /// <summary>The context as of the last update.</summary>
        private IContext TokenContext;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="modFolder">The absolute path to the mod folder.</param>
        public HasFileValueProvider(string modFolder)
            : base(ConditionType.HasFile, canHaveMultipleValuesForRoot: false)
        {
            this.ModFolder = modFolder;
            this.EnableInputArguments(required: true, canHaveMultipleValues: false);
        }

        /// <summary>Update the underlying values.</summary>
        /// <param name="context">The condition context.</param>
        /// <returns>Returns whether the values changed.</returns>
        public override void UpdateContext(IContext context)
        {
            this.TokenContext = context;
            this.IsValidInContext = true;
        }

        /// <summary>Get the allowed values for an input argument (or <c>null</c> if any value is allowed).</summary>
        /// <param name="input">The input argument, if applicable.</param>
        /// <exception cref="InvalidOperationException">The input argument doesn't match this value provider, or does not respect <see cref="IValueProvider.AllowsInput"/> or <see cref="IValueProvider.RequiresInput"/>.</exception>
        public override InvariantHashSet GetAllowedValues(string input)
        {
            return input != null
                ? InvariantHashSet.Boolean()
                : null;
        }

        /// <summary>Get the current values.</summary>
        /// <param name="input">The input argument, if applicable.</param>
        /// <exception cref="InvalidOperationException">The input argument doesn't match this value provider, or does not respect <see cref="IValueProvider.AllowsInput"/> or <see cref="IValueProvider.RequiresInput"/>.</exception>
        public override IEnumerable<string> GetValues(string input)
        {
            this.AssertInputArgument(input);

            yield return this.GetPathExists(input).ToString();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get whether the given file path exists.</summary>
        /// <param name="path">A relative file path.</param>
        /// <exception cref="InvalidOperationException">The path is not relative or contains directory climbing (../).</exception>
        private bool GetPathExists(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            // parse tokens
            TokenString tokenStr = new TokenString(path, this.TokenContext);
            if (tokenStr.InvalidTokens.Any())
                return false;
            tokenStr.UpdateContext(this.TokenContext);
            path = tokenStr.Value;

            // get normalised path
            if (string.IsNullOrWhiteSpace(path))
                return false;
            path = PathUtilities.NormalisePathSeparators(path);

            // validate
            if (Path.IsPathRooted(path))
                throw new InvalidOperationException($"The {ConditionType.HasFile} token requires a relative path.");
            if (!PathUtilities.IsSafeRelativePath(path))
                throw new InvalidOperationException($"The {ConditionType.HasFile} token requires a relative path and cannot contain directory climbing (../).");

            // check file existence
            string fullPath = Path.Combine(this.ModFolder, PathUtilities.NormalisePathSeparators(path));
            return File.Exists(fullPath);
        }
    }
}
