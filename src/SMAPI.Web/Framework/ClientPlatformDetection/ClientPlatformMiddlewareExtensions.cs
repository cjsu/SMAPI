using Microsoft.AspNetCore.Builder;

namespace StardewModdingAPI.Web.Framework.ClientPlatformDetection
{
    /// <summary>Extension methods for the client platform middleware.</summary>
    internal static class ClientPlatformMiddlewareExtensions
    {
        /// <summary>Adds client platform detection to the request pipeline.</summary>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder with the client platform middleware enabled.</returns>
        public static IApplicationBuilder UseClientPlatform(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ClientPlatformMiddleware>();
        }
    }
}
