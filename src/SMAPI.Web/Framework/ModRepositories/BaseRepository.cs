using System.Text.RegularExpressions;
using System.Threading.Tasks;
using StardewModdingAPI.Toolkit.Framework.UpdateData;

namespace StardewModdingAPI.Web.Framework.ModRepositories
{
    internal abstract class RepositoryBase : IModRepository
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The unique key for this vendor.</summary>
        public ModRepositoryKey VendorKey { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public abstract void Dispose();

        /// <summary>Get metadata about a mod in the repository.</summary>
        /// <param name="id">The mod ID in this repository.</param>
        public abstract Task<ModInfoModel> GetModInfoAsync(string id);


        /*********
        ** Protected methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="vendorKey">The unique key for this vendor.</param>
        protected RepositoryBase(ModRepositoryKey vendorKey)
        {
            this.VendorKey = vendorKey;
        }

        /// <summary>Normalize a version string.</summary>
        /// <param name="version">The version to normalize.</param>
        protected string NormalizeVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return null;

            version = version.Trim();
            if (Regex.IsMatch(version, @"^v\d", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)) // common version prefix
                version = version.Substring(1);

            return version;
        }
    }
}
