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
using System.Management.Automation;
using IgorSoft.PowerShellCloudProvider.Interface.Composition;
using IgorSoft.PowerShellCloudProvider.Tests.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace IgorSoft.PowerShellCloudProvider.Tests.Drive
{
    [TestClass]
    public class NewDriveTests
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
        public void NewDrive_WhereGatewayIsExported_PerformsComposition()
        {
            var gatewayMock = new Mock<ICloudGateway>(MockBehavior.Strict).Object;

            compositionFixture.ExportGateway(gatewayMock);

            new PipelineFixture().Invoke(string.Format(CultureInfo.InvariantCulture,
                "New-PSDrive -PSProvider {0} -Name X -Root '{1}' -Description '{2}' -ApiKey {3} -EncryptionKey {4}",
                CloudProvider.PROVIDER_NAME, CompositionFixture.MOCKGATEWAY_NAME + "|" + root, description, apiKey, encryptionKey)
            );
        }

        [TestMethod]
        public void NewDrive_WhereCredentialsAreEmpty_CreatesDrive()
        {
            var gatewayMock = new Mock<ICloudGateway>(MockBehavior.Strict).Object;

            compositionFixture.ExportGateway(gatewayMock);

            var result = new PipelineFixture().Invoke(new string[] {
                string.Format(CultureInfo.InvariantCulture,
                    "New-PSDrive -PSProvider {0} -Name X -Root '{1}' -Description '{2}' -ApiKey {3} -EncryptionKey {4}",
                    CloudProvider.PROVIDER_NAME, CompositionFixture.MOCKGATEWAY_NAME + "|" + root, description, apiKey, encryptionKey),
                string.Format(CultureInfo.InvariantCulture,
                    "Get-PSDrive -PSProvider {0}", CloudProvider.PROVIDER_NAME)
            });

            Assert.AreEqual(1, result.Count, "Unexpected number of results");
            Assert.IsInstanceOfType(result[0].BaseObject, typeof(PSDriveInfo), "Result is not of type PSDriveInfo");

            var driveInfo = result[0].BaseObject as PSDriveInfo;
            Assert.AreEqual("X:", driveInfo.Root, "Unexpected root");
            Assert.AreEqual(CompositionFixture.MOCKGATEWAY_NAME, ((CloudDrive)driveInfo).DisplayRoot, "Unexpected display root");
            Assert.AreEqual(description, driveInfo.Description, "Unexpected description");
        }

        [TestMethod]
        public void NewDrive_WhereCredentialsAreSpecified_CreatesDrive()
        {
            var gatewayMock = new Mock<ICloudGateway>(MockBehavior.Strict).Object;

            compositionFixture.ExportGateway(gatewayMock);

            var pipelineFixture = new PipelineFixture();
            pipelineFixture.SetVariable("credential", PipelineFixture.GetCredential("TestUser", "TestPassword"));
            var result = pipelineFixture.Invoke(new string[] {
                string.Format(CultureInfo.InvariantCulture,
                    "New-PSDrive -PSProvider {0} -Name Y -Root '{1}' -Description '{2}' -Credential $credential -ApiKey {3} -EncryptionKey {4}",
                    CloudProvider.PROVIDER_NAME, CompositionFixture.MOCKGATEWAY_NAME + "|" + root, description, apiKey, encryptionKey),
                string.Format(CultureInfo.InvariantCulture,
                    "Get-PSDrive -PSProvider {0}", CloudProvider.PROVIDER_NAME)
            });

            Assert.AreEqual(1, result.Count, "Unexpected number of results");
            Assert.IsInstanceOfType(result[0].BaseObject, typeof(PSDriveInfo), "Results is not of type PSDriveInfo");

            var driveInfo = result[0].BaseObject as PSDriveInfo;
            Assert.AreEqual("Y:", driveInfo.Root, "Unexpected root");
            Assert.AreEqual(CompositionFixture.MOCKGATEWAY_NAME + "@TestUser", ((CloudDrive)driveInfo).DisplayRoot, "Unexpected display root");
            Assert.AreEqual(description, driveInfo.Description, "Unexpected description");
        }

        [TestMethod]
        public void NewDrive_WhereGatewayIsMissing_Throws()
        {
            var gatewayMock = new Mock<ICloudGateway>(MockBehavior.Strict).Object;

            compositionFixture.ExportGateway(gatewayMock);

            try {
                new PipelineFixture().Invoke(string.Format(CultureInfo.InvariantCulture,
                    "New-PSDrive -PSProvider {0} -Name X -Root '{1}' -Description '{2}' -ApiKey {3} -EncryptionKey {4}",
                    CloudProvider.PROVIDER_NAME, "unknown|" + root, description, apiKey, encryptionKey)
                );
            } catch (AssertFailedException ex) {
                Assert.IsTrue(ex.Message.EndsWith("InvalidOperation: (:) [New-PSDrive], KeyNotFoundException", StringComparison.Ordinal));
            }
        }
    }
}