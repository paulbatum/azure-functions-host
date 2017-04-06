// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Script.Description;
using Microsoft.Azure.WebJobs.Script.Extensibility;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Microsoft.Azure.WebJobs.Script.Binding
{
    // General binder that works directly off SDK interfaces. 
    class GeneralScriptBindingProvider : ScriptBindingProvider
    {
        // the constructor is fixed, so we need to pass additional information  $$$
        public IMetadataTooling Tooling { get; set; }

        public GeneralScriptBindingProvider(
            JobHostConfiguration config, 
            JObject hostMetadata, 
            TraceWriter traceWriter) :
            base(config, hostMetadata, traceWriter)
        {
        }

        public override bool TryCreate(ScriptBindingContext context, out ScriptBinding binding)
        {
            string name = context.Type;
            var attrType = this.Tooling.GetAttributeTypeFromName(name);
            if (attrType == null)
            {
                binding = null;
                return false;
            }

            var attr = this.Tooling.GetAttribute(attrType, context.Metadata);
                        
            binding = new GeneralScriptBinding(this.Tooling, attr, context);
            return true;
        }

        public override bool TryResolveAssembly(string assemblyName, out Assembly assembly)
        {
            assembly = this.Tooling.TryResolveAssembly(assemblyName);
            return (assembly != null);
        }


        // Context.DataType may frequently be missing. 
        static Type GetRequestedType(ScriptBindingContext context)
        {
            Type type = ParseDataType(context);

            if (type == null)
            {
                return null;
            }

            // $$$ error if Cardinality is set but type isn't? 

            Cardinality cardinality;
            if (!Enum.TryParse<Cardinality>(context.Cardinality, out cardinality))
            {
                cardinality = Cardinality.One; // default 
            }

            if (cardinality == Cardinality.Many)
            {
                // arrays are supported for both trigger input as well
                // as output bindings
                type = type.MakeArrayType();
            }
            return type;

        }

        // Parse the DataType field and return as a System.Type.
        private static Type ParseDataType(ScriptBindingContext context)
        {
            DataType result;
            if (Enum.TryParse<DataType>(context.DataType, out result))
            {
                switch (result)
                {
                    case DataType.Binary:
                        return typeof(byte[]);

                    case DataType.Stream:
                        return typeof(Stream);

                    case DataType.String:
                        return typeof(string);
                }
            }

            return null; ;
        }

        class GeneralScriptBinding : ScriptBinding, IResultProcessingBinding
        {
            private readonly Attribute _attribute;
            private readonly IMetadataTooling _tooling;

            private Type _defaultType;

            private MethodInfo _applyReturn; // Action<object,object>

            public GeneralScriptBinding(IMetadataTooling tooling, Attribute attribute, ScriptBindingContext context)
                : base(context)
            {
                _tooling = tooling;
                _attribute = attribute;

                _applyReturn = attribute.GetType().GetMethod("ApplyReturn", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);              
            }

            public bool CanProcessResult(object result)
            {
                return _applyReturn != null;
            }

            public void ProcessResult(
                IDictionary<string, object> functionArguments, 
                object[] systemArguments, 
                string triggerInputName, 
                object result)
            {
                if (result == null)
                {
                    return;
                }
                                
                object context;
                if (functionArguments.TryGetValue(triggerInputName, out context))
                {
                    _applyReturn.Invoke(null, new object[] { context, result } );
                }
            }

            // This should only be called in script scenarios (not C#). 
            // So explicitly make it lazy. 
            public override Type DefaultType
            {
                get
                {
                    if (_defaultType == null)
                    {                        
                        Type requestedType = GetRequestedType(this.Context);
                        _defaultType = _tooling.GetDefaultType(_attribute, this.Context.Access, requestedType);
                    }
                    return _defaultType;
                }
            }

            public override Collection<Attribute> GetAttributes()
            {
                return new Collection<Attribute> { _attribute } ;
            }
        }
    }
}
