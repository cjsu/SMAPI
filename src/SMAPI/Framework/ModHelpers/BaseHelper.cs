namespace StardewModdingAPI.Framework.ModHelpers
{
    /// <summary>The common base class for mod helpers.</summary>
    internal abstract class BaseHelper : IModLinked
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The unique ID of the mod for which the helper was created.</summary>
        public string ModID { get; }


        /*********
        ** Protected methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="modID">The unique ID of the relevant mod.</param>
        protected BaseHelper(string modID)
        {
            this.ModID = modID;
        }
    }
}
