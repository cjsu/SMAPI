using System.Linq;
using ContentPatcher.Framework.Conditions;
using ContentPatcher.Framework.Patches;
using ContentPatcher.Framework.Tokens;

namespace ContentPatcher.Framework.Commands
{
    /// <summary>A summary of patch info shown in the SMAPI console.</summary>
    internal class PatchInfo
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The patch name shown in log messages, without the content pack prefix.</summary>
        public string ShortName { get; }

        /// <summary>The patch type.</summary>
        public string Type { get; }

        /// <summary>The asset name to intercept.</summary>
        public string RawTargetAsset { get; }

        /// <summary>The parsed asset name (if available).</summary>
        public TokenString ParsedTargetAsset { get; }

        /// <summary>The parsed conditions (if available).</summary>
        public ConditionDictionary ParsedConditions { get; }

        /// <summary>The content pack which requested the patch.</summary>
        public ManagedContentPack ContentPack { get; }

        /// <summary>Whether the patch is loaded.</summary>
        public bool IsLoaded { get; }

        /// <summary>Whether the patch should be applied in the current context.</summary>
        public bool MatchesContext { get; }

        /// <summary>Whether the patch is currently applied.</summary>
        public bool IsApplied { get; }

        /// <summary>The reason this patch is disabled (if applicable).</summary>
        public string ReasonDisabled { get; }

        /// <summary>The tokens used by this patch in its fields.</summary>
        public TokenName[] TokensUsed { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="patch">The patch to represent.</param>
        public PatchInfo(DisabledPatch patch)
        {
            this.ShortName = this.GetShortName(patch.ContentPack, patch.LogName);
            this.Type = patch.Type;
            this.RawTargetAsset = patch.AssetName;
            this.ParsedTargetAsset = null;
            this.ParsedConditions = null;
            this.ContentPack = patch.ContentPack;
            this.IsLoaded = false;
            this.MatchesContext = false;
            this.IsApplied = false;
            this.ReasonDisabled = patch.ReasonDisabled;
            this.TokensUsed = new TokenName[0];
        }

        /// <summary>Construct an instance.</summary>
        /// <param name="patch">The patch to represent.</param>
        public PatchInfo(IPatch patch)
        {
            this.ShortName = this.GetShortName(patch.ContentPack, patch.LogName);
            this.Type = patch.Type.ToString();
            this.RawTargetAsset = patch.RawTargetAsset.Raw;
            this.ParsedTargetAsset = patch.RawTargetAsset;
            this.ParsedConditions = patch.Conditions;
            this.ContentPack = patch.ContentPack;
            this.IsLoaded = true;
            this.MatchesContext = patch.MatchesContext;
            this.IsApplied = patch.IsApplied;
            this.TokensUsed = patch.GetTokensUsed().ToArray();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get the patch name shown in log messages, without the content pack prefix.</summary>
        /// <param name="contentPack">The content pack which requested the patch.</param>
        /// <param name="logName">The unique patch name shown in log messages.</param>
        private string GetShortName(ManagedContentPack contentPack, string logName)
        {
            string prefix = contentPack.Manifest.Name + " > ";
            return logName.StartsWith(prefix)
                ? logName.Substring(prefix.Length)
                : logName;
        }
    }
}
