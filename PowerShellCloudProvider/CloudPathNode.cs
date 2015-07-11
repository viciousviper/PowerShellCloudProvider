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
using System.Management.Automation.Provider;
using System.Text.RegularExpressions;
using CodeOwls.PowerShell.Paths;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using CodeOwls.PowerShell.Provider.PathNodes;
using IgorSoft.PowerShellCloudProvider.Interface.IO;
using IgorSoft.PowerShellCloudProvider.Parameters;

namespace IgorSoft.PowerShellCloudProvider
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal sealed class CloudPathNode : PathNodeBase, INewItem, IRemoveItem, IGetItemContent, ISetItemContent, IClearItemContent, ICopyItem, IMoveItem, IRenameItem
    {
        internal enum Mode
        {
            Directory,
            File
        }

        private Mode itemMode;

        private IPathValue nodeValue;

        private CloudPathNode parent;

        private IList<CloudPathNode> children;

        public object CopyItemParameters => null;

        public override string ItemMode => itemMode.ToString();

        public override string Name => nodeValue.Name;

        public object MoveItemParameters => null;

        public object NewItemParameters => null;

        public IEnumerable<string> NewItemTypeNames
        {
            get {
                yield return Mode.Directory.ToString();
                yield return Mode.File.ToString();
            }
        }

        public object RemoveItemParameters => null;

        public object RenameItemParameters => null;

        public CloudPathNode(FileSystemInfoContract fileSystemInfo)
        {
            var directoryInfo = fileSystemInfo as DirectoryInfoContract;
            if (directoryInfo != null) {
                itemMode = Mode.Directory;
                nodeValue = new ContainerPathValue(directoryInfo, directoryInfo.Name);
                return;
            }

            var fileInfo = fileSystemInfo as FileInfoContract;
            if (fileInfo != null) {
                itemMode = Mode.File;
                nodeValue = new LeafPathValue(fileInfo, fileInfo.Name);
                return;
            }

            throw new InvalidOperationException();
        }

        private bool IsProviderPathNode(IProviderContext providerContext)
        {
            var path = Name;
            for (var parentNode = parent; parentNode != null; parentNode = parentNode.parent)
                path = path.Insert(0, parentNode.parent != null ? parentNode.Name + Path.DirectorySeparatorChar : Path.DirectorySeparatorChar.ToString());
            if (path == "-")
                path = Path.DirectorySeparatorChar.ToString();

            return CloudPathResolver.GetRelativePath(providerContext, providerContext.Path) == path;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "parent")]
        private void SetParent(CloudPathNode parent)
        {
            this.parent = parent;
            var directory = ((DirectoryInfoContract)parent?.nodeValue.Item);

            var fileItem = nodeValue.Item as FileInfoContract;
            if (fileItem != null) {
                fileItem.Directory = directory;
                return;
            }

            var directoryItem = nodeValue.Item as DirectoryInfoContract;
            if (directoryItem != null) {
                directoryItem.Parent = directory;
                return;
            }
        }

        public void ClearContent(IProviderContext providerContext)
        {
            if (itemMode != Mode.File)
                throw new NotSupportedException($"{nameof(ItemMode)} = {ItemMode}");
            if (providerContext == null)
                throw new ArgumentNullException(nameof(providerContext));

            var drive = providerContext.Drive as ICloudDrive;
            if (drive != null) {
                drive.ClearContent((FileInfoContract)nodeValue.Item);
                return;
            }

            throw new InvalidOperationException();
        }

        public object ClearContentDynamicParameters(IProviderContext providerContext) => null;

        public IContentReader GetContentReader(IProviderContext providerContext)
        {
            if (providerContext == null)
                throw new ArgumentNullException(nameof(providerContext));
            if (itemMode != Mode.File)
                throw new NotSupportedException($"{nameof(ItemMode)} = {ItemMode}");

            var drive = providerContext.Drive as ICloudDrive;
            if (drive != null) {
                var fileInfo = (FileInfoContract)nodeValue.Item;
                return new CloudContentReaderWriter(drive.GetContent(fileInfo, new ProgressProxy(providerContext, "Get-Content", fileInfo.Name)), providerContext.DynamicParameters as CloudContentReaderWriterParameters);
            }

            throw new InvalidOperationException();
        }

        public object GetContentReaderDynamicParameters(IProviderContext providerContext) => new CloudContentReaderWriterParameters();

        public IContentWriter GetContentWriter(IProviderContext providerContext)
        {
            if (providerContext == null)
                throw new ArgumentNullException(nameof(providerContext));
            if (itemMode != Mode.File)
                throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, "ItemMode = {0}", ItemMode));

            var drive = providerContext.Drive as ICloudDrive;
            if (drive != null) {
                var fileInfo = (FileInfoContract)nodeValue.Item;
                return new CloudContentReaderWriter(s => drive.SetContent(fileInfo, s, new ProgressProxy(providerContext, "Set-Content", fileInfo.Name)), providerContext.DynamicParameters as CloudContentReaderWriterParameters);
            }

            throw new InvalidOperationException();
        }

        public object GetContentWriterDynamicParameters(IProviderContext providerContext) => new CloudContentReaderWriterParameters();

        public override IEnumerable<IPathNode> GetNodeChildren(IProviderContext providerContext)
        {
            if (providerContext == null)
                throw new ArgumentNullException(nameof(providerContext));

            if (!nodeValue.IsCollection)
                return null;

            if (children == null || providerContext.Force && IsProviderPathNode(providerContext)) {
                children = (((ICloudDrive)providerContext.Drive).GetChildItem((DirectoryInfoContract)nodeValue.Item).Select(f => new CloudPathNode(f))).ToList();
                foreach (var child in children)
                    child.SetParent(this);
            }

            if (!string.IsNullOrEmpty(providerContext.Filter) && !providerContext.ResolveFinalNodeFilterItems) {
                var regex = new Regex("^" + providerContext.Filter.Replace("*", ".*").Replace('?', '.') + "$");
                return children.Where(i => regex.IsMatch(i.Name));
            }

            return children;
        }

        public override IPathValue GetNodeValue() => nodeValue;

        public IPathValue CopyItem(IProviderContext providerContext, string path, string copyPath, IPathValue destinationContainer, bool recurse)
        {
            if (providerContext == null)
                throw new ArgumentNullException(nameof(providerContext));
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            if (destinationContainer == null)
                throw new ArgumentNullException(nameof(destinationContainer));

            var drive = providerContext.Drive as ICloudDrive;
            if (drive != null) {
                var copyItem = new CloudPathNode(drive.CopyItem((FileSystemInfoContract)nodeValue.Item, copyPath, (DirectoryInfoContract)destinationContainer.Item, recurse));
                var destinationContainerNode = providerContext.ResolvePath(((DirectoryInfoContract)destinationContainer.Item).Id.Value) as CloudPathNode;
                destinationContainerNode.children.Add(copyItem);
                copyItem.SetParent(destinationContainerNode);
                return copyItem.nodeValue;
            }

            throw new InvalidOperationException();
        }

        public IPathValue MoveItem(IProviderContext providerContext, string path, string movePath, IPathValue destinationContainer)
        {
            if (providerContext == null)
                throw new ArgumentNullException(nameof(providerContext));
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            if (destinationContainer == null)
                throw new ArgumentNullException(nameof(destinationContainer));

            var drive = providerContext.Drive as ICloudDrive;
            if (drive != null) {
                var moveItem = new CloudPathNode(drive.MoveItem((FileSystemInfoContract)nodeValue.Item, movePath, (DirectoryInfoContract)destinationContainer.Item));
                var destinationContainerNode = providerContext.ResolvePath(((DirectoryInfoContract)destinationContainer.Item).Id.Value) as CloudPathNode;
                destinationContainerNode.children.Add(moveItem);
                moveItem.SetParent(destinationContainerNode);
                parent.children.Remove(this);
                SetParent(null);
                return moveItem.nodeValue;
            }

            throw new InvalidOperationException();
        }

        public IPathValue NewItem(IProviderContext providerContext, string path, string itemTypeName, object newItemValue)
        {
            if (providerContext == null)
                throw new ArgumentNullException(nameof(providerContext));
            if (path == null)
                throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, Resources.ItemAlreadyPresent, providerContext.Path));
            if (itemMode != Mode.Directory)
                throw new NotSupportedException($"{nameof(ItemMode)} = {ItemMode}");
            Mode mode;
            if (!Enum.TryParse(itemTypeName, out mode))
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.UnknownItemType, itemTypeName), nameof(providerContext));

            var drive = providerContext.Drive as ICloudDrive;
            if (drive != null) {
                var baseDirectory = (DirectoryInfoContract)nodeValue.Item;
                CloudPathNode newItem = null;
                switch (mode) {
                    case Mode.Directory:
                        newItem = new CloudPathNode(drive.NewDirectoryItem(baseDirectory, path));
                        newItem.children = new List<CloudPathNode>();
                        break;
                    case Mode.File:
                        Stream stream = null;

                        if ((stream = newItemValue as Stream) != null) {
                            // nothing else to to
                        } else {
                            var newItemByteArray = newItemValue as byte[];
                            if (newItemByteArray != null) {
                                stream = new MemoryStream(newItemByteArray);
                            } else {
                                var newItemString = newItemValue as string;
                                if (newItemString != null) {
                                    stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(newItemString));
                                } else if (newItemValue != null) {
                                    throw new NotSupportedException(string.Format(CultureInfo.CurrentCulture, Resources.UnsupportedContentType, newItemValue.GetType().Name));
                                }
                            }
                        }

                        newItem = new CloudPathNode(drive.NewFileItem(baseDirectory, path, stream, new ProgressProxy(providerContext, "New-Item", path)));
                        break;
                }
                children.Add(newItem);
                newItem.SetParent(this);
                return newItem.nodeValue;
            }

            throw new InvalidOperationException();
        }

        public void RemoveItem(IProviderContext providerContext, string path, bool recurse)
        {
            if (providerContext == null)
                throw new ArgumentNullException(nameof(providerContext));

            var drive = providerContext.Drive as ICloudDrive;
            if (drive != null) {
                parent.children.Remove(this);
                drive.RemoveItem((FileSystemInfoContract)nodeValue.Item, recurse);
                SetParent(null);
                return;
            }

            throw new InvalidOperationException();
        }

        public void RenameItem(IProviderContext providerContext, string path, string newName)
        {
            if (providerContext == null)
                throw new ArgumentNullException(nameof(providerContext));

            var drive = providerContext.Drive as ICloudDrive;
            if (drive != null) {
                var fileSystemInfo = (FileSystemInfoContract)nodeValue.Item;
                fileSystemInfo = drive.RenameItem(fileSystemInfo, newName);
                if (itemMode == Mode.Directory) {
                    nodeValue = new ContainerPathValue(fileSystemInfo, fileSystemInfo.Name);
                    return;
                } else if (itemMode == Mode.File) {
                    nodeValue = new LeafPathValue(fileSystemInfo, fileSystemInfo.Name);
                    return;
                }
            }

            throw new InvalidOperationException();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Used for DebuggerDisplay")]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private string DebuggerDisplay => $"{ItemMode} {Name}";
    }
}