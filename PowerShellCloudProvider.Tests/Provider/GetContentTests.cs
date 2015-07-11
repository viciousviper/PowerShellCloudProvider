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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using IgorSoft.PowerShellCloudProvider.Interface.IO;
using IgorSoft.PowerShellCloudProvider.Tests.Fixtures;

namespace IgorSoft.PowerShellCloudProvider.Tests.Provider
{
    [TestClass]
    public class GetContentTests
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
        public void GetContent_WhereNodeIsFileAndEncodingIsByte_ReturnsContent()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var content = new MemoryStream(TestContent.MultiLineTestContent.Select(c => Convert.ToByte(c)).ToArray());

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\", rootDirectoryItems);
            gatewayMock.SetupSequence(g => g.GetContent(rootName, new FileId(@"\File.ext")))
                .Returns(content)
                .Throws(new InvalidOperationException(@"Redundant access to \File.ext"));
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Get-Content -Path X:\File.ext -Encoding Byte"
            );

            CollectionAssert.AreEqual(content.ToArray(), result.Select(p => p.BaseObject).ToArray(), "Mismatching content");
        }

        [TestMethod]
        public void GetContent_WhereNodeIsFileAndEncodingIsByteAndReadCountIsZero_ReturnsContent()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var content = new MemoryStream(TestContent.MultiLineTestContent.Select(c => Convert.ToByte(c)).ToArray());

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\", rootDirectoryItems);
            gatewayMock.SetupSequence(g => g.GetContent(rootName, new FileId(@"\File.ext")))
                .Returns(content)
                .Throws(new InvalidOperationException(@"Redundant access to \File.ext"));
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Get-Content -Path X:\File.ext -Encoding Byte -ReadCount 0"
            );

            Assert.AreEqual(1, result.Count, "Unexpected number of results");
            Assert.IsInstanceOfType(result[0].BaseObject, typeof(byte[]), "Results is not of type byte[]");
            CollectionAssert.AreEqual(content.ToArray(), result[0].BaseObject as byte[], "Mismatching content");
        }

        [TestMethod]
        public void GetContent_WhereNodeIsFileAndEncodingIsUnknown_ReturnsContent()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var content = new MemoryStream(TestContent.MultiLineTestContent.Select(c => Convert.ToByte(c)).ToArray());

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\", rootDirectoryItems);
            gatewayMock.SetupSequence(g => g.GetContent(rootName, new FileId(@"\File.ext")))
                .Returns(content)
                .Throws(new InvalidOperationException(@"Redundant access to \File.ext"));
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Get-Content -Path X:\File.ext"
            );

            CollectionAssert.AreEqual(TestContent.MultiLineTestContent.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries), result.Select(p => p.BaseObject).ToArray(), "Mismatching content");
        }

        [TestMethod]
        public void GetContent_WhereNodeIsFileAndEncodingIsUnknownAndReadCountIsZero_ReturnsContent()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var content = new MemoryStream(TestContent.MultiLineTestContent.Select(c => Convert.ToByte(c)).ToArray());

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\", rootDirectoryItems);
            gatewayMock.SetupSequence(g => g.GetContent(rootName, new FileId(@"\File.ext")))
                .Returns(content)
                .Throws(new InvalidOperationException(@"Redundant access to \File.ext"));
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Get-Content -Path X:\File.ext -ReadCount 0"
            );

            Assert.AreEqual(1, result.Count, "Unexpected number of results");
            Assert.IsInstanceOfType(result[0].BaseObject, typeof(string[]), "Results is not of type string[]");
            CollectionAssert.AreEqual(TestContent.MultiLineTestContent.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries), (string[])result[0].BaseObject, "Mismatching content");
        }

        [TestMethod]
        public void GetContent_WhereNodeIsFileAndEncryptionKeyIsSpecified_ReturnsDecryptedContent()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var content = new MemoryStream(TestContent.MultiLineTestContent.Select(c => Convert.ToByte(c)).ToArray());

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\", rootDirectoryItems);
            gatewayMock.SetupSequence(g => g.GetContent(rootName, new FileId(@"\File.ext")))
                .Returns(content.Encrypt(FileSystemFixture.EncryptionKey))
                .Throws(new InvalidOperationException(@"Redundant access to \File.ext"));
            compositionFixture.ExportGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(
                FileSystemFixture.NewDriveCommandWithEncryptionKey,
                @"Get-Content -Path X:\File.ext -ReadCount 0"
            );

            Assert.AreEqual(1, result.Count, "Unexpected number of results");
            Assert.IsInstanceOfType(result[0].BaseObject, typeof(string[]), "Results is not of type string[]");
            CollectionAssert.AreEqual(TestContent.MultiLineTestContent.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries), (string[])result[0].BaseObject, "Mismatching content");
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void GetContent_WhereNodeIsDirectory_Throws()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\", rootDirectoryItems);
            gatewayMock.SetupSequence(g => g.GetContent(rootName, new FileId(@"\SubDir")))
                .Throws(new NotSupportedException(@"Access to path \SubDir is denied"));
            compositionFixture.ExportGateway(gatewayMock.Object);

            try {
                new PipelineFixture().Invoke(
                    FileSystemFixture.NewDriveCommand,
                    @"Get-Content -Path X:\SubDir"
                );
            } catch (CmdletInvocationException ex) {
                throw ex.InnerException;
            }
        }

        [TestMethod]
        public void GetContent_WherePathIsUnknownFile_Throws()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;

            var gatewayMock = mockingFixture.InitializeGetChildItems(rootName, @"\", rootDirectoryItems);
            compositionFixture.ExportGateway(gatewayMock.Object);

            try {
                new PipelineFixture().Invoke(
                    FileSystemFixture.NewDriveCommand,
                    @"Get-Content -Path X:\FileUnknown"
                );

                throw new InvalidOperationException("Expected Exception was not thrown");
            } catch (AssertFailedException ex) {
                Assert.IsTrue(ex.Message.EndsWith("ObjectNotFound: (X:\\FileUnknown:String) [Get-Content], ItemNotFoundException", StringComparison.Ordinal));
            }

            gatewayMock.VerifyAll();
        }
    }
}