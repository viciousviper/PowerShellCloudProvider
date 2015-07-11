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
using System.Globalization;
using System.Linq;
using IgorSoft.PowerShellCloudProvider.Interface.Composition;
using IgorSoft.PowerShellCloudProvider.Interface.IO;
using IgorSoft.PowerShellCloudProvider.Tests.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace IgorSoft.PowerShellCloudProvider.Tests.Provider
{
    [TestClass]
    public class GetChildItemTests
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
        public void GetChildItem_WherePathIsRoot_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\", rootDirectoryItems);
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Get-ChildItem -Path X:\"
            );

            Assert.AreEqual(rootDirectoryItems.Length, result.Count, "Unexpected number of results");
            CollectionAssert.AreEquivalent(rootDirectoryItems, result.Select(p => p.BaseObject).Cast<FileSystemInfoContract>().ToList());
        }

        [TestMethod]
        public void GetChildItem_WherePathIsRootAndCredentialsAreSpecified_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName("@TestUser");
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\", rootDirectoryItems);
            compositionFixture.ExportGateway(gatewayMock.Object);

            var pipelineFixture = new PipelineFixture();
            pipelineFixture.SetVariable("credential", PipelineFixture.GetCredential("TestUser", "TestPassword"));
            var result = pipelineFixture.Invoke(
                FileSystemFixture.NewDriveCommandWithCredential,
                @"Get-ChildItem -Path Y:\"
            );

            Assert.AreEqual(rootDirectoryItems.Length, result.Count, "Unexpected number of results");
            CollectionAssert.AreEquivalent(rootDirectoryItems, result.Select(p => p.BaseObject).Cast<FileSystemInfoContract>().ToList());
        }

        [TestMethod]
        public void GetChildItem_WherePathIsRootAndFilterIsSpecified_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;

            var gatewayMock = mockingFixture.InitializeGetChildItemsWithFilter(rootName, @"\", "*File.ext", rootDirectoryItems);
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Get-ChildItem -Path X:\ -Filter *File.ext"
            );

            Assert.AreEqual(rootDirectoryItems.Count(i => i.Name.EndsWith("File.ext", StringComparison.Ordinal)), result.Count, "Unexpected number of results");
            CollectionAssert.AllItemsAreInstancesOfType(result.Select(i => i.BaseObject).ToList(), typeof(FileInfoContract), "Results are not of type FileInfoContract");
            CollectionAssert.AreEqual(rootDirectoryItems.Where(i => i.Name.EndsWith("File.ext", StringComparison.Ordinal)).ToList(), result.Select(i => i.BaseObject).ToList(), "Mismatched result");
        }

        [TestMethod]
        public void GetChildItem_WherePathIsSubSubDirectoryAndFilterIsSpecified_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;
            var subSubDirectoryItems = fileSystemFixture.SubSubDirectoryItems;

            var gatewayMock = mockingFixture.InitializeGetChildItemsWithFilter(rootName, @"\SubDir\SubSubDir", "*File.ext", rootDirectoryItems, subDirectoryItems, subSubDirectoryItems);
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Get-ChildItem -Path X:\SubDir\SubSubDir -Filter *File.ext"
            );

            Assert.AreEqual(subSubDirectoryItems.Count(i => i.Name.EndsWith("File.ext", StringComparison.Ordinal)), result.Count, "Unexpected number of results");
            CollectionAssert.AllItemsAreInstancesOfType(result.Select(i => i.BaseObject).ToList(), typeof(FileInfoContract), "Results are not of type FileInfoContract");
            CollectionAssert.AreEqual(subSubDirectoryItems.Where(i => i.Name.EndsWith("File.ext", StringComparison.Ordinal)).ToList(), result.Select(i => i.BaseObject).ToList(), "Mismatched result");
        }

        [TestMethod]
        public void GetChildItem_WherePathIsRootAndIncludeIsSpecified_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\", rootDirectoryItems);
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Get-ChildItem -Path X:\ -Include *File.ext"
            );

            Assert.AreEqual(3, result.Count, "Unexpected number of results");
            CollectionAssert.AreEqual(rootDirectoryItems.Where(i => i.Name.EndsWith("File.ext", StringComparison.Ordinal)).ToList(), result.Select(r => r.BaseObject).ToList(), "Mismatched result");
        }

        [TestMethod]
        public void GetChildItem_WherePathIsRootAndExcludeIsSpecified_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;
            var subSubDirectoryItems = fileSystemFixture.SubSubDirectoryItems;

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\SubDir\SubSubDir", rootDirectoryItems, subDirectoryItems, subSubDirectoryItems);
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Get-ChildItem -Path X:\ -Exclude *File.ext"
            );

            Assert.AreEqual(2, result.Count, "Unexpected number of results");
            CollectionAssert.AreEquivalent(rootDirectoryItems.Where(i => !i.Name.EndsWith("File.ext", StringComparison.Ordinal)).ToList(), result.Select(i => i.BaseObject).ToList(), "Mismatched result");
        }

        [TestMethod]
        public void GetChildItem_WherePathIsUnknownRoot_Throws()
        {
            var gatewayMock = new Mock<ICloudGateway>(MockBehavior.Strict);

            compositionFixture.ExportGateway(gatewayMock.Object);

            try {
                new PipelineFixture().Invoke(
                    FileSystemFixture.NewDriveCommand,
                    @"Get-ChildItem -Path Undefined:\"
                );

                throw new InvalidOperationException("Expected Exception was not thrown");
            } catch (AssertFailedException ex) {
                Assert.IsTrue(ex.Message.EndsWith(@"ObjectNotFound: (Undefined:String) [Get-ChildItem], DriveNotFoundException", StringComparison.Ordinal));
            }
        }

        [TestMethod]
        public void GetChildItem_WherePathIsSubDirectory_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\SubDir", rootDirectoryItems, subDirectoryItems);
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Get-ChildItem -Path X:\SubDir"
            );

            Assert.AreEqual(subDirectoryItems.Length, result.Count, "Unexpected number of results");
            CollectionAssert.AreEquivalent(subDirectoryItems, result.Select(p => p.BaseObject).Cast<FileSystemInfoContract>().ToList());
        }

        [TestMethod]
        public void GetChildItem_WherePathIsSubSubDirectory_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;
            var subSubDirectoryItems = fileSystemFixture.SubSubDirectoryItems;

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\SubDir\SubSubDir", rootDirectoryItems, subDirectoryItems, subSubDirectoryItems);
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Get-ChildItem -Path X:\SubDir\SubSubDir"
            );

            Assert.AreEqual(subSubDirectoryItems.Length, result.Count, "Unexpected number of results");
            CollectionAssert.AreEquivalent(subSubDirectoryItems, result.Select(p => p.BaseObject).Cast<FileSystemInfoContract>().ToList());
        }

        [TestMethod]
        public void GetChildItemRecursive_WherePathIsRoot_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;
            var subSubDirectoryItems = fileSystemFixture.SubSubDirectoryItems;

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\SubDir\SubSubDir", rootDirectoryItems, subDirectoryItems, subSubDirectoryItems);
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Get-ChildItem -Path X:\ -Recurse"
            );

            Assert.AreEqual(rootDirectoryItems.Length + subDirectoryItems.Length + subSubDirectoryItems.Length, result.Count, "Unexpected number of results");
            CollectionAssert.AreEquivalent(rootDirectoryItems.Union(subDirectoryItems).Union(subSubDirectoryItems).ToList(), result.Select(p => p.BaseObject).Cast<FileSystemInfoContract>().ToList());
        }

        [TestMethod]
        public void GetChildItemRecursive_WherePathIsSubDirectory_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;
            var subSubDirectoryItems = fileSystemFixture.SubSubDirectoryItems;

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\SubDir\SubSubDir", rootDirectoryItems, subDirectoryItems, subSubDirectoryItems);
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Get-ChildItem -Path X:\SubDir -Recurse"
            );

            Assert.AreEqual(subDirectoryItems.Length + subSubDirectoryItems.Length, result.Count, "Unexpected number of results");
            CollectionAssert.AreEquivalent(subDirectoryItems.Union(subSubDirectoryItems).ToList(), result.Select(p => p.BaseObject).Cast<FileSystemInfoContract>().ToList());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        [TestMethod]
        public void GetChildItem_WherePathIsRootAndForceIsSpecified_RefreshesResult()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var rootDirectoryItemsRefreshed = fileSystemFixture.RootDirectoryItems
                .Select(f => f is FileInfoContract
                    ? new FileInfoContract(f.Id.Value.Insert(f.Id.Value.IndexOf(".ext", StringComparison.Ordinal), "Refreshed"), f.Name.Insert(f.Name.IndexOf(".ext", StringComparison.Ordinal), "Refreshed"), f.Created, f.Updated, ((FileInfoContract)f).Size, ((FileInfoContract)f).Hash) as FileSystemInfoContract
                    : new DirectoryInfoContract(f.Id + "Refreshed", f.Name + "Refreshed", f.Created, f.Updated) as FileSystemInfoContract)
                .ToArray();

            var gatewayMock = new MockingFixture().InitializeGetChildItems(rootName, null);
            gatewayMock.SetupSequence(g => g.GetChildItem(rootName, new DirectoryId(@"\")))
                .Returns(rootDirectoryItems)
                .Returns(rootDirectoryItemsRefreshed)
                .Throws(new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, @"Redundant access to \")));
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Get-ChildItem -Path X:\",
                @"Get-ChildItem -Path X:\ -Force"
            );

            Assert.AreEqual(rootDirectoryItemsRefreshed.Length, result.Count, "Unexpected number of results");
            CollectionAssert.AreEquivalent(rootDirectoryItemsRefreshed, result.Select(p => p.BaseObject).Cast<FileSystemInfoContract>().ToList());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        [TestMethod]
        public void GetChildItem_WherePathIsSubDirectoryAndForceIsSpecified_RefreshesResult()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;
            var subDirectoryItemsRefreshed = fileSystemFixture.SubDirectoryItems
                .Select(f => f is FileInfoContract
                    ? new FileInfoContract(f.Id.Value.Insert(f.Id.Value.IndexOf(".ext", StringComparison.Ordinal), "Refreshed"), f.Name.Insert(f.Name.IndexOf(".ext", StringComparison.Ordinal), "Refreshed"), f.Created, f.Updated, ((FileInfoContract)f).Size, ((FileInfoContract)f).Hash) as FileSystemInfoContract
                    : new DirectoryInfoContract(f.Id + "Refreshed", f.Name + "Refreshed", f.Created, f.Updated) as FileSystemInfoContract)
                .ToArray();

            var gatewayMock = new MockingFixture().InitializeGetChildItems(rootName, string.Empty, rootDirectoryItems);
            gatewayMock.SetupSequence(g => g.GetChildItem(rootName, new DirectoryId(@"\SubDir")))
                .Returns(subDirectoryItems)
                .Returns(subDirectoryItemsRefreshed)
                .Throws(new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, @"Redundant access to \SubDir")));
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Get-ChildItem -Path X:\SubDir",
                @"Get-ChildItem -Path X:\SubDir -Force"
            );

            Assert.AreEqual(subDirectoryItemsRefreshed.Length, result.Count, "Unexpected number of results");
            CollectionAssert.AreEquivalent(subDirectoryItemsRefreshed, result.Select(p => p.BaseObject).Cast<FileSystemInfoContract>().ToList());
        }
    }
}