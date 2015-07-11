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
    internal sealed class AsyncCloudDrive : CloudDriveBase, ICloudDrive
    {
        private const int MAX_BULKDOWNLOAD_SIZE = 1 << 29;

        private IAsyncCloudGateway gateway;

        public AsyncCloudDrive(PSDriveInfo driveInfo, RootName rootName, IAsyncCloudGateway gateway, CloudDriveParameters parameters) : base(driveInfo, rootName, parameters)
        {
            this.gateway = gateway;
        }

        protected override DriveInfoContract GetDrive()
        {
            try {
                if (drive == null) {
                    drive = gateway.GetDriveAsync(rootName, apiKey).Result;
                    drive.Name = Name + Path.VolumeSeparatorChar;
                }
                return drive;
            } catch (AggregateException ex) when (ex.InnerExceptions.Count == 1) {
                throw ex.InnerExceptions[0];
            }
        }

        public RootDirectoryInfoContract GetRoot()
        {
            try {
                var root = gateway.GetRootAsync(rootName, apiKey).Result;
                root.Drive = GetDrive();
                return root;
            } catch (AggregateException ex) when (ex.InnerExceptions.Count == 1) {
                throw ex.InnerExceptions[0];
            }
        }

        public IEnumerable<FileSystemInfoContract> GetChildItem(DirectoryInfoContract parent)
        {
            try {
                return gateway.GetChildItemAsync(rootName, parent.Id).Result;
            } catch (AggregateException ex) when (ex.InnerExceptions.Count == 1) {
                throw ex.InnerExceptions[0];
            }
        }

        public void ClearContent(FileInfoContract target)
        {
            try {
                Func<FileSystemInfoLocator> locator = () => new FileSystemInfoLocator(target);
                gateway.ClearContentAsync(rootName, target.Id, locator).Wait();
                target.Size = 0;
            } catch (AggregateException ex) when (ex.InnerExceptions.Count == 1) {
                throw ex.InnerExceptions[0];
            } finally {
                InvalidateDrive();
            }
        }

        public Stream GetContent(FileInfoContract source, ProgressProxy progress)
        {
            try {
                var result = gateway.GetContentAsync(rootName, source.Id).Result;

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
            } catch (AggregateException ex) when (ex.InnerExceptions.Count == 1) {
                throw ex.InnerExceptions[0];
            }
        }

        public void SetContent(FileInfoContract target, Stream content, ProgressProxy progress)
        {
            try {
                if (!string.IsNullOrEmpty(encryptionKey))
                    content = content.Encrypt(encryptionKey);

                Func<FileSystemInfoLocator> locator = () => new FileSystemInfoLocator(target);
                ProgressProxy.ProgressFunc<bool> func = p => gateway.SetContentAsync(rootName, target.Id, content, p, locator);
                ProgressProxy.TraceProgressOn(func, progress);
                target.Size = content.Length;
            } catch (AggregateException ex) when (ex.InnerExceptions.Count == 1) {
                throw ex.InnerExceptions[0];
            } finally {
                InvalidateDrive();
            }
        }

        public FileSystemInfoContract CopyItem(FileSystemInfoContract source, string copyPath, DirectoryInfoContract destination, bool recurse)
        {
            try {
                return gateway.CopyItemAsync(rootName, source.Id, copyPath, destination.Id, recurse).Result;
            } catch (AggregateException ex) when (ex.InnerExceptions.Count == 1) {
                throw ex.InnerExceptions[0];
            } finally {
                InvalidateDrive();
            }
        }

        public FileSystemInfoContract MoveItem(FileSystemInfoContract source, string movePath, DirectoryInfoContract destination)
        {
            try {
                Func<FileSystemInfoLocator> locator = () => new FileSystemInfoLocator(source);
                return gateway.MoveItemAsync(rootName, source.Id, movePath, destination.Id, locator).Result;
            } catch (AggregateException ex) when (ex.InnerExceptions.Count == 1) {
                throw ex.InnerExceptions[0];
            } finally {
                InvalidateDrive();
            }
        }

        public DirectoryInfoContract NewDirectoryItem(DirectoryInfoContract parent, string name)
        {
            try {
                return gateway.NewDirectoryItemAsync(rootName, parent.Id, name).Result;
            } catch (AggregateException ex) when (ex.InnerExceptions.Count == 1) {
                throw ex.InnerExceptions[0];
            }
        }

        public FileInfoContract NewFileItem(DirectoryInfoContract parent, string name, Stream content, ProgressProxy progress)
        {
            try {
                if (content != null && !string.IsNullOrEmpty(encryptionKey))
                    content = content.Encrypt(encryptionKey);

                ProgressProxy.ProgressFunc<FileInfoContract> func = p => gateway.NewFileItemAsync(rootName, parent.Id, name, content, p);
                return ProgressProxy.TraceProgressOn(func, progress);
            } catch (AggregateException ex) when (ex.InnerExceptions.Count == 1) {
                throw ex.InnerExceptions[0];
            } finally {
                InvalidateDrive();
            }
        }

        public void RemoveItem(FileSystemInfoContract target, bool recurse)
        {
            try {
                gateway.RemoveItemAsync(rootName, target.Id, recurse).Wait();
            } catch (AggregateException ex) when (ex.InnerExceptions.Count == 1) {
                throw ex.InnerExceptions[0];
            } finally {
                InvalidateDrive();
            }
        }

        public FileSystemInfoContract RenameItem(FileSystemInfoContract target, string newName)
        {
            try {
                Func<FileSystemInfoLocator> locator = () => new FileSystemInfoLocator(target);
                return gateway.RenameItemAsync(rootName, target.Id, newName, locator).Result;
            } catch (AggregateException ex) when (ex.InnerExceptions.Count == 1) {
                throw ex.InnerExceptions[0];
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Debugger Display")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay => $"{nameof(AsyncCloudDrive)} {Name} ({Root})";
    }
}