// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Script.Description
{
    public class WebAssemblyCompilationService : ICompilationService<IDotNetCompilation>
    {
        private static readonly string[] _supportedFileTypes = { ".wasm" };

        public string Language => DotNetScriptTypes.WebAssembly;

        public bool PersistsOutput => false;

        public IEnumerable<string> SupportedFileTypes => _supportedFileTypes;

        async Task<object> ICompilationService.GetFunctionCompilationAsync(FunctionMetadata functionMetadata)
            => await GetFunctionCompilationAsync(functionMetadata);

        public Task<IDotNetCompilation> GetFunctionCompilationAsync(FunctionMetadata functionMetadata)
        {
            return Task.FromResult<IDotNetCompilation>(new WebAssemblyCompilation(functionMetadata.ScriptFile, functionMetadata.EntryPoint));
        }
    }
}
