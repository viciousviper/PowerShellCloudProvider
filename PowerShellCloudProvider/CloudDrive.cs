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
using System.IO;
using System.Management.Automation;
using IgorSoft.PowerShellCloudProvider.Interface;
using IgorSoft.PowerShellCloudProvider.Interface.Composition;
using IgorSoft.PowerShellCloudProvider.Interface.IO;
using IgorSoft.PowerShellCloudProvider.Parameters;

namespace IgorSoft.PowerShellCloudProvider
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal sealed class CloudDrive : CloudDriveBase, ICloudDrive
    {
        private const int MAX_BULKDOWNLOAD_SIZE = 1 << 29;

        private ICloudGateway gateway;

        public CloudDrive(PSDriveInfo driveInfo, RootName rootName, ICloudGateway gateway, CloudDriveParameters parameters) : base(driveInfo, rootName, parameters)
        {
            this.gateway = gateway;
        }

        protected override DriveInfoContract GetDrive()
        {
            if (drive == null) {
                drive = gateway.GetDrive(rootName, apiKey);
                drive.Name = Name + Path.VolumeSeparatorChar;
            }
            return drive;
        }

        public RootDirectoryInfoContract GetRoot()
        {
            var root = gateway.GetRoot(rootName, apiKey);
            root.Drive = GetDrive();
            return root;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Language", "CSE0003:Use expression-bodied members")]
        public IEnumerable<FileSystemInfoContract> GetChildItem(DirectoryInfoContract parent)
        {
            return gateway.GetChildItem(rootName, parent.Id);
        }

        public void ClearContent(FileInfoContract target)
        {
            gateway.ClearContent(rootName, target.Id);
            target.Size = 0;

            InvalidateDrive();
        }

        public Stream GetContent(FileInfoContract source, ProgressProxy progress)
        {
            var result = gateway.GetContent(rootName, source.Id);

            if (!result.CanSeek) {
                var bufferStream = new MemoryStream();
                result.CopyTo(bufferStream, MAX_BULKDOWNLOAD_SIZE);
                bufferStream.Seek(0, SeekOrigin.Begin);
                result.Dispose();
                result = bufferStream;
            }

            result = new ProgressStream(result, progress);

            if (!string.IsNullOrEmpty(encryptionKey))
                result = result.Decrypt(encryptionKey);

            return result;
        }

        public void SetContent(FileInfoContract target, Stream content, ProgressProxy progress)
        {
            if (!string.IsNullOrEmpty(encryptionKey))
                content = content.Encrypt(encryptionKey);

            gateway.SetContent(rootName, target.Id, content, progress);
            target.Size = content.Length;

            InvalidateDrive();
        }

        public FileSystemInfoContract CopyItem(FileSystemInfoContract source, string copyPath, DirectoryInfoContract destination, bool recurse)
        {
            InvalidateDrive();

            return gateway.CopyItem(rootName, source.Id, copyPath, destination.Id, recurse);
        }

        public FileSystemInfoContract MoveItem(FileSystemInfoContract source, string movePath, DirectoryInfoContract destination)
        {
            InvalidateDrive();

            return gateway.MoveItem(rootName, source.Id, movePath, destination.Id);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Language", "CSE0003:Use expression-bodied members")]
        public DirectoryInfoContract NewDirectoryItem(DirectoryInfoContract parent, string name)
        {
            return gateway.NewDirectoryItem(rootName, parent.Id, name);
        }

        public FileInfoContract NewFileItem(DirectoryInfoContract parent, string name, Stream content, ProgressProxy progress)
        {
            if (!string.IsNullOrEmpty(encryptionKey))
                content = content.Encrypt(encryptionKey);

            InvalidateDrive();

            return gateway.NewFileItem(rootName, parent.Id, name, content, progress);
        }

        public void RemoveItem(FileSystemInfoContract target, bool recurse)
        {
            gateway.RemoveItem(rootName, target.Id, recurse);

            InvalidateDrive();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Language", "CSE0003:Use expression-bodied members")]
        public FileSystemInfoContract RenameItem(FileSystemInfoContract target, string newName)
        {
            return gateway.RenameItem(rootName, target.Id, newName);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay => $"{nameof(CloudDrive)} {Name} ({Root})";
    }
}