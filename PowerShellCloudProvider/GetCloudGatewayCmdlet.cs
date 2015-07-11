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
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Text.RegularExpressions;
using IgorSoft.PowerShellCloudProvider.Interface.Composition;

namespace IgorSoft.PowerShellCloudProvider
{
    [Cmdlet(VerbsCommon.Get, "CloudGateway")]
    [CLSCompliant(false)]
    public class GetCloudGatewayCmdlet : Cmdlet
    {
        public enum GatewayMode
        {
            Any = 0,
            Async,
            Sync
        }

        [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
        public class GatewayInfo
        {
            public string CloudService { get; }

            public Uri ServiceUri { get; }

            public GatewayMode Mode { get; }

            public Version GatewayVersion { get; }

            public AssemblyName Api { get; }

            public GatewayInfo(string cloudService, Uri serviceUri, GatewayMode mode, Version gatewayVersion, AssemblyName api)
            {
                CloudService = cloudService;
                ServiceUri = serviceUri;
                Mode = mode;
                GatewayVersion = gatewayVersion;
                Api = api;
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Used for DebuggerDisplay")]
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
            private string DebuggerDisplay => $"{nameof(GatewayInfo)} {CloudService} Mode={Mode} GatewayVersion={GatewayVersion} Api={Api}";
        }

        [Parameter(Position = 0)]
        public string Name { get; set; }

        [Parameter(Position = 1)]
        public GatewayMode Mode { get; set; }

        [ImportMany]
        internal IEnumerable<ExportFactory<IAsyncCloudGateway, CloudGatewayMetadata>> AsyncGateways { get; set; }

        [ImportMany]
        internal IEnumerable<ExportFactory<ICloudGateway, CloudGatewayMetadata>> SyncGateways { get; set; }

        private GatewayInfo CreateGatewayInfo<TGateway>(ExportFactory<TGateway, CloudGatewayMetadata> export, GatewayMode mode)
        {
            var metadata = export.Metadata;
            return new GatewayInfo(metadata.CloudService, metadata.ServiceUri, mode, export.CreateExport().Value.GetType().Assembly.GetName().Version, metadata.ApiAssembly);
        }

        protected override void BeginProcessing()
        {
            CompositionInitializer.Initialize(CloudProvider.COMPOSITION_DIRECTORY);
            CompositionInitializer.SatisfyImports(this);

            base.BeginProcessing();
        }

        protected override void ProcessRecord()
        {
            var namePattern = !string.IsNullOrEmpty(Name) ? new Regex(Name.Replace('?', '.').Replace("*", ".*")) : null;

            var gateways = new List<GatewayInfo>();

            if (Mode != GatewayMode.Sync)
                gateways.AddRange(AsyncGateways.Where(g => namePattern?.IsMatch(g.Metadata.CloudService) ?? true).Select(g => CreateGatewayInfo(g, GatewayMode.Async)));
            if (Mode != GatewayMode.Async)
                gateways.AddRange(SyncGateways.Where(g => namePattern?.IsMatch(g.Metadata.CloudService) ?? true).Select(g => CreateGatewayInfo(g, GatewayMode.Sync)));

            gateways.Sort((g1, g2) => string.Compare(g1.CloudService, g2.CloudService, StringComparison.Ordinal));

            foreach (var gateway in gateways)
                WriteObject(gateway);
        }
    }
}
