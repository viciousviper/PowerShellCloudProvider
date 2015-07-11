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
using Moq;

namespace IgorSoft.PowerShellCloudProvider.Tests.Provider
{
    [TestClass]
    public class SetContentAsyncTests
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
        public void SetContentAsync_WhereNodeIsFileAndEncodingIsByte_AcceptsContent()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var content = TestContent.MultiLineTestContent.Select(c => Convert.ToByte(c)).ToArray();

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.ClearContentAsync(rootName, new FileId(@"\File.ext"), It.IsAny<Func<FileSystemInfoLocator>>()))
                .ReturnsAsync(true)
                .Verifiable();
            gatewayMock.Setup(g => g.SetContentAsync(rootName, new FileId(@"\File.ext"), It.Is<Stream>(s => new BinaryReader(s, System.Text.Encoding.Default, true).ReadBytes((int)s.Length).SequenceEqual(content)), It.Is<IProgress<ProgressValue>>(p => true), It.IsAny<Func<FileSystemInfoLocator>>()))
                .ReturnsAsync(true)
                .Verifiable();
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            var pipelineFixture = new PipelineFixture();
            pipelineFixture.SetVariable("value", content);
            pipelineFixture.Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Set-Content -Path X:\File.ext $value -Encoding Byte"
            );

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void SetContentAsync_WhereNodeIsFileAndEncodingIsUnknown_AcceptsContent()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var content = TestContent.MultiLineTestContent.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.ClearContentAsync(rootName, new FileId(@"\File.ext"), It.IsAny<Func<FileSystemInfoLocator>>()))
                .ReturnsAsync(true)
                .Verifiable();
            gatewayMock.Setup(g => g.SetContentAsync(rootName, new FileId(@"\File.ext"), It.Is<Stream>(s => new StreamReader(s, System.Text.Encoding.Default, true, 1024, true).ReadToEnd().TrimEnd('\r', '\n') == string.Join("\r\n", content)), It.Is<IProgress<ProgressValue>>(p => true), It.IsAny<Func<FileSystemInfoLocator>>()))
                .ReturnsAsync(true)
                .Verifiable();
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            var pipelineFixture = new PipelineFixture();
            pipelineFixture.SetVariable("value", content);
            pipelineFixture.Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Set-Content -Path X:\File.ext $value"
            );

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void SetContentAsync_WhereNodeIsFileAndPassThruIsSet_ReturnsContent()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var content = TestContent.MultiLineTestContent.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.ClearContentAsync(rootName, new FileId(@"\File.ext"), It.IsAny<Func<FileSystemInfoLocator>>()))
                .ReturnsAsync(true)
                .Verifiable();
            gatewayMock.Setup(g => g.SetContentAsync(rootName, new FileId(@"\File.ext"), It.Is<Stream>(s => new StreamReader(s, System.Text.Encoding.Default, true, 1024, true).ReadToEnd().TrimEnd('\r', '\n') == string.Join("\r\n", content)), It.Is<IProgress<ProgressValue>>(p => true), It.IsAny<Func<FileSystemInfoLocator>>()))
                .ReturnsAsync(true)
                .Verifiable();
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            var pipelineFixture = new PipelineFixture();
            pipelineFixture.SetVariable("value", content);
            var result = pipelineFixture.Invoke(
                FileSystemFixture.NewDriveCommand,
                @"Set-Content -Path X:\File.ext $value -PassThru"
            );

            Assert.AreEqual(1, result.Count, "Unexpected number of results");
            Assert.IsInstanceOfType(result[0].BaseObject, typeof(string[]), "Results is not of type string[]");
            CollectionAssert.AreEqual(content, (string[])result[0].BaseObject, "Mismatching content");

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        public void SetContentAsync_WhereNodeIsFileAndEncryptionKeyIsSpecified_EncryptsContent()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var content = TestContent.MultiLineTestContent.Select(c => Convert.ToByte(c)).ToArray();

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.ClearContentAsync(rootName, new FileId(@"\File.ext"), It.IsAny<Func<FileSystemInfoLocator>>()))
                .ReturnsAsync(true)
                .Verifiable();
            Func<Stream, byte[], bool> matches = (s, c) => {
                var d = s.Decrypt(FileSystemFixture.EncryptionKey);
                return new BinaryReader(d, System.Text.Encoding.Default, true).ReadBytes((int)d.Length).SequenceEqual(content);
            };
            gatewayMock.Setup(g => g.SetContentAsync(rootName, new FileId(@"\File.ext"), It.Is<Stream>(s => matches(s, content)), It.Is<IProgress<ProgressValue>>(p => true), It.IsAny<Func<FileSystemInfoLocator>>()))
                .Verifiable();
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            var pipelineFixture = new PipelineFixture();
            pipelineFixture.SetVariable("value", content);
            pipelineFixture.Invoke(
                FileSystemFixture.NewDriveCommandWithEncryptionKey,
                @"Set-Content -Path X:\File.ext $value -Encoding Byte"
            );

            gatewayMock.VerifyAll();
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void SetContentAsync_WhereNodeIsDirectory_Throws()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var content = TestContent.MultiLineTestContent.Select(c => Convert.ToByte(c)).ToArray();

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\", rootDirectoryItems);
            gatewayMock.Setup(g => g.SetContentAsync(rootName, new FileId(@"\SubDir"), It.Is<Stream>(s => new BinaryReader(s, System.Text.Encoding.Default, true).ReadBytes((int)s.Length).SequenceEqual(content)), null, It.IsAny<Func<FileSystemInfoLocator>>()))
                .ThrowsAsync(new NotSupportedException(@"Access to path \SubDir is denied"));
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            try {
                var pipelineFixture = new PipelineFixture();
                pipelineFixture.SetVariable("value", content);
                pipelineFixture.Invoke(
                    FileSystemFixture.NewDriveCommand,
                    @"Set-Content -Path X:\SubDir $value"
                );
            } catch (CmdletInvocationException ex) {
                throw ex.InnerException;
            }
        }

        [TestMethod]
        public void SetContentAsync_WherePathIsUnknownFile_Throws()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var content = TestContent.MultiLineTestContent.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\", rootDirectoryItems);
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            try {
                var pipelineFixture = new PipelineFixture();
                pipelineFixture.SetVariable("value", content);
                pipelineFixture.Invoke(
                    FileSystemFixture.NewDriveCommand,
                    @"Set-Content -Path X:\FileUnknown $value"
                );

                throw new InvalidOperationException("Expected Exception was not thrown");
            } catch (AssertFailedException ex) {
                Assert.IsTrue(ex.Message.EndsWith(@"ObjectNotFound: (X:\FileUnknown:String) [Set-Content], ItemNotFoundException", StringComparison.Ordinal));
            }

            gatewayMock.VerifyAll();
        }
    }
}