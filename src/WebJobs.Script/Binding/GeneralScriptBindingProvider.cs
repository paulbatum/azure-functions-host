﻿// Copyright (c) .NET Foundation. All rights reserved.
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
    // This single binder can service all SDK extensions by leveraging the SDK metadata provider. 
    public class GeneralScriptBindingProvider : ScriptBindingProvider
    {        
        private IJobHostMetadataProvider _tooling;
                
        public GeneralScriptBindingProvider(
            JobHostConfiguration config, 
            JObject hostMetadata, 
            TraceWriter traceWriter) :
            base(config, hostMetadata, traceWriter)
        {
        }

        // The constructor is fixed and ScriptBindingProvider are instantated for us by the Script runtime. 
        // Extensions may get registered after this class is instantiated. 
        // So we need a final call that lets us get the tooling snapshot of the graph after all extensions are set. 
        public void FinishInit()
        {
            this._tooling  = this.Config.GetTooling();
        }

        public override bool TryCreate(ScriptBindingContext context, out ScriptBinding binding)
        {
            string name = context.Type;
            var attrType = this._tooling.GetAttributeTypeFromName(name);
            if (attrType == null)
            {
                binding = null;
                return false;
            }

            var attr = this._tooling.GetAttribute(attrType, context.Metadata);
                        
            binding = new GeneralScriptBinding(this._tooling, attr, context);
            return true;
        }

        public override bool TryResolveAssembly(string assemblyName, out Assembly assembly)
        {
            return this._tooling.TryResolveAssembly(assemblyName, out assembly);
        }


        // Function.json specifies a type via optional DataType and Cardinality properties. 
        // Read the properties and convert that into a System.Type. 
        static Type GetRequestedType(ScriptBindingContext context)
        {
            Type type = ParseDataType(context);

            if (type == null)
            {
                // No type information specified. 
                return null;
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

        private class GeneralScriptBinding : ScriptBinding, IResultProcessingBinding
        {
            private readonly Attribute _attribute;
            private readonly IJobHostMetadataProvider _tooling;

            private Type _defaultType;

            private MethodInfo _applyReturn; // Action<object,object>

            public GeneralScriptBinding(IJobHostMetadataProvider tooling, Attribute attribute, ScriptBindingContext context)
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

            public override Collection<Attribute> GetAttributes() => new Collection<Attribute> { _attribute } ;            
        }
    }
}
