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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Text.RegularExpressions;
using CodeOwls.PowerShell.Paths.Processors;
using CodeOwls.PowerShell.Provider;
using IgorSoft.PowerShellCloudProvider.Interface.IO;
using IgorSoft.PowerShellCloudProvider.Parameters;

namespace IgorSoft.PowerShellCloudProvider
{
    /// <summary>
    /// Provider for custom filesystems
    /// </summary>
    [CmdletProvider(PROVIDER_NAME, ProviderCapabilities.Credentials | ProviderCapabilities.ExpandWildcards | ProviderCapabilities.Filter | ProviderCapabilities.ShouldProcess)]
    public sealed class CloudProvider : Provider
    {
        internal const string PROVIDER_NAME = "CloudFileSystem";

        internal const string COMPOSITION_DIRECTORY = "Gateways";

        private static IDictionary<PSDriveInfo, IPathResolver> pathResolverCache = new Dictionary<PSDriveInfo, IPathResolver>();

        private static IPathResolver defaultResolver;

        protected override IPathResolver PathResolver
        {
            get {
                if (PSDriveInfo == null)
                    return defaultResolver ?? (defaultResolver = new DelegatingPathResolver(pathResolverCache));

                IPathResolver result = null;
                if (!pathResolverCache.TryGetValue(PSDriveInfo, out result))
                    pathResolverCache.Add(PSDriveInfo, result = new CloudPathResolver((ICloudDrive)PSDriveInfo));
                return result;
            }
        }

        private bool CreateIntermediateDirectories(string path)
        {
            string parentPath = GetParentPath(path, PSDriveInfo.Root);
            if (ItemExists(parentPath))
                return true;

            NewItem(parentPath, CloudPathNode.Mode.Directory.ToString(), null);
            return ItemExists(parentPath);
        }

        protected override string[] ExpandPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            int index = path.LastIndexOf(Path.DirectorySeparatorChar);
            var basePath = index >= 0 ? path.Substring(0, index) : path;

            var pathNode = PathResolver.ResolvePath(CreateContext(basePath), basePath)?.Single();
            if (pathNode == null)
                throw new ItemNotFoundException(basePath);
            var baseDirectory = (DirectoryInfoContract)pathNode.GetNodeValue().Item;
            var filter = index >= 0 ? new Regex("^" + path.Substring(index + 1).Replace("*", ".*").Replace('?', '.') + "$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) : null;

            var matches = ((ICloudDrive)PSDriveInfo).GetChildItem(baseDirectory);
            return matches.Where(i => filter == null || filter.IsMatch(i.Name)).Select(i => string.Concat(basePath, Path.DirectorySeparatorChar, i.Name)).ToArray();
        }

        protected override PSDriveInfo NewDrive(PSDriveInfo drive)
        {
            if (drive == null)
                throw new ArgumentNullException(nameof(drive));

            var factory = new CloudDriveFactory();
            CompositionInitializer.SatisfyImports(factory);

            var parameters = DynamicParameters as CloudDriveParameters;
            if (string.IsNullOrEmpty(parameters?.EncryptionKey))
                WriteWarning(string.Format(CultureInfo.CurrentCulture, Resources.UnencryptedDrive, drive.Name, drive.Credential.UserName, drive.Root));

            return base.NewDrive(factory.CreateCloudDrive(drive, parameters));
        }

        protected override object NewDriveDynamicParameters() => new CloudDriveParameters();

        protected override void NewItem(string path, string itemTypeName, object newItemValue)
        {
            if (Force && !CreateIntermediateDirectories(path))
                return;

            base.NewItem(path, itemTypeName, newItemValue);
        }

        protected override PSDriveInfo RemoveDrive(PSDriveInfo drive)
        {
            if (pathResolverCache.ContainsKey(drive))
                pathResolverCache.Remove(drive);
            return base.RemoveDrive(drive);
        }

        protected override ProviderInfo Start(ProviderInfo providerInfo)
        {
            Environment.CurrentDirectory = SessionState.Path.CurrentFileSystemLocation.Path;

            CompositionInitializer.Preload(typeof(Interface.Composition.ICloudGateway));
            CompositionInitializer.Initialize(COMPOSITION_DIRECTORY);

            return base.Start(providerInfo);
        }
    }
}