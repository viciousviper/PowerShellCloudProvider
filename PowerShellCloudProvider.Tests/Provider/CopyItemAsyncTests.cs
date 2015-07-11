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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using IgorSoft.PowerShellCloudProvider.Interface.IO;
using IgorSoft.PowerShellCloudProvider.Tests.Fixtures;

namespace IgorSoft.PowerShellCloudProvider.Tests.Provider
{
    [TestClass]
    public class CopyItemAsyncTests
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
        public void CopyItemAsync_WherePathIsFile_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;

            var original = (FileInfoContract)rootDirectoryItems.Single(i => i.Name == "File.ext");

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.CopyItemAsync(rootName, new FileId(@"\File.ext"), @"FileCopy.ext", new DirectoryId(@"\"), false))
                .ReturnsAsync(new FileInfoContract(original.Id.Value.Replace("File", "FileCopy"), original.Name.Replace("File", "FileCopy"), original.Created, original.Updated, original.Size, null))
                .Verifiable();
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Copy-Item -Path X:\File.ext -Destination X:\FileCopy.ext"
            );

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void CopyItemAsync_WherePathIsFile_CopiesFile()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;

            var original = (FileInfoContract)rootDirectoryItems.Single(i => i.Name == "File.ext");
            var copy = default(FileInfoContract);

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.CopyItemAsync(rootName, new FileId(@"\File.ext"), @"FileCopy.ext", new DirectoryId(@"\"), false))
                .ReturnsAsync(copy = new FileInfoContract(original.Id.Value.Replace("File", "FileCopy"), original.Name.Replace("File", "FileCopy"), original.Created, original.Updated, original.Size, original.Hash))
                .Verifiable();
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Copy-Item -Path X:\File.ext -Destination X:\FileCopy.ext",
                @"Get-ChildItem -Path X:\"
            );

            Assert.AreEqual(rootDirectoryItems.Count() + 1, result.Count, "Unexpected number of results");
            CollectionAssert.Contains(result.Select(p => p.BaseObject).ToArray(), original, "Original item is missing");
            CollectionAssert.Contains(result.Select(p => p.BaseObject).ToArray(), copy, "Copied item is missing");

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void CopyItemAsync_WherePathIsUnknownFile_Throws()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\", rootDirectoryItems);
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            try {
                new PipelineFixture().Invoke(
                    FileSystemFixture.NewDriveCommand,
                    @"Copy-Item -Path X:\FileUnknown -Destination X:\FileCopy"
                );

                throw new InvalidOperationException("Expected Exception was not thrown");
            } catch (AssertFailedException ex) {
                Assert.IsTrue(ex.Message.EndsWith("ObjectNotFound: (X:\\FileUnknown:String) [Copy-Item], ItemNotFoundException", StringComparison.Ordinal));
            }
        }

        [TestMethod]
        public void CopyItemAsync_WherePathIsFileInSubDirectory_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;

            var original = (FileInfoContract)subDirectoryItems.Single(i => i.Name == "SubFile.ext");

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\SubDir", rootDirectoryItems, subDirectoryItems);
            gatewayMock.Setup(g => g.CopyItemAsync(rootName, new FileId(@"\SubDir\SubFile.ext"), @"SubFileCopy.ext", new DirectoryId(@"\SubDir"), false))
                .ReturnsAsync(new FileInfoContract(original.Id.Value.Replace("SubFile", "SubFileCopy"), original.Name.Replace("SubFile", "SubFileCopy"), original.Created, original.Updated, original.Size, original.Hash))
                .Verifiable();
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Copy-Item -Path X:\SubDir\SubFile.ext -Destination X:\SubDir\SubFileCopy.ext"
            );

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void CopyItemAsync_WherePathIsFileInSubDirectory_CopiesFile()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;

            var original = (FileInfoContract)subDirectoryItems.Single(i => i.Name == "SubFile.ext");
            var copy = default(FileInfoContract);

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\SubDir", rootDirectoryItems, subDirectoryItems);
            gatewayMock.Setup(g => g.CopyItemAsync(rootName, new FileId(@"\SubDir\SubFile.ext"), @"SubFileCopy.ext", new DirectoryId(@"\SubDir"), false))
                .ReturnsAsync(copy = new FileInfoContract(original.Id.Value.Replace("SubFile", "SubFileCopy"), original.Name.Replace("SubFile", "SubFileCopy"), original.Created, original.Updated, original.Size, original.Hash))
                .Verifiable();
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Copy-Item -Path X:\SubDir\SubFile.ext -Destination X:\SubDir\SubFileCopy.ext",
                @"Get-ChildItem -Path X:\SubDir"
            );

            Assert.AreEqual(subDirectoryItems.Count() + 1, result.Count, "Unexpected number of results");
            CollectionAssert.Contains(result.Select(p => p.BaseObject).ToArray(), original, "Original item is missing");
            CollectionAssert.Contains(result.Select(p => p.BaseObject).ToArray(), copy, "Copied item is missing");

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void CopyItemAsync_WherePathIsFileAndDestinationDirectoryIsDifferent_CopiesFile()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;

            var original = (FileInfoContract)subDirectoryItems.Single(i => i.Name == "SubFile.ext");
            var copy = default(FileInfoContract);

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\SubDir", rootDirectoryItems, subDirectoryItems);
            gatewayMock.Setup(g => g.CopyItemAsync(rootName, new FileId(@"\File.ext"), @"FileCopy.ext", new DirectoryId(@"\SubDir"), false))
                .ReturnsAsync(copy = new FileInfoContract(original.Id.Value.Replace("File", "FileCopy"), original.Name.Replace("File", "FileCopy"), original.Created, original.Updated, original.Size, original.Hash))
                .Verifiable();
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Copy-Item -Path X:\File.ext -Destination X:\SubDir\FileCopy.ext",
                @"Get-ChildItem -Path X:\SubDir"
            );

            Assert.AreEqual(subDirectoryItems.Count() + 1, result.Count, "Unexpected number of results");
            CollectionAssert.Contains(result.Select(p => p.BaseObject).ToArray(), copy, "Copied item is missing");

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void CopyItemAsync_WherePathIsDirectory_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;

            var original = (DirectoryInfoContract)rootDirectoryItems.Single(i => i.Name == "SubDir");

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.CopyItemAsync(rootName, new DirectoryId(@"\SubDir"), "SubDirCopy", new DirectoryId(@"\"), false))
                .ReturnsAsync(new DirectoryInfoContract(original.Id.Value.Replace("SubDir", "SubDirCopy"), original.Name.Replace("SubDir", "SubDirCopy"), original.Created, original.Updated))
                .Verifiable();
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Copy-Item -Path X:\SubDir -Destination X:\SubDirCopy"
            );

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void CopyItemAsync_WherePathIsDirectory_CopiesDirectory()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;

            var original = (DirectoryInfoContract)rootDirectoryItems.Single(i => i.Name == "SubDir");
            var copied = default(DirectoryInfoContract);

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.CopyItemAsync(rootName, new DirectoryId(@"\SubDir"), "SubDirCopy", new DirectoryId(@"\"), false))
                .ReturnsAsync(copied = new DirectoryInfoContract(original.Id.Value.Replace("SubDir", "SubDirCopy"), original.Name.Replace("SubDir", "SubDirCopy"), original.Created, original.Updated))
                .Verifiable();
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Copy-Item -Path X:\SubDir -Destination X:\SubDirCopy",
                @"Get-ChildItem -Path X:\"
            );

            Assert.AreEqual(rootDirectoryItems.Count() + 1, result.Count, "Unexpected number of results");
            CollectionAssert.Contains(result.Select(p => p.BaseObject).ToArray(), original, "Original item is missing");
            CollectionAssert.Contains(result.Select(p => p.BaseObject).ToArray(), copied, "Copied item is missing");

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void CopyItemAsync_WherePathIsUnknownDirectory_Throws()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\", rootDirectoryItems);
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            try {
                new PipelineFixture().Invoke(
                    FileSystemFixture.NewDriveCommand,
                    @"Copy-Item -Path X:\SubDirUnknown -Destination X:\SubDirCopy"
                );

                throw new InvalidOperationException("Expected Exception was not thrown");
            } catch (AssertFailedException ex) {
                Assert.IsTrue(ex.Message.EndsWith(@"ObjectNotFound: (X:\SubDirUnknown:String) [Copy-Item], ItemNotFoundException", StringComparison.Ordinal));
            }
        }

        [TestMethod]
        public void CopyItemAsync_WherePathIsDirectoryInSubDirectory_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;

            var original = (DirectoryInfoContract)subDirectoryItems.Single(i => i.Name == "SubSubDir");

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\SubDir", rootDirectoryItems, subDirectoryItems);
            gatewayMock.Setup(g => g.CopyItemAsync(rootName, new DirectoryId(@"\SubDir\SubSubDir"), "SubSubDirCopy", new DirectoryId(@"\SubDir"), false))
                .ReturnsAsync(new DirectoryInfoContract(original.Id.Value.Replace("SubSubDir", "SubSubDirCopy"), original.Name.Replace("SubSubDir", "SubSubDirCopy"), original.Created, original.Updated))
                .Verifiable();
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Copy-Item -Path X:\SubDir\SubSubDir -Destination X:\SubDir\SubSubDirCopy"
            );

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void CopyItemAsync_WherePathIsDirectoryInSubDirectory_CopiesDirectory()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;

            var original = (DirectoryInfoContract)subDirectoryItems.Single(i => i.Name == "SubSubDir");
            var copied = default(DirectoryInfoContract);

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\SubDir", rootDirectoryItems, subDirectoryItems);
            gatewayMock.Setup(g => g.CopyItemAsync(rootName, new DirectoryId(@"\SubDir\SubSubDir"), "SubSubDirCopy", new DirectoryId(@"\SubDir"), false))
                .ReturnsAsync(copied = new DirectoryInfoContract(original.Id.Value.Replace("SubSubDir", "SubSubDirCopy"), original.Name.Replace("SubSubDir", "SubSubDirCopy"), original.Created, original.Updated))
                .Verifiable();
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Copy-Item -Path X:\SubDir\SubSubDir -Destination X:\SubDir\SubSubDirCopy",
                @"Get-ChildItem -Path X:\SubDir"
            );

            Assert.AreEqual(subDirectoryItems.Count() + 1, result.Count, "Unexpected number of results");
            CollectionAssert.Contains(result.Select(p => p.BaseObject).ToArray(), original, "Original item is missing");
            CollectionAssert.Contains(result.Select(p => p.BaseObject).ToArray(), copied, "Copied item is missing");

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void CopyItemAsync_WherePathIsDirectoryAndDestinationDirectoryIsDifferent_CopiesDirectory()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;

            var original = (DirectoryInfoContract)rootDirectoryItems.Single(i => i.Name == "SubDir");
            var copy = default(DirectoryInfoContract);

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\SubDir", rootDirectoryItems, subDirectoryItems);
            gatewayMock.Setup(g => g.CopyItemAsync(rootName, new DirectoryId(@"\SubDir"), @"SubDirCopy", new DirectoryId(@"\SubDir"), false))
                .ReturnsAsync(copy = new DirectoryInfoContract(original.Id.Value.Replace("SubDir", "SubDirCopy"), original.Name.Replace("SubDir", "SubDirCopy"), original.Created, original.Updated))
                .Verifiable();
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Copy-Item -Path X:\SubDir -Destination X:\SubDir\SubDirCopy",
                @"Get-ChildItem -Path X:\SubDir"
            );

            Assert.AreEqual(subDirectoryItems.Count() + 1, result.Count, "Unexpected number of results");
            CollectionAssert.Contains(result.Select(p => p.BaseObject).ToArray(), copy, "Copied item is missing");

            gatewayMock.VerifyAll();
        }
    }
}