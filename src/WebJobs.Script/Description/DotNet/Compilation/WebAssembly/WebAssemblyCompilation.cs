// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Wasm2cilWrapper;

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

        public ImmutableArray<Diagnostic> GetDiagnostics() => ImmutableArray<Diagnostic>.Empty;

        async Task<object> ICompilation.EmitAsync(CancellationToken cancellationToken)
            => await EmitAsync(cancellationToken);

        public Task<DotNetCompilationResult> EmitAsync(CancellationToken cancellationToken)
        {
            var bytes = WasmWrapper.compile(File.ReadAllBytes(_assemblyFilePath));
            return Task.FromResult(DotNetCompilationResult.FromBytes(bytes));
        }

        public FunctionSignature GetEntryPointSignature(IFunctionEntryPointResolver entryPointResolver, Assembly functionAssembly)
        {
            //Type functionType = functionAssembly.GetType("test.foo") ?? throw new InvalidOperationException($"type not found");
            //MethodInfo method = functionType.GetMethod(_entryPointName, BindingFlags.Static | BindingFlags.Public) ?? throw new InvalidOperationException($"method not found");

            Type functionType = functionAssembly.GetType("test.foo") ?? throw new InvalidOperationException($"type not found");
            MethodInfo webAssemblyMethod = functionType.GetMethod(_entryPointName, BindingFlags.Static | BindingFlags.Public) ?? throw new InvalidOperationException($"method not found");
            WebAssemblyProxy.Target = webAssemblyMethod;

            var initializer = functionType?.GetMethod("__wasm_call_ctors") ?? throw new Exception("Didn't find initializer");
            initializer.Invoke(null, null);

            var proxyMethod = typeof(WebAssemblyProxy).GetMethod(nameof(WebAssemblyProxy.InvokeProxy), BindingFlags.Static | BindingFlags.Public);
            IEnumerable<FunctionParameter> functionParameters = proxyMethod.GetParameters().Select(p => new FunctionParameter(p.Name, p.ParameterType.FullName, p.IsOptional, GetParameterRefKind(p)));
            return new FunctionSignature(proxyMethod.ReflectedType.FullName, proxyMethod.Name, ImmutableArray.CreateRange(functionParameters.ToArray()), proxyMethod.ReturnType.Name, hasLocalTypeReference: false);
        }

        private static RefKind GetParameterRefKind(ParameterInfo parameter)
        {
            if (parameter.IsOut)
            {
                return RefKind.Out;
            }

            return RefKind.None;
        }
    }

    public class WebAssemblyProxy
    {
        public static MethodInfo Target { get; set; }

        public static void InvokeProxy(string stringinput, out string stringoutput)
        {
            var chars = stringinput.ToCharArray();
            var nlen = Encoding.UTF8.GetByteCount(chars) + 1;
            var byteArray = new byte[nlen];
            nlen = Encoding.UTF8.GetBytes(chars, 0, chars.Length, byteArray, 0);
            byteArray[nlen] = 0;

            Marshal.Copy(byteArray, 0, wasi_unstable.__mem, byteArray.Length);

            var outputOffset = 1024;

            Target.Invoke(null, new object[] { 0, outputOffset });

            var reversedBytes = new byte[nlen];
            Marshal.Copy(wasi_unstable.__mem + outputOffset, reversedBytes, 0, reversedBytes.Length);
            stringoutput = Encoding.UTF8.GetString(reversedBytes);
        }
    }
}
