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
using System.Linq;
using IgorSoft.PowerShellCloudProvider.Interface.IO;
using IgorSoft.PowerShellCloudProvider.Tests.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace IgorSoft.PowerShellCloudProvider.Tests.Provider
{
    [TestClass]
    public class MoveItemAsyncTests
    {
        private FileSystemFixture fileSystemFixture;

        private CompositionFixture compositionFixture;

        private MockingFixture mockingFixture;

        [TestInitialize]
        public void Initialize()
        {
            fileSystemFixture = new FileSystemFixture();
            compositionFixture = new CompositionFixture();
            mockingFixture = new MockingFixture();
        }

        [TestCleanup]
        public void Cleanup()
        {
            mockingFixture = null;
            compositionFixture = null;
            fileSystemFixture = null;
        }

        [TestMethod]
        public void MoveItemAsync_WherePathIsFile_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;

            var original = (FileInfoContract)rootDirectoryItems.Single(i => i.Name == "File.ext");

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.MoveItemAsync(rootName, new FileId(@"\File.ext"), @"FileMove.ext", new DirectoryId(@"\"), It.IsAny<Func<FileSystemInfoLocator>>()))
                .ReturnsAsync(new FileInfoContract(original.Id.Value.Replace("File", "FileMove"), original.Name.Replace("File", "FileMove"), original.Created, original.Updated, original.Size, null))
                .Verifiable();
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Move-Item -Path X:\File.ext -Destination X:\FileMove.ext"
            );

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void MoveItemAsync_WherePathIsFile_MovesFile()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;

            var original = (FileInfoContract)rootDirectoryItems.Single(i => i.Name == "File.ext");
            var moved = default(FileInfoContract);

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.MoveItemAsync(rootName, new FileId(@"\File.ext"), @"FileMove.ext", new DirectoryId(@"\"), It.IsAny<Func<FileSystemInfoLocator>>()))
                .ReturnsAsync(moved = new FileInfoContract(original.Id.Value.Replace("File", "FileMove"), original.Name.Replace("File", "FileMove"), original.Created, original.Updated, original.Size, original.Hash))
                .Verifiable();
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Move-Item -Path X:\File.ext -Destination X:\FileMove.ext",
                @"Get-ChildItem -Path X:\"
            );

            Assert.AreEqual(rootDirectoryItems.Count(), result.Count, "Unexpected number of results");
            CollectionAssert.DoesNotContain(result.Select(p => p.BaseObject).ToArray(), original, "Original item remains");
            CollectionAssert.Contains(result.Select(p => p.BaseObject).ToArray(), moved, "Copied item is missing");

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void MoveItemAsync_WherePathIsUnknownFile_Throws()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\", rootDirectoryItems);
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            try {
                new PipelineFixture().Invoke(
                    FileSystemFixture.NewDriveCommand,
                    @"Move-Item -Path X:\FileUnknown -Destination X:\FileMove"
                );

                throw new InvalidOperationException("Expected Exception was not thrown");
            } catch (AssertFailedException ex) {
                Assert.IsTrue(ex.Message.EndsWith("InvalidOperation: (:) [Move-Item], PSInvalidOperationException", StringComparison.Ordinal));
            }
        }

        [TestMethod]
        public void MoveItemAsync_WherePathIsFileInSubDirectory_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;

            var original = (FileInfoContract)subDirectoryItems.Single(i => i.Name == "SubFile.ext");

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\SubDir", rootDirectoryItems, subDirectoryItems);
            gatewayMock.Setup(g => g.MoveItemAsync(rootName, new FileId(@"\SubDir\SubFile.ext"), @"SubFileMove.ext", new DirectoryId(@"\SubDir"), It.IsAny<Func<FileSystemInfoLocator>>()))
                .ReturnsAsync(new FileInfoContract(original.Id.Value.Replace("SubFile", "SubFileMove"), original.Name.Replace("SubFile", "SubFileMove"), original.Created, original.Updated, original.Size, original.Hash))
                .Verifiable();
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Move-Item -Path X:\SubDir\SubFile.ext -Destination X:\SubDir\SubFileMove.ext"
            );

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void MoveItemAsync_WherePathIsFileInSubDirectory_MovesFile()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;

            var original = (FileInfoContract)subDirectoryItems.Single(i => i.Name == "SubFile.ext");
            var moved = default(FileInfoContract);

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\SubDir", rootDirectoryItems, subDirectoryItems);
            gatewayMock.Setup(g => g.MoveItemAsync(rootName, new FileId(@"\SubDir\SubFile.ext"), @"SubFileMove.ext", new DirectoryId(@"\SubDir"), It.IsAny<Func<FileSystemInfoLocator>>()))
                .ReturnsAsync(moved = new FileInfoContract(original.Id.Value.Replace("SubFile", "SubFileMove"), original.Name.Replace("SubFile", "SubFileMove"), original.Created, original.Updated, original.Size, original.Hash))
                .Verifiable();
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Move-Item -Path X:\SubDir\SubFile.ext -Destination X:\SubDir\SubFileMove.ext",
                @"Get-ChildItem -Path X:\SubDir"
            );

            Assert.AreEqual(subDirectoryItems.Count(), result.Count, "Unexpected number of results");
            CollectionAssert.DoesNotContain(result.Select(p => p.BaseObject).ToArray(), original, "Original item remains");
            CollectionAssert.Contains(result.Select(p => p.BaseObject).ToArray(), moved, "Moved item is missing");

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void MoveItemAsync_WherePathIsFileAndDestinationDirectoryIsDifferent_MovesFile()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;

            var original = (FileInfoContract)subDirectoryItems.Single(i => i.Name == "SubFile.ext");
            var moved = default(FileInfoContract);

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\SubDir", rootDirectoryItems, subDirectoryItems);
            gatewayMock.Setup(g => g.MoveItemAsync(rootName, new FileId(@"\File.ext"), @"FileMove.ext", new DirectoryId(@"\SubDir"), It.IsAny<Func<FileSystemInfoLocator>>()))
                .ReturnsAsync(moved = new FileInfoContract(original.Id.Value.Replace("File", "FileMove"), original.Name.Replace("File", "FileMove"), original.Created, original.Updated, original.Size, original.Hash))
                .Verifiable();
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Move-Item -Path X:\File.ext -Destination X:\SubDir\FileMove.ext",
                @"Get-ChildItem -Path X:\SubDir"
            );

            Assert.AreEqual(subDirectoryItems.Count() + 1, result.Count, "Unexpected number of results");
            CollectionAssert.Contains(result.Select(p => p.BaseObject).ToArray(), moved, "Copied item is missing");

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void MoveItemAsync_WherePathIsDirectory_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;

            var original = (DirectoryInfoContract)rootDirectoryItems.Single(i => i.Name == "SubDir");

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.MoveItemAsync(rootName, new DirectoryId(@"\SubDir"), "SubDirMove", new DirectoryId(@"\"), It.IsAny<Func<FileSystemInfoLocator>>()))
                .ReturnsAsync(new DirectoryInfoContract(original.Id.Value.Replace("SubDir", "SubDirMove"), original.Name.Replace("SubDir", "SubDirMove"), original.Created, original.Updated))
                .Verifiable();
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Move-Item -Path X:\SubDir -Destination X:\SubDirMove"
            );

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void MoveItemAsync_WherePathIsDirectory_MovesDirectory()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;

            var original = (DirectoryInfoContract)rootDirectoryItems.Single(i => i.Name == "SubDir");
            var renamed = default(DirectoryInfoContract);

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.MoveItemAsync(rootName, new DirectoryId(@"\SubDir"), "SubDirMove", new DirectoryId(@"\"), It.IsAny<Func<FileSystemInfoLocator>>()))
                .ReturnsAsync(renamed = new DirectoryInfoContract(original.Id.Value.Replace("SubDir", "SubDirMove"), original.Name.Replace("SubDir", "SubDirMove"), original.Created, original.Updated))
                .Verifiable();
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Move-Item -Path X:\SubDir -Destination X:\SubDirMove",
                @"Get-ChildItem -Path X:\"
            );

            Assert.AreEqual(rootDirectoryItems.Count(), result.Count, "Unexpected number of results");
            CollectionAssert.DoesNotContain(result.Select(p => p.BaseObject).ToArray(), original, "Original item remains");
            CollectionAssert.Contains(result.Select(p => p.BaseObject).ToArray(), renamed, "Moved item is missing");

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void MoveItemAsync_WherePathIsUnknownDirectory_Throws()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\", rootDirectoryItems);
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            try {
                new PipelineFixture().Invoke(
                    FileSystemFixture.NewDriveCommand,
                    @"Move-Item -Path X:\SubDirUnknown -Destination X:\SubDirMove"
                );

                throw new InvalidOperationException("Expected Exception was not thrown");
            } catch (AssertFailedException ex) {
                Assert.IsTrue(ex.Message.EndsWith(@"InvalidOperation: (:) [Move-Item], PSInvalidOperationException", StringComparison.Ordinal));
            }
        }

        [TestMethod]
        public void MoveItemAsync_WherePathIsDirectoryInSubDirectory_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;

            var original = (DirectoryInfoContract)subDirectoryItems.Single(i => i.Name == "SubSubDir");

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\SubDir", rootDirectoryItems, subDirectoryItems);
            gatewayMock.Setup(g => g.MoveItemAsync(rootName, new DirectoryId(@"\SubDir\SubSubDir"), "SubSubDirMove", new DirectoryId(@"\SubDir"), It.IsAny<Func<FileSystemInfoLocator>>()))
                .ReturnsAsync(new DirectoryInfoContract(original.Id.Value.Replace("SubSubDir", "SubSubDirMove"), original.Name.Replace("SubSubDir", "SubSubDirMove"), original.Created, original.Updated))
                .Verifiable();
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Move-Item -Path X:\SubDir\SubSubDir -Destination X:\SubDir\SubSubDirMove"
            );

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void MoveItemAsync_WherePathIsDirectoryInSubDirectory_CopiesDirectory()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;

            var original = (DirectoryInfoContract)subDirectoryItems.Single(i => i.Name == "SubSubDir");
            var moved = default(DirectoryInfoContract);

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\SubDir", rootDirectoryItems, subDirectoryItems);
            gatewayMock.Setup(g => g.MoveItemAsync(rootName, new DirectoryId(@"\SubDir\SubSubDir"), "SubSubDirMove", new DirectoryId(@"\SubDir"), It.IsAny<Func<FileSystemInfoLocator>>()))
                .ReturnsAsync(moved = new DirectoryInfoContract(original.Id.Value.Replace("SubSubDir", "SubSubDirMove"), original.Name.Replace("SubSubDir", "SubSubDirMove"), original.Created, original.Updated))
                .Verifiable();
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Move-Item -Path X:\SubDir\SubSubDir -Destination X:\SubDir\SubSubDirMove",
                @"Get-ChildItem -Path X:\SubDir"
            );

            Assert.AreEqual(subDirectoryItems.Count(), result.Count, "Unexpected number of results");
            CollectionAssert.DoesNotContain(result.Select(p => p.BaseObject).ToArray(), original, "Original item remains");
            CollectionAssert.Contains(result.Select(p => p.BaseObject).ToArray(), moved, "Moved item is missing");

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void MoveItemAsync_WherePathIsDirectoryAndDestinationDirectoryIsDifferent_MovesDirectory()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectory2Items = fileSystemFixture.SubDirectory2Items;

            var original = (DirectoryInfoContract)rootDirectoryItems.Single(i => i.Name == "SubDir");
            var moved = default(DirectoryInfoContract);

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\SubDir2", rootDirectoryItems, subDirectory2Items);
            gatewayMock.Setup(g => g.MoveItemAsync(rootName, new DirectoryId(@"\SubDir"), @"SubDirMove", new DirectoryId(@"\SubDir2"), It.IsAny<Func<FileSystemInfoLocator>>()))
                .ReturnsAsync(moved = new DirectoryInfoContract(original.Id.Value.Replace("SubDir", "SubDirMove"), original.Name.Replace("SubDir", "SubDirMove"), original.Created, original.Updated))
                .Verifiable();
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Move-Item -Path X:\SubDir -Destination X:\SubDir2\SubDirMove",
                @"Get-ChildItem -Path X:\SubDir2"
            );

            Assert.AreEqual(subDirectory2Items.Count() + 1, result.Count, "Unexpected number of results");
            CollectionAssert.Contains(result.Select(p => p.BaseObject).ToArray(), moved, "Copied item is missing");

            gatewayMock.VerifyAll();
        }
    }
}