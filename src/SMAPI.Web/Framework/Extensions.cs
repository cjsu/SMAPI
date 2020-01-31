using System;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace StardewModdingAPI.Web.Framework
{
    /// <summary>Provides extensions on ASP.NET Core types.</summary>
    public static class Extensions
    {
        /// <summary>Get a URL with the absolute path for an action method. Unlike <see cref="IUrlHelper.Action"/>, only the specified <paramref name="values"/> are added to the URL without merging values from the current HTTP request.</summary>
        /// <param name="helper">The URL helper to extend.</param>
        /// <param name="action">The name of the action method.</param>
        /// <param name="controller">The name of the controller.</param>
        /// <param name="values">An object that contains route values.</param>
        /// <param name="absoluteUrl">Get an absolute URL instead of a server-relative path/</param>
        /// <returns>The generated URL.</returns>
        public static string PlainAction(this IUrlHelper helper, [AspMvcAction] string action, [AspMvcController] string controller, object values = null, bool absoluteUrl = false)
        {
            RouteValueDictionary valuesDict = new RouteValueDictionary(values);
            foreach (var value in helper.ActionContext.RouteData.Values)
            {
                if (!valuesDict.ContainsKey(value.Key))
                    valuesDict[value.Key] = null; // explicitly remove it from the URL
            }

            string url = helper.Action(action, controller, valuesDict);
            if (absoluteUrl)
            {
                HttpRequest request = helper.ActionContext.HttpContext.Request;
                Uri baseUri = new Uri($"{request.Scheme}://{request.Host}");
                url = new Uri(baseUri, url).ToString();
            }
            return url;
        }
    }
}
