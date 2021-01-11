using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using SMAPI.Web.LegacyRedirects.Framework;
using StardewModdingAPI.Toolkit.Serialization;

namespace SMAPI.Web.LegacyRedirects
{
    /// <summary>The web app startup configuration.</summary>
    public class Startup
    {
        /*********
        ** Public methods
        *********/
        /// <summary>The method called by the runtime to add services to the container.</summary>
        /// <param name="services">The service injection container.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddControllers()
                .AddNewtonsoftJson(options => Startup.ConfigureJsonNet(options.SerializerSettings));
        }

        /// <summary>The method called by the runtime to configure the HTTP request pipeline.</summary>
        /// <param name="app">The application builder.</param>
        /// <param name="env">The hosting environment.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app
                .UseRewriter(this.GetRedirectRules())
                .UseRouting()
                .UseAuthorization()
                .UseEndpoints(endpoints => endpoints.MapControllers());
        }

        /// <summary>Configure a Json.NET serializer.</summary>
        /// <param name="settings">The serializer settings to edit.</param>
        internal static void ConfigureJsonNet(JsonSerializerSettings settings)
        {
            foreach (JsonConverter converter in new JsonHelper().JsonSettings.Converters)
                settings.Converters.Add(converter);

            settings.Formatting = Formatting.Indented;
            settings.NullValueHandling = NullValueHandling.Ignore;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get the redirect rules to apply.</summary>
        private RewriteOptions GetRedirectRules()
        {
            var redirects = new RewriteOptions();

            redirects.Add(
                new LambdaRewriteRule((context, request, response) =>
                {
                    string host = request.Host.Host;

                    // map API requests to proxy
                    // This is needed because the low-level HTTP client SMAPI uses for Linux/Mac compatibility doesn't support redirects.
                    if (host == "api.smapi.io")
                    {
                        request.Path = $"/api{request.Path}";
                        return;
                    }

                    // redirect other requests to Azure
                    string newRoot = host switch
                    {
                        "api.smapi.io" => "smapi.io/api",
                        "json.smapi.io" => "smapi.io/json",
                        "log.smapi.io" => "smapi.io/log",
                        "mods.smapi.io" => "smapi.io/mods",
                        _ => "smapi.io"
                    };
                    response.StatusCode = (int)HttpStatusCode.PermanentRedirect;
                    response.Headers["Location"] = $"{(request.IsHttps ? "https" : "http")}://{newRoot}{request.PathBase}{request.Path}{request.QueryString}";
                    context.Result = RuleResult.EndResponse;
                })
            );

            return redirects;
        }
    }
}
