// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.WebJobs.Script.Description
{
    public class WebAssemblyCompilation : IDotNetCompilation
    {
        private readonly string _assemblyFilePath;
        private readonly string _entryPointName;

        public WebAssemblyCompilation(string assemblyFilePath, string entryPointName)
        {
            _assemblyFilePath = assemblyFilePath;
            _entryPointName = entryPointName;
        }

        public Task<DotNetCompilationResult> EmitAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ImmutableArray<Diagnostic> GetDiagnostics() => ImmutableArray<Diagnostic>.Empty;

        public FunctionSignature GetEntryPointSignature(IFunctionEntryPointResolver entryPointResolver, Assembly functionAssembly)
        {
            throw new NotImplementedException();
        }

        Task<object> ICompilation.EmitAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
