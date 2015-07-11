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
using System.Management.Automation;
using IgorSoft.PowerShellCloudProvider.Interface.IO;
using IgorSoft.PowerShellCloudProvider.Tests.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IgorSoft.PowerShellCloudProvider.Tests.Provider
{
    [TestClass]
    public class RemoveItemTests
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
        public void RemoveItem_WherePathIsFile_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.RemoveItem(rootName, new FileId(@"\File.ext"), false)).Verifiable();
            compositionFixture.ExportGateway(gatewayMock.Object);

            new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Remove-Item -Path X:\File.ext"
            );

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void RemoveItem_WherePathIsFile_RemovesFile()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.RemoveItem(rootName, new FileId(@"\File.ext"), false)).Verifiable();
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Remove-Item -Path X:\File.ext",
                @"Get-ChildItem -Path X:\"
            );

            Assert.AreEqual(rootDirectoryItems.Length - 1, result.Count, "Unexpected number of results");
            CollectionAssert.AreEquivalent(rootDirectoryItems.Where(f => f.Name != "File.ext").ToList(), result.Select(p => p.BaseObject).Cast<FileSystemInfoContract>().ToList());

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void RemoveItem_WherePathIsUnknownFile_Throws()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.RemoveItem(rootName, new FileId(@"\FileUnknown"), false))
                .Throws<FileNotFoundException>();
            compositionFixture.ExportGateway(gatewayMock.Object);

            try {
                new PipelineFixture().Invoke(
                    FileSystemFixture.NewDriveCommand,
                    @"Remove-Item -Path X:\FileUnknown"
                );

                throw new InvalidOperationException("Expected Exception was not thrown");
            } catch (AssertFailedException ex) {
                Assert.IsTrue(ex.Message.EndsWith(@"ObjectNotFound: (X:\FileUnknown:String) [Remove-Item], ItemNotFoundException", StringComparison.Ordinal));
            }
        }

        [TestMethod]
        public void RemoveItem_WherePathIsFileInSubDirectory_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\SubDir", rootDirectoryItems, subDirectoryItems);
            gatewayMock.Setup(g => g.RemoveItem(rootName, new FileId(@"\SubDir\SubFile.ext"), false)).Verifiable();
            compositionFixture.ExportGateway(gatewayMock.Object);

            new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Remove-Item -Path X:\SubDir\SubFile.ext"
            );

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void RemoveItem_WherePathIsFileInSubDirectory_RemovesFile()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\SubDir", rootDirectoryItems, subDirectoryItems);
            gatewayMock.Setup(g => g.RemoveItem(rootName, new FileId(@"\SubDir\SubFile.ext"), false)).Verifiable();
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Remove-Item -Path X:\SubDir\SubFile.ext",
                @"Get-ChildItem -Path X:\SubDir"
            );

            Assert.AreEqual(subDirectoryItems.Length - 1, result.Count, "Unexpected number of results");
            CollectionAssert.AreEquivalent(subDirectoryItems.Where(f => f.Name != "SubFile.ext").ToList(), result.Select(p => p.BaseObject).Cast<FileSystemInfoContract>().ToList());

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void RemoveItem_WherePathIsDirectory_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\SubDir", rootDirectoryItems, subDirectoryItems);
            gatewayMock.Setup(g => g.RemoveItem(rootName, new DirectoryId(@"\SubDir"), true)).Verifiable();
            compositionFixture.ExportGateway(gatewayMock.Object);

            new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Remove-Item -Path X:\SubDir -Recurse"
            );

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void RemoveItem_WherePathIsSubDirectory_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;
            var subSubDirectoryItems = fileSystemFixture.SubSubDirectoryItems;

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\SubDir\SubSubDir", rootDirectoryItems, subDirectoryItems, subSubDirectoryItems);
            gatewayMock.Setup(g => g.RemoveItem(rootName, new DirectoryId(@"\SubDir\SubSubDir"), true)).Verifiable();
            compositionFixture.ExportGateway(gatewayMock.Object);

            new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Remove-Item -Path X:\SubDir\SubSubDir -Recurse"
            );

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        [ExpectedException(typeof(System.Management.Automation.Host.HostException))]
        public void RemoveItem_WherePathIsNonEmptyDirectoryAndRecurseIsNotSpecified_Throws()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\SubDir", rootDirectoryItems, subDirectoryItems);
            compositionFixture.ExportGateway(gatewayMock.Object);

            try {
                new PipelineFixture().Invoke(
                    FileSystemFixture.NewDriveCommand,
                    @"Remove-Item -Path X:\SubDir"
                );
            } catch (CmdletInvocationException ex) {
                throw ex.InnerException;
            }

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void RemoveItem_WherePathIsUnknownDirectory_Throws()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\", rootDirectoryItems);
            compositionFixture.ExportGateway(gatewayMock.Object);

            try {
                new PipelineFixture().Invoke(
                    FileSystemFixture.NewDriveCommand,
                    @"Remove-Item -Path X:\SubDirUnknown -Recurse"
                );

                throw new InvalidOperationException("Expected Exception was not thrown");
            } catch (AssertFailedException ex) {
                Assert.IsTrue(ex.Message.EndsWith(@"ObjectNotFound: (X:\SubDirUnknown:String) [Remove-Item], ItemNotFoundException", StringComparison.Ordinal));
            }

            gatewayMock.VerifyAll();
        }
    }
}