using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Pathoschild.Http.Client;
using StardewModdingAPI.Toolkit.Framework.Clients.WebApi;

namespace SMAPI.Web.LegacyRedirects.Controllers
{
    /// <summary>Provides an API to perform mod update checks.</summary>
    [ApiController]
    [Produces("application/json")]
    [Route("api/v{version}/mods")]
    public class ModsApiController : Controller
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Fetch version metadata for the given mods.</summary>
        /// <param name="model">The mod search criteria.</param>
        [HttpPost]
        public async Task<IEnumerable<ModEntryModel>> PostAsync([FromBody] ModSearchModel model)
        {
            using IClient client = new FluentClient("https://smapi.io/api");

            Startup.ConfigureJsonNet(client.Formatters.JsonFormatter.SerializerSettings);

            return await client
                .PostAsync(this.Request.Path)
                .WithBody(model)
                .AsArray<ModEntryModel>();
        }
    }
}
