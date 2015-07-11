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
using IgorSoft.PowerShellCloudProvider.Interface.Composition;
using IgorSoft.PowerShellCloudProvider.Tests.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace IgorSoft.PowerShellCloudProvider.Tests.Drive
{
    [TestClass]
    public class RemoveDriveAsyncTests
    {
        private const string root = @"\";

        private const string description = "Test Drive";

        private const string apiKey = "abcdefghi";

        private const string encryptionKey = "01234567";

        private CompositionFixture compositionFixture;

        [TestInitialize]
        public void Initialize()
        {
            compositionFixture = new CompositionFixture();
        }

        [TestCleanup]
        public void Cleanup()
        {
            compositionFixture = null;
        }

        [TestMethod]
        public void RemoveDriveAsync_RemovesDrive()
        {
            var gatewayMock = new Mock<IAsyncCloudGateway>(MockBehavior.Strict).Object;

            compositionFixture.ExportAsyncGateway(gatewayMock);

            var pipelineFixture = new PipelineFixture();
            var result = pipelineFixture.Invoke(new string[] {
                string.Format(CultureInfo.InvariantCulture,
                    "New-PSDrive -PSProvider {0} -Name X -Root '{1}' -Description '{2}' -ApiKey {3} -EncryptionKey {4}",
                    CloudProvider.PROVIDER_NAME, CompositionFixture.MOCKGATEWAY_NAME + "|" + root, description, apiKey, encryptionKey),
                "Remove-PSDrive -Name X",
                string.Format(CultureInfo.InvariantCulture,
                    "Get-PSDrive -PSProvider {0}", CloudProvider.PROVIDER_NAME)
            });

            Assert.AreEqual(0, result.Count, "Unexpected number of results");
        }
    }
}