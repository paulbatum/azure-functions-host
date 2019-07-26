// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.Binding;
using Microsoft.Azure.WebJobs.Script.Diagnostics;
using Microsoft.Azure.WebJobs.Script.Extensibility;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Script.Description
{
    public class WebAssemblyInvoker : DotNetFunctionInvoker
    {
        internal WebAssemblyInvoker(ScriptHost host, FunctionMetadata functionMetadata, Collection<FunctionBinding> inputBindings, Collection<FunctionBinding> outputBindings, IFunctionEntryPointResolver functionEntryPointResolver, ICompilationServiceFactory<ICompilationService<IDotNetCompilation>, IFunctionMetadataResolver> compilationServiceFactory, ILoggerFactory loggerFactory, IMetricsLogger metricsLogger, ICollection<IScriptBindingProvider> bindingProviders, IFunctionMetadataResolver metadataResolver = null) : base(host, functionMetadata, inputBindings, outputBindings, functionEntryPointResolver, compilationServiceFactory, loggerFactory, metricsLogger, bindingProviders, metadataResolver)
        {
        }

        internal override Task<MethodInfo> GetFunctionTargetAsync(bool isInvocation = false)
        {
            return base.GetFunctionTargetAsync(isInvocation);
        }

        protected override MethodInfo ResolveEntryPoint(FunctionSignature functionSignature, Assembly targetAssembly)
        {
            return base.ResolveEntryPoint(functionSignature, typeof(WebAssemblyProxy).Assembly);
        }
    }
}
