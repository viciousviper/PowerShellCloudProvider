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
using System.Text.RegularExpressions;
using IgorSoft.PowerShellCloudProvider.Interface;
using IgorSoft.PowerShellCloudProvider.Interface.Composition;
using IgorSoft.PowerShellCloudProvider.Interface.IO;
using IgorSoft.PowerShellCloudProvider.Tests.MoqExtensions;
using Moq;

namespace IgorSoft.PowerShellCloudProvider.Tests.Fixtures
{
    internal class MockingFixture
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Language", "CSE0003:Use expression-bodied members")]
        public Mock<ICloudGateway> InitializeGetChildItems(RootName rootName, string path, params IEnumerable<FileSystemInfoContract>[] pathChildItems)
        {
            return InitializeGetChildItemsWithFilter(rootName, path, null, pathChildItems);
        }

        public Mock<ICloudGateway> InitializeGetChildItemsWithFilter(RootName rootName, string path, string filter, params IEnumerable<FileSystemInfoContract>[] pathChildItems)
        {
            var gatewayMock = new Mock<ICloudGateway>(MockBehavior.Strict);
            gatewayMock.SetupSequence(g => g.GetRoot(rootName, FileSystemFixture.ApiKey))
                .Returns(FileSystemFixture.Root)
                .Throws(new InvalidOperationException(@"Redundant call to GetRoot()"));
            gatewayMock.SetupSequence(g => g.GetDrive(rootName, FileSystemFixture.ApiKey))
                .Returns(FileSystemFixture.Drive)
                .Throws(new InvalidOperationException(@"Redundant call to GetDrive()"));

            if (path != null) {
                path = path + Path.DirectorySeparatorChar;
                for (int i = 0, j = 0; i >= 0; i = path.IndexOf(Path.DirectorySeparatorChar, Math.Min(path.Length, i + 2)), ++j) {
                    var currentPath = new DirectoryId(path.Substring(0, Math.Max(1, i)));

                    bool applyFilter = j == pathChildItems.Length - 1;
                    var effectiveFilter = applyFilter && filter != null ? new Regex("^" + filter.Replace("*", ".*") + "$") : null;
                    gatewayMock.SetupSequence(g => g.GetChildItem(rootName, currentPath))
                        .Returns(applyFilter ? pathChildItems[j].Where(f => effectiveFilter == null || effectiveFilter.IsMatch(f.Name)) : pathChildItems[j])
                        .Throws(new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, @"Redundant access to {0}", currentPath)));
                }
            }

            return gatewayMock;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Language", "CSE0003:Use expression-bodied members")]
        public Mock<IAsyncCloudGateway> InitializeGetChildItemsAsync(RootName rootName, string path, params IEnumerable<FileSystemInfoContract>[] pathChildItems)
        {
            return InitializeGetChildItemsAsyncWithFilter(rootName, path, null, pathChildItems);
        }

        public Mock<IAsyncCloudGateway> InitializeGetChildItemsAsyncWithFilter(RootName rootName, string path, string filter, params IEnumerable<FileSystemInfoContract>[] pathChildItems)
        {
            var gatewayMock = new Mock<IAsyncCloudGateway>(MockBehavior.Strict);
            gatewayMock.SetupSequence(g => g.GetRootAsync(rootName, FileSystemFixture.ApiKey))
                .ReturnsAsync(FileSystemFixture.Root)
                .ThrowsAsync(new InvalidOperationException("Redundant call to GetRoot()"));
            gatewayMock.SetupSequence(g => g.GetDriveAsync(rootName, FileSystemFixture.ApiKey))
                .ReturnsAsync(FileSystemFixture.Drive)
                .Throws(new InvalidOperationException("Redundant call to GetDrive()"));

            if (path != null) {
                path = path + Path.DirectorySeparatorChar;
                for (int i = 0, j = 0; i >= 0; i = path.IndexOf(Path.DirectorySeparatorChar, Math.Min(path.Length, i + 2)), ++j) {
                    var currentPath = new DirectoryId(path.Substring(0, Math.Max(1, i)));

                    bool applyFilter = j == pathChildItems.Length - 1;
                    var effectiveFilter = applyFilter && filter != null ? new Regex("^" + filter.Replace("*", ".*") + "$") : null;
                    gatewayMock.SetupSequence(g => g.GetChildItemAsync(rootName, currentPath))
                        .ReturnsAsync(applyFilter ? pathChildItems[j].Where(f => effectiveFilter == null || effectiveFilter.IsMatch(f.Name)) : pathChildItems[j])
                        .ThrowsAsync(new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Redundant access to {0}", currentPath)));
                }
            }

            return gatewayMock;
        }
    }
}
