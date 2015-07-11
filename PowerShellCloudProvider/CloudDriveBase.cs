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
using System.Management.Automation;
using CodeOwls.PowerShell.Provider;
using IgorSoft.PowerShellCloudProvider.Interface;
using IgorSoft.PowerShellCloudProvider.Interface.IO;
using IgorSoft.PowerShellCloudProvider.Parameters;

namespace IgorSoft.PowerShellCloudProvider
{
    internal abstract class CloudDriveBase : Drive
    {
        protected RootName rootName;

        protected string apiKey;

        protected string encryptionKey;

        protected DriveInfoContract drive;

        public new string DisplayRoot { get; }

        public long? Free => GetDrive().FreeSpace;

        public long? Used => GetDrive().UsedSpace;

        protected CloudDriveBase(PSDriveInfo driveInfo, RootName rootName, CloudDriveParameters parameters) : base(driveInfo)
        {
            if (driveInfo == null)
                throw new ArgumentNullException(nameof(driveInfo));

            this.rootName = rootName;
            DisplayRoot = rootName.Value;
            if (parameters != null) {
                apiKey = parameters.ApiKey;
                encryptionKey = parameters.EncryptionKey;
            }
            if (string.IsNullOrEmpty(encryptionKey))
                DisplayRoot = DisplayRoot.Insert(0, "*");
        }

        protected abstract DriveInfoContract GetDrive();

        protected void InvalidateDrive()
        {
            drive = null;
        }
    }
}