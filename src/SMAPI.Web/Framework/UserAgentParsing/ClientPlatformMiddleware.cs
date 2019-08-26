using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace StardewModdingAPI.Web.Framework.UserAgentParsing
{
    /// <summary>Middleware that detects the client's platform.</summary>
    public class ClientPlatformMiddleware
    {
        /// <summary>The key used to retrieve the client's platform from <see cref="HttpContext.Items"/>.</summary>
        public const string ClientPlatformKey = "ClientPlatformKey";

        /// <summary>The next delegate in the middleware pipeline.</summary>
        private readonly RequestDelegate Next;

        /// <summary>Construct an instance.</summary>
        /// <param name="next">The next delegate in the middleware pipeline.</param>
        public ClientPlatformMiddleware(RequestDelegate next)
        {
            this.Next = next;
        }

        /// <summary>Invoke the middleware.</summary>
        /// <param name="context">The HTTP request context.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            context.Items[ClientPlatformMiddleware.ClientPlatformKey] = this.DetectClientPlatform(context.Request.Headers["User-Agent"]);

            await this.Next(context);
        }

        /// <summary>Detect the platform that the client is on.</summary>
        /// <param name="userAgent">The client's user agent.</param>
        /// <returns>The client's platform, or null if no platforms could be detected.</returns>
        private Platform? DetectClientPlatform(string userAgent)
        {
            switch (userAgent)
            {
                case string ua when ua.Contains("Windows"):
                    return Platform.Windows;
                // check for Android before Linux because Android user agents also contain Linux
                case string ua when ua.Contains("Android"):
                    return Platform.Android;
                case string ua when ua.Contains("Linux"):
                    return Platform.Linux;
                case string ua when ua.Contains("Mac"):
                    return Platform.Mac;
                default:
                    return null;
            }
        }
    }
}
