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
using System.IO;
using System.Linq;
using IgorSoft.PowerShellCloudProvider.Interface.IO;
using IgorSoft.PowerShellCloudProvider.Tests.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IgorSoft.PowerShellCloudProvider.Tests.Provider
{
    [TestClass]
    public class RenameItemTests
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
        public void RenameItem_WherePathIsFile_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;

            var original = (FileInfoContract)rootDirectoryItems.Single(i => i.Name == "File.ext");

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.RenameItem(rootName, new FileId(@"\File.ext"), "FileRenamed.ext"))
                .Returns(new FileInfoContract(original.Id.Value.Replace("File", "FileRenamed"), original.Name.Replace("File", "FileRenamed"), original.Created, original.Updated, original.Size, null))
                .Verifiable();
            compositionFixture.ExportGateway(gatewayMock.Object);

            new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Rename-Item -Path X:\File.ext -NewName FileRenamed.ext"
            );

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void RenameItem_WherePathIsFile_RenamesFile()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;

            var original = (FileInfoContract)rootDirectoryItems.Single(i => i.Name == "File.ext");
            var renamed = default(FileInfoContract);

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.RenameItem(rootName, new FileId(@"\File.ext"), "FileRenamed.ext"))
                .Returns(renamed = new FileInfoContract(original.Id.Value.Replace("File", "FileRenamed"), original.Name.Replace("File", "FileRenamed"), original.Created, original.Updated, original.Size, original.Hash))
                .Verifiable();
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Rename-Item -Path X:\File.ext -NewName FileRenamed.ext",
                @"Get-ChildItem -Path X:\"
            );

            Assert.AreEqual(rootDirectoryItems.Count(), result.Count, "Unexpected number of results");
            CollectionAssert.DoesNotContain(result.Select(p => p.BaseObject).ToArray(), original, "Unrenamed original remains");
            CollectionAssert.Contains(result.Select(p => p.BaseObject).ToArray(), renamed, "Renamed item is missing");

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void RenameItem_WherePathIsUnknownFile_Throws()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.RenameItem(rootName, new FileId(@"\FileUnknown"), "FileRenamed"))
                .Throws<FileNotFoundException>();
            compositionFixture.ExportGateway(gatewayMock.Object);

            try {
                new PipelineFixture().Invoke(
                    FileSystemFixture.NewDriveCommand,
                    @"Rename-Item -Path X:\FileUnknown -NewName FileRenamed"
                );

                throw new InvalidOperationException("Expected Exception was not thrown");
            } catch (AssertFailedException ex) {
                Assert.IsTrue(ex.Message.EndsWith(@"InvalidOperation: (:) [Rename-Item], PSInvalidOperationException", StringComparison.Ordinal));
            }
        }

        [TestMethod]
        public void RenameItem_WherePathIsFileInSubDirectory_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;

            var original = (FileInfoContract)subDirectoryItems.Single(i => i.Name == "SubFile.ext");

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\SubDir", rootDirectoryItems, subDirectoryItems);
            gatewayMock.Setup(g => g.RenameItem(rootName, new FileId(@"\SubDir\SubFile.ext"), "SubFileRenamed.ext"))
                .Returns(new FileInfoContract(original.Id.Value.Replace("SubFile", "SubFileRenamed"), original.Name.Replace("SubFile", "SubFileRenamed"), original.Created, original.Updated, original.Size, original.Hash))
                .Verifiable();
            compositionFixture.ExportGateway(gatewayMock.Object);

            new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Rename-Item -Path X:\SubDir\SubFile.ext -NewName SubFileRenamed.ext"
            );

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void RenameItem_WherePathIsFileInSubDirectory_RenamesFile()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;

            var original = (FileInfoContract)subDirectoryItems.Single(i => i.Name == "SubFile.ext");
            var renamed = default(FileInfoContract);

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\SubDir", rootDirectoryItems, subDirectoryItems);
            gatewayMock.Setup(g => g.RenameItem(rootName, new FileId(@"\SubDir\SubFile.ext"), "SubFileRenamed.ext"))
                .Returns(renamed = new FileInfoContract(original.Id.Value.Replace("SubFile", "SubFileRenamed"), original.Name.Replace("SubFile", "SubFileRenamed"), original.Created, original.Updated, original.Size, original.Hash))
                .Verifiable();
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Rename-Item -Path X:\SubDir\SubFile.ext -NewName SubFileRenamed.ext",
                @"Get-ChildItem -Path X:\SubDir"
            );

            Assert.AreEqual(subDirectoryItems.Count(), result.Count, "Unexpected number of results");
            CollectionAssert.DoesNotContain(result.Select(p => p.BaseObject).ToArray(), original, "Unrenamed original remains");
            CollectionAssert.Contains(result.Select(p => p.BaseObject).ToArray(), renamed, "Renamed item is missing");

            gatewayMock.VerifyAll();
        }


        [TestMethod]
        public void RenameItem_WherePathIsDirectory_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;

            var original = (DirectoryInfoContract)rootDirectoryItems.Single(i => i.Name == "SubDir");

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.RenameItem(rootName, new DirectoryId(@"\SubDir"), "SubDirRenamed"))
                .Returns(new DirectoryInfoContract(original.Id.Value.Replace("SubDir", "SubDirRenamed"), original.Name.Replace("SubDir", "SubDirRenamed"), original.Created, original.Updated))
                .Verifiable();
            compositionFixture.ExportGateway(gatewayMock.Object);

            new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Rename-Item -Path X:\SubDir -NewName SubDirRenamed"
            );

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void RenameItem_WherePathIsDirectory_RenamesDirectory()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;

            var original = (DirectoryInfoContract)rootDirectoryItems.Single(i => i.Name == "SubDir");
            var renamed = default(DirectoryInfoContract);

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.RenameItem(rootName, new DirectoryId(@"\SubDir"), "SubDirRenamed"))
                .Returns(renamed = new DirectoryInfoContract(original.Id.Value.Replace("SubDir", "SubDirRenamed"), original.Name.Replace("SubDir", "SubDirRenamed"), original.Created, original.Updated))
                .Verifiable();
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Rename-Item -Path X:\SubDir -NewName SubDirRenamed",
                @"Get-ChildItem -Path X:\"
            );

            Assert.AreEqual(rootDirectoryItems.Count(), result.Count, "Unexpected number of results");
            CollectionAssert.DoesNotContain(result.Select(p => p.BaseObject).ToArray(), original, "Unrenamed original remains");
            CollectionAssert.Contains(result.Select(p => p.BaseObject).ToArray(), renamed, "Renamed item is missing");

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void RenameItem_WherePathIsUnknownDirectory_Throws()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.RenameItem(rootName, new DirectoryId(@"\SubDirUnknown"), "SubDirRenamed"))
                .Throws<FileNotFoundException>();
            compositionFixture.ExportGateway(gatewayMock.Object);

            try {
                new PipelineFixture().Invoke(
                    FileSystemFixture.NewDriveCommand,
                    @"Rename-Item -Path X:\SubDirUnknown -NewName SubDirRenamed"
                );

                throw new InvalidOperationException("Expected Exception was not thrown");
            } catch (AssertFailedException ex) {
                Assert.IsTrue(ex.Message.EndsWith(@"InvalidOperation: (:) [Rename-Item], PSInvalidOperationException", StringComparison.Ordinal));
            }
        }

        [TestMethod]
        public void RenameItem_WherePathIsDirectoryInSubDirectory_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;

            var original = (DirectoryInfoContract)subDirectoryItems.Single(i => i.Name == "SubSubDir");

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\SubDir", rootDirectoryItems, subDirectoryItems);
            gatewayMock.Setup(g => g.RenameItem(rootName, new DirectoryId(@"\SubDir\SubSubDir"), "SubSubDirRenamed"))
                .Returns(new DirectoryInfoContract(original.Id.Value.Replace("SubSubDir", "SubSubDirRenamed"), original.Name.Replace("SubSubDir", "SubSubDirRenamed"), original.Created, original.Updated))
                .Verifiable();
            compositionFixture.ExportGateway(gatewayMock.Object);

            new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Rename-Item -Path X:\SubDir\SubSubDir -NewName SubSubDirRenamed"
            );

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void RenameItem_WherePathIsDirectoryInSubDirectory_RenamesDirectory()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;

            var original = (DirectoryInfoContract)subDirectoryItems.Single(i => i.Name == "SubSubDir");
            var renamed = default(DirectoryInfoContract);

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\SubDir", rootDirectoryItems, subDirectoryItems);
            gatewayMock.Setup(g => g.RenameItem(rootName, new DirectoryId(@"\SubDir\SubSubDir"), "SubSubDirRenamed"))
                .Returns(renamed = new DirectoryInfoContract(original.Id.Value.Replace("SubSubDir", "SubSubDirRenamed"), original.Name.Replace("SubSubDir", "SubSubDirRenamed"), original.Created, original.Updated))
                .Verifiable();
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Rename-Item -Path X:\SubDir\SubSubDir -NewName SubSubDirRenamed",
                @"Get-ChildItem -Path X:\SubDir"
            );

            Assert.AreEqual(subDirectoryItems.Count(), result.Count, "Unexpected number of results");
            CollectionAssert.DoesNotContain(result.Select(p => p.BaseObject).ToArray(), original, "Unrenamed original remains");
            CollectionAssert.Contains(result.Select(p => p.BaseObject).ToArray(), renamed, "Renamed item is missing");

            gatewayMock.VerifyAll();
        }
    }
}