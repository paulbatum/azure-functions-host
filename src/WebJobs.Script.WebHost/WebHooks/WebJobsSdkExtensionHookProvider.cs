// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Script.WebHost.Controllers;
using HttpHandler = Microsoft.Azure.WebJobs.IAsyncConverter<System.Net.Http.HttpRequestMessage, System.Net.Http.HttpResponseMessage>;

namespace Microsoft.Azure.WebJobs.Script.WebHost
{
    // Gives binding extensions access to a http handler. 
    // This is registered with the JobHostConfiguration and extensions will call on it to register for a handler. 
    internal class WebJobsSdkExtensionHookProvider : IWebhookProvider
    {
        // Map from an extension name to a http handler. 
        private IDictionary<string, HttpHandler> _customHttpHandlers = new Dictionary<string, HttpHandler>(StringComparer.OrdinalIgnoreCase);

        public IDictionary<string, HttpHandler> CustomHttpHandlers => _customHttpHandlers;

        public Uri GetUrl(IExtensionConfigProvider extension)
        {
            var extensionType = extension.GetType();
            var handler = extension as HttpHandler;
            if (handler == null)
            {
                throw new InvalidOperationException($"Extension must implemnent IAsyncConverter<HttpRequestMessage, HttpResponseMessage> in order to receive hooks");
            }

            string name = extensionType.Name;
            _customHttpHandlers[name] = handler;

            return AdminController.GetExtensionHook(name);
        }
    }
}