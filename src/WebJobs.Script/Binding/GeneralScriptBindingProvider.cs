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

namespace Microsoft.Azure.WebJobs.Script.Binding
{
    // General binder that works directly off SDK interfaces. 
    class GeneralScriptBindingProvider : ScriptBindingProvider
    {
        public ITooling Tooling { get; set; }

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

            var attrs = this.Tooling.GetAttributes(attrType, context.Metadata);
                        
            binding = new GeneralScriptBinding(this.Tooling, attrs, context);
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
                // DataType field is commonly missing. Default to JObject.  
                return typeof(JObject);
            }

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

        class GeneralScriptBinding : ScriptBinding
        {
            private readonly Attribute[] _attributes;
            private readonly ITooling _tooling;

            private Type _defaultType;

            public GeneralScriptBinding(ITooling tooling, Attribute[] attributes, ScriptBindingContext context)
                : base(context)
            {
                _tooling = tooling;
                _attributes = attributes;
            }

            // This should only be called in script scenarios (not C#). 
            // So explicitly make it lazy. 
            public override Type DefaultType
            {
                get
                {
                    if (_defaultType == null)
                    {
                        var attr = _attributes[0];
                        Type requestedType = GetRequestedType(this.Context);
                        _defaultType = _tooling.GetDefaultType(attr, this.Context.Access, requestedType);
                    }
                    return _defaultType;
                }
            }

            public override Collection<Attribute> GetAttributes()
            {
                return new Collection<Attribute>(_attributes);
            }
        }
    }
}
