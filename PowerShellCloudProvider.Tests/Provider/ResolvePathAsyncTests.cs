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
using System.Management.Automation;
using IgorSoft.PowerShellCloudProvider.Tests.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IgorSoft.PowerShellCloudProvider.Tests.Provider
{
    [TestClass]
    public class ResolvePathAsyncTests
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
        public void ResolvePath_WhereBasePathIsRoot_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\", rootDirectoryItems);
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(new string[] {
                FileSystemFixture.NewDriveCommand,
                @"Resolve-Path X:\*"
            });

            Assert.AreEqual(rootDirectoryItems.Length, result.Count, "Unexpected number of results");
            CollectionAssert.AllItemsAreInstancesOfType(result.Select(i => i.BaseObject).ToList(), typeof(PathInfo), "Result is not of type PathInfo");
        }

        [TestMethod]
        public void ResolvePath_WhereBasePathIsSubDirectoryAndPatternContainsPrefix_CallsGatewayCorrectly()
        {
            var rootName = FileSystemFixture.GetRootName();
            var rootDirectoryItems = fileSystemFixture.RootDirectoryItems;
            var subDirectoryItems = fileSystemFixture.SubDirectoryItems;

            var gatewayMock = mockingFixture.InitializeGetChildItemsAsync(rootName, @"\SubDir", rootDirectoryItems, subDirectoryItems);
            compositionFixture.ExportAsyncGateway(gatewayMock.Object);

            var result = new PipelineFixture().Invoke(new string[] {
                FileSystemFixture.NewDriveCommand,
                @"Resolve-Path X:\SubDir\Sub*"
            });

            Assert.AreEqual(subDirectoryItems.Count(i => i.Name.StartsWith("Sub", StringComparison.Ordinal)), result.Count, "Unexpected number of results");
            CollectionAssert.AllItemsAreInstancesOfType(result.Select(i => i.BaseObject).ToList(), typeof(PathInfo), "Result is not of type PathInfo");
            CollectionAssert.AreEqual(subDirectoryItems.Where(i => i.Name.StartsWith("Sub", StringComparison.Ordinal)).Select(i => @"X:" + i.Id).ToList(), result.Select(i => ((PathInfo)i.BaseObject).Path).ToList());
        }
    }
}