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
using System.Globalization;
using System.IO;
using System.Management.Automation;
using CodeOwls.PowerShell.Provider;
using IgorSoft.PowerShellCloudProvider.Interface;
using IgorSoft.PowerShellCloudProvider.Interface.Composition;
using IgorSoft.PowerShellCloudProvider.Parameters;

namespace IgorSoft.PowerShellCloudProvider
{
    internal sealed class CloudDriveFactory
    {
        [Import]
        internal IGatewayManager GatewayManager { get; set; }

        internal Drive CreateCloudDrive(PSDriveInfo driveInfo, CloudDriveParameters parameters)
        {
            var rootName = CreateRootName(driveInfo);

            var asyncGateway = default(IAsyncCloudGateway);
            if (GatewayManager.TryGetAsyncCloudGatewayForSchema(rootName.Schema, out asyncGateway))
                return new AsyncCloudDrive(CreateDriveInfo(driveInfo), rootName, asyncGateway, parameters);

            var gateway = default(ICloudGateway);
            if (GatewayManager.TryGetCloudGatewayForSchema(rootName.Schema, out gateway))
                return new CloudDrive(CreateDriveInfo(driveInfo), rootName, gateway, parameters);

            throw new KeyNotFoundException(string.Format(CultureInfo.CurrentCulture, Resources.NoGatewayForSchema, rootName.Schema));
        }

        private static RootName CreateRootName(PSDriveInfo driveInfo)
        {
            var rootComponents = driveInfo.Root.Split('|');

            return new RootName(rootComponents[0], driveInfo.Credential.UserName, rootComponents.Length > 1 ? rootComponents[1] : null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Language", "CSE0003:Use expression-bodied members")]
        private static PSDriveInfo CreateDriveInfo(PSDriveInfo driveInfo)
        {
            return new PSDriveInfo(driveInfo.Name, driveInfo.Provider, driveInfo.Name + Path.VolumeSeparatorChar, driveInfo.Description, driveInfo.Credential);
        }
    }
}
