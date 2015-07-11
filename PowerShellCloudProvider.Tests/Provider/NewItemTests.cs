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
using IgorSoft.PowerShellCloudProvider.Interface.IO;
using IgorSoft.PowerShellCloudProvider.Tests.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace IgorSoft.PowerShellCloudProvider.Tests.Provider
{
    [TestClass]
    public class NewItemTests
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
        public void NewItem_WherePathIsRootAndItemTypeIsDirectory_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var newItem = new DirectoryInfoContract(@"\SubDir", "SubDir", DateTime.Now, DateTime.Now);

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.NewDirectoryItem(rootName, new DirectoryId(@"\"), "NewSubDir"))
                .Returns(newItem)
                .Verifiable();
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"New-Item -Type Directory -Path X:\ -Name NewSubDir"
            );

            Assert.AreEqual(1, result.Count, "Unexpected number of results");
            Assert.AreEqual(newItem, result[0].BaseObject, "Mismatching result");

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void NewItem_WherePathIsRootAndItemTypeIsDirectory_CreatesNewDirectory()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var newItem = new DirectoryInfoContract(@"\NewSubDir", "NewSubDir", DateTime.Now, DateTime.Now);

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.NewDirectoryItem(rootName, new DirectoryId(@"\"), "NewSubDir"))
                .Returns(newItem)
                .Verifiable();
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"New-Item -Type Directory -Path X:\ -Name NewSubDir",
                @"Get-ChildItem -Path X:\"
            );

            Assert.AreEqual(rootDirectoryItems.Length + 1, result.Count, "Unexpected number of results");
            CollectionAssert.Contains(result.Select(p => p.BaseObject).ToArray(), newItem, "Missing result");

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void NewItem_WherePathIsSubDirAndItemTypeIsDirectory_CreatesNewDirectory()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;
            var newItem = new DirectoryInfoContract(@"\SubDir\NewSubSubDir", "NewSubSubDir", DateTime.Now, DateTime.Now);

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\SubDir", rootDirectoryItems, subDirectoryItems);
            gatewayMock.Setup(g => g.NewDirectoryItem(rootName, new DirectoryId(@"\SubDir"), "NewSubSubDir"))
                .Returns(newItem)
                .Verifiable();
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"New-Item -Type Directory -Path X:\SubDir -Name NewSubSubDir",
                @"Get-ChildItem -Path X:\SubDir"
            );

            Assert.AreEqual(subDirectoryItems.Length + 1, result.Count, "Unexpected number of results");
            CollectionAssert.Contains(result.Select(p => p.BaseObject).ToArray(), newItem, "Missing result");

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void NewItem_WherePathIsRootAndItemTypeIsFile_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var newItem = new FileInfoContract(@"\NewFile", "NewFile", DateTime.Now, DateTime.Now, 0, null);

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.NewFileItem(rootName, new DirectoryId(@"\"), "NewFile", null, It.Is<IProgress<ProgressValue>>(p => true)))
                .Returns(newItem)
                .Verifiable();
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"New-Item -Type File -Path X:\ -Name NewFile"
            );

            Assert.AreEqual(1, result.Count, "Unexpected number of results");
            Assert.AreEqual(newItem, result[0].BaseObject, "Mismatching result");

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void NewItem_WherePathIsRootAndItemTypeIsFile_CreatesNewFile()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var newItem = new FileInfoContract(@"\NewFile", "NewFile", DateTime.Now, DateTime.Now, 0, null);

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.NewFileItem(rootName, new DirectoryId(@"\"), "NewFile", null, It.Is<IProgress<ProgressValue>>(p => true)))
                .Returns(newItem)
                .Verifiable();
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"New-Item -Type File -Path X:\ -Name NewFile",
                @"Get-ChildItem -Path X:\"
            );

            Assert.AreEqual(rootDirectoryItems.Length + 1, result.Count, "Unexpected number of results");
            CollectionAssert.Contains(result.Select(p => p.BaseObject).ToArray(), newItem, "Missing result");

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void NewItem_WherePathIsSubDirAndItemTypeIsFile_CreatesNewFile()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;
            var newItem = new FileInfoContract(@"\SubDir\NewSubFile", "NewSubFile", DateTime.Now, DateTime.Now, 0, null);

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\SubDir", rootDirectoryItems, subDirectoryItems);
            gatewayMock.Setup(g => g.NewFileItem(rootName, new DirectoryId(@"\SubDir"), "NewSubFile", null, It.Is<IProgress<ProgressValue>>(p => true)))
                .Returns(newItem)
                .Verifiable();
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"New-Item -Type File -Path X:\SubDir -Name NewSubFile",
                @"Get-ChildItem -Path X:\SubDir"
            );

            Assert.AreEqual(subDirectoryItems.Length + 1, result.Count, "Unexpected number of results");
            CollectionAssert.Contains(result.Select(p => p.BaseObject).ToArray(), newItem, "Missing result");

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void NewItem_WherePathIsSubDirAndItemTypeIsFile_CanRemoveNewFile()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;
            var newItem = new FileInfoContract(@"\SubDir\NewSubFile", "NewSubFile", DateTime.Now, DateTime.Now, 0, null);

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\SubDir", rootDirectoryItems, subDirectoryItems);
            gatewayMock.Setup(g => g.NewFileItem(rootName, new DirectoryId(@"\SubDir"), "NewSubFile", null, It.Is<IProgress<ProgressValue>>(p => true)))
                .Returns(newItem)
                .Verifiable();
            gatewayMock.Setup(g => g.RemoveItem(rootName, new FileId(@"\SubDir\NewSubFile"), false)).Verifiable();
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"New-Item -Type File -Path X:\SubDir -Name NewSubFile",
                @"Remove-Item -Path X:\SubDir\NewSubFile",
                @"Get-ChildItem -Path X:\SubDir"
            );

            Assert.AreEqual(subDirectoryItems.Length, result.Count, "Unexpected number of results");
            CollectionAssert.DoesNotContain(result.Select(p => p.BaseObject).ToArray(), newItem, "Excessive result");

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void NewItem_WherePathIsRootAndItemTypeIsFile_PassesValue()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var content = TestContent.SingleLineTestContent;
            var newItem = new FileInfoContract(@"\NewFile", "NewFile", DateTime.Now, DateTime.Now, content.Length, FileSystemFixture.GetHash(content));

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.NewFileItem(rootName, new DirectoryId(@"\"), "NewFile", It.Is<Stream>(s => new StreamReader(s).ReadToEnd() == content), It.Is<IProgress<ProgressValue>>(p => true)))
                .Returns(newItem)
                .Verifiable();
            compositionFixture.ExportGateway(gatewayMock.Object);

            var pipelineFixture = new PipelineFixture();
            pipelineFixture.SetVariable("value", content);
            var result = pipelineFixture.Invoke(
                FileSystemFixture.NewDriveCommand,
                @"New-Item -Type File -Path X:\ -Name NewFile -Value $value"
            );

            Assert.AreEqual(1, result.Count, "Unexpected number of results");
            Assert.AreEqual(newItem, result[0].BaseObject, "Mismatching result");

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void NewItem_WhereItemIsAlreadyPresent_Throws()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var content = TestContent.SingleLineTestContent;

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\", rootDirectoryItems);
            compositionFixture.ExportGateway(gatewayMock.Object);

            try {
                var pipelineFixture = new PipelineFixture();
                pipelineFixture.SetVariable("value", content);
                pipelineFixture.Invoke(
                    FileSystemFixture.NewDriveCommand,
                    @"New-Item -Type File -Path X:\ -Name File.ext -Value $value"
                );

                throw new InvalidOperationException("Expected Exception was not thrown");
            } catch (AssertFailedException ex) {
                Assert.IsTrue(ex.Message.EndsWith("NotSpecified: (X:\\File.ext:String) [New-Item], NotSupportedException", StringComparison.Ordinal));
            }

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void NewItem_WhereEncryptionKeyIsSpecifiedAndItemTypeIsFile_PassesEncryptedValue()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var content = TestContent.SingleLineTestContent;
            var newItem = new FileInfoContract(@"\NewFile", "NewFile", DateTime.Now, DateTime.Now, content.Length, FileSystemFixture.GetHash(content));

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.NewFileItem(rootName, new DirectoryId(@"\"), "NewFile", It.Is<Stream>(s => EncryptionFixture.GetDecryptingReader(s, FileSystemFixture.EncryptionKey).ReadToEnd() == content), It.Is<IProgress<ProgressValue>>(p => true)))
                .Returns(newItem)
                .Verifiable();
            compositionFixture.ExportGateway(gatewayMock.Object);

            var pipelineFixture = new PipelineFixture();
            pipelineFixture.SetVariable("value", content);
            var result = pipelineFixture.Invoke(
                FileSystemFixture.NewDriveCommandWithEncryptionKey,
                @"New-Item -Type File -Path X:\ -Name NewFile -Value $value"
            );

            Assert.AreEqual(1, result.Count, "Unexpected number of results");
            Assert.AreEqual(newItem, result[0].BaseObject, "Mismatching result");

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void NewItem_WherePathIsIncompleteAndItemTypeIsDirectory_CreatesNewDirectory()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var newIntermediateDirectory = new DirectoryInfoContract(@"\NewSubDir", "NewSubDir", DateTime.Now, DateTime.Now);
            var newItem = new DirectoryInfoContract(@"\NewSubDir\NewSubSubDir", "NewSubSubDir", DateTime.Now, DateTime.Now);

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.NewDirectoryItem(rootName, new DirectoryId(@"\"), "NewSubDir"))
                .Returns(newIntermediateDirectory)
                .Verifiable();
            gatewayMock.Setup(g => g.NewDirectoryItem(rootName, new DirectoryId(@"\NewSubDir"), "NewSubSubDir"))
                .Returns(newItem)
                .Verifiable();
            var subDirs = new Queue<string>(new[] { @"\", @"\NewSubDir", @"\NewSubDir\NewSubSubDir" });
            var predicateIterator = new Func<string, bool>(s => s == subDirs.Dequeue());
            gatewayMock.SetupSequence(g => g.GetChildItem(rootName, It.Is<DirectoryId>(d => predicateIterator(d.Value))))
                .Returns(new FileSystemInfoContract[0])
                .Returns(new FileSystemInfoContract[0])
                .Returns(new FileSystemInfoContract[0])
                .Throws(new InvalidOperationException(@"Redundant access to directory"));
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"New-Item -Type Directory -Path X:\NewSubDir -Name NewSubSubDir -Force",
                @"Get-ChildItem -Path X:\ -Filter 'NewSub*' -Recurse"
            );

            Assert.AreEqual(2, result.Count, "Unexpected number of results");
            CollectionAssert.AreEqual(result.Select(i => i.BaseObject).ToArray(), new[] { newIntermediateDirectory, newItem }, "Unexpected result");

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void NewItem_WherePathIsIncompleteAndItemTypeIsFile_CreatesNewFile()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var newIntermediateDirectory = new DirectoryInfoContract(@"\NewSubDir", "NewSubDir", DateTime.Now, DateTime.Now);
            var newItem = new FileInfoContract(@"\NewSubDir\NewSubFile", "NewSubFile", DateTime.Now, DateTime.Now, 0, null);

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.NewDirectoryItem(rootName, new DirectoryId(@"\"), "NewSubDir"))
                .Returns(newIntermediateDirectory)
                .Verifiable();
            gatewayMock.Setup(g => g.NewFileItem(rootName, new DirectoryId(@"\NewSubDir"), "NewSubFile", null, It.Is<IProgress<ProgressValue>>(p => true)))
                .Returns(newItem)
                .Verifiable();
            var subDirs = new Queue<string>(new[] { @"\", @"\NewSubDir" });
            var predicateIterator = new Func<string, bool>(s => s == subDirs.Dequeue());
            gatewayMock.SetupSequence(g => g.GetChildItem(rootName, It.Is<DirectoryId>(d => predicateIterator(d.Value))))
                .Returns(new FileSystemInfoContract[0])
                .Returns(new FileSystemInfoContract[0])
                .Throws(new InvalidOperationException(@"Redundant access to directory"));
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"New-Item -Type File -Path X:\NewSubDir -Name NewSubFile -Force",
                @"Get-ChildItem -Path X:\ -Filter 'NewSub*' -Recurse"
            );

            Assert.AreEqual(2, result.Count, "Unexpected number of results");
            CollectionAssert.AreEqual(result.Select(i => i.BaseObject).ToArray(), new FileSystemInfoContract[] { newIntermediateDirectory, newItem }, "Unexpected result");

            gatewayMock.VerifyAll();
        }
    }
}