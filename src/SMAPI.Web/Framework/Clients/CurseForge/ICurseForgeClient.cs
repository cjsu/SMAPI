using System;
using System.Threading.Tasks;

namespace StardewModdingAPI.Web.Framework.Clients.CurseForge
{
    /// <summary>An HTTP client for fetching mod metadata from the CurseForge API.</summary>
    internal interface ICurseForgeClient : IDisposable
    {
        /*********
        ** Methods
        *********/
        /// <summary>Get metadata about a mod.</summary>
        /// <param name="id">The CurseForge mod ID.</param>
        /// <returns>Returns the mod info if found, else <c>null</c>.</returns>
        Task<CurseForgeMod> GetModAsync(long id);
    }
}
