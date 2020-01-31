using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;

namespace SMAPI.Web.LegacyRedirects.Framework
{
    /// <summary>Rewrite requests to prepend the subdomain portion (if any) to the path.</summary>
    /// <remarks>Derived from <a href="https://stackoverflow.com/a/44526747/262123" />.</remarks>
    internal class LambdaRewriteRule : IRule
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Rewrite an HTTP request if needed.</summary>
        private readonly Action<RewriteContext, HttpRequest, HttpResponse> Rewrite;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="rewrite">Rewrite an HTTP request if needed.</param>
        public LambdaRewriteRule(Action<RewriteContext, HttpRequest, HttpResponse> rewrite)
        {
            this.Rewrite = rewrite ?? throw new ArgumentNullException(nameof(rewrite));
        }

        /// <summary>Applies the rule. Implementations of ApplyRule should set the value for <see cref="RewriteContext.Result" /> (defaults to RuleResult.ContinueRules).</summary>
        /// <param name="context">The rewrite context.</param>
        public void ApplyRule(RewriteContext context)
        {
            HttpRequest request = context.HttpContext.Request;
            HttpResponse response = context.HttpContext.Response;
            this.Rewrite(context, request, response);
        }
    }
}
