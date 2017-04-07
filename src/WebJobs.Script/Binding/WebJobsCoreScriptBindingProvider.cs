// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Script.Extensibility;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Script.Binding
{
    /// <summary>
    /// Enables all Core SDK Triggers/Binders
    /// </summary>
    internal class WebJobsCoreScriptBindingProvider : ScriptBindingProvider
    {
        public WebJobsCoreScriptBindingProvider(JobHostConfiguration config, JObject hostMetadata, TraceWriter traceWriter) 
            : base(config, hostMetadata, traceWriter)
        {
        }

        public override void Initialize()
        {
           // Extensions will already apply configuraiton 

            // Apply Blobs configuration
            Config.Blobs.CentralizedPoisonQueue = true;   // TEMP : In the next release we'll remove this and accept the core SDK default
            var configSection = (JObject)Metadata["blobs"];
            JToken value = null;
            if (configSection != null)
            {
                if (configSection.TryGetValue("centralizedPoisonQueue", out value))
                {
                    Config.Blobs.CentralizedPoisonQueue = (bool)value;
                }
            }
        
            Config.UseScriptExtensions();
        }

        public override bool TryCreate(ScriptBindingContext context, out ScriptBinding binding)
        {
            binding = null;

            if (string.Compare(context.Type, "blobTrigger", StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(context.Type, "blob", StringComparison.OrdinalIgnoreCase) == 0)
            {
                binding = new BlobScriptBinding(context);
            }
            else if (string.Compare(context.Type, "httpTrigger", StringComparison.OrdinalIgnoreCase) == 0)
            {
                binding = new HttpScriptBinding(context);
            }
            else if (string.Compare(context.Type, "manualTrigger", StringComparison.OrdinalIgnoreCase) == 0)
            {
                binding = new ManualScriptBinding(context);
            }
            // Queue, Table handled by general case. 

            return binding != null;
        }

        private class HttpScriptBinding : ScriptBinding
        {
            public HttpScriptBinding(ScriptBindingContext context) : base(context)
            {
            }

            public override Type DefaultType
            {
                get
                {
                    return typeof(HttpRequestMessage);
                }
            }

            public override Collection<Attribute> GetAttributes()
            {
                Collection<Attribute> attributes = new Collection<Attribute>();

                attributes.Add(new HttpTriggerAttribute
                {
                    RouteTemplate = Context.GetMetadataValue<string>("route")
                });

                return attributes;
            }
        }

        private class ManualScriptBinding : ScriptBinding
        {
            public ManualScriptBinding(ScriptBindingContext context) : base(context)
            {
            }

            public override Type DefaultType
            {
                get
                {
                    return typeof(string);
                }
            }

            public override Collection<Attribute> GetAttributes()
            {
                Collection<Attribute> attributes = new Collection<Attribute>();

                attributes.Add(new ManualTriggerAttribute());

                return attributes;
            }
        }

        private class BlobScriptBinding : ScriptBinding
        {
            public BlobScriptBinding(ScriptBindingContext context) : base(context)
            {
            }

            public override Type DefaultType
            {
                get
                {
                    return typeof(Stream);
                }
            }

            public override Collection<Attribute> GetAttributes()
            {
                Collection<Attribute> attributes = new Collection<Attribute>();

                string path = Context.GetMetadataValue<string>("path");
                if (Context.IsTrigger)
                {
                    attributes.Add(new BlobTriggerAttribute(path));
                }
                else
                {
                    attributes.Add(new BlobAttribute(path, Context.Access));
                }

                string account = Context.GetMetadataValue<string>("connection");
                if (!string.IsNullOrEmpty(account))
                {
                    attributes.Add(new StorageAccountAttribute(account));
                }

                return attributes;
            }
        }
    }
}
