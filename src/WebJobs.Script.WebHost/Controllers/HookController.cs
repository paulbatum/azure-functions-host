// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Azure.WebJobs.Host.Config;

namespace Microsoft.Azure.WebJobs.Script.WebHost.Controllers
{
    // Custom web hooks for extensions. 
    public class HookController : ApiController
    {
        private readonly WebScriptHostManager _scriptHostManager;

        public HookController(WebScriptHostManager scriptHostManager)
        {
            _scriptHostManager = scriptHostManager;
        }

        [Route("hook/{name}")]
        [HttpGet]
        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> ExtensionHook(string name, CancellationToken token)
        {
            var host = this._scriptHostManager.Instance;

            IAsyncConverter<HttpRequestMessage, HttpResponseMessage> hook;
            if (host.CustomHttpHandlers.TryGetValue(name, out hook))
            {
                var response = await hook.ConvertAsync(this.Request, token);
                return response;
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        // Provides the URL fpr accessing this route. 
        internal class HookProvider : IWebhookProvider
        {
            public string GetUrl(Type extensionType)
            {
                // key the URL off extension name since that's stalbe value.
                string key = extensionType.Name;

                var hostName = 
                    ConfigurationManager.AppSettings["WEBSITE_HOSTNAME_PROXY"] ??
                    ConfigurationManager.AppSettings["WEBSITE_HOSTNAME"];

                if (hostName == null)
                {
                    return null;
                }

                return $"https://{hostName}/hook/{key}";                
            }
        }
    }
}