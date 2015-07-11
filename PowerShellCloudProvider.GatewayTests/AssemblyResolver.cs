/*
The MIT License(MIT)

Copyright(c) 2015 IgorSoft

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Composition;
using System.Reflection;
using System.Text.RegularExpressions;

namespace IgorSoft.PowerShellCloudProvider.GatewayTests
{
    internal class AssemblyResolver
    {
        private const string ASSEMBLY_NAME_REGEX = @"^(?<Name>[\w\.]+), Version=(?<Version>[0-9\.]+), Culture=(?<Culture>\w+), PublicKeyToken=(?<PublicKeyToken>[0-9a-f]{16})$";

        private readonly Regex regex = new Regex(ASSEMBLY_NAME_REGEX, RegexOptions.Compiled);

        private static AssemblyResolver instance;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        [ImportMany]
        public IEnumerable<Func<string, Assembly>> BindingRedirects { get; set; }

        private AssemblyResolver()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            CompositionInitializer.HostInitialized += (s, e) => CompositionInitializer.SatisfyImports(this);
        }

        public static void Initialize()
        {
            if (instance == null)
                instance = new AssemblyResolver();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase")]
        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (BindingRedirects != null)
                foreach (var redirect in BindingRedirects) {
                    var assembly = redirect(args.Name);
                    if (assembly != null) {
                        var assemblyName = assembly.GetName();
                        var match = regex.Match(args.Name);
                        if (match.Success
                                && match.Groups["Name"].Value == assemblyName.Name
                                && match.Groups["PublicKeyToken"].Value == BitConverter.ToString(assemblyName.GetPublicKeyToken()).Replace("-", string.Empty).ToLowerInvariant())
                            return assembly;
                    }
                }

            return null;
        }
    }
}
