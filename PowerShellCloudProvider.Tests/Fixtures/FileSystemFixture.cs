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
using System.Security.Cryptography;
using IgorSoft.PowerShellCloudProvider.Interface;
using IgorSoft.PowerShellCloudProvider.Interface.IO;

namespace IgorSoft.PowerShellCloudProvider.Tests.Fixtures
{
    internal class FileSystemFixture
    {
        private static SHA1 sha1 = SHA1.Create();

        public static string Description => "Test Drive";

        public static string ApiKey => "abcdefghi";

        public static string EncryptionKey => "01234567";

        public static string NewDriveCommand => string.Format(CultureInfo.InvariantCulture,
            "New-PSDrive -PSProvider {0} -Name X -Root '{1}' -Description '{2}' -ApiKey {3}",
            CloudProvider.PROVIDER_NAME, CompositionFixture.MOCKGATEWAY_NAME, Description, ApiKey);

        public static string NewDriveCommandWithCredential => string.Format(CultureInfo.InvariantCulture,
            "New-PSDrive -PSProvider {0} -Name Y -Root '{1}' -Description '{2}' -Credential $credential -ApiKey {3}",
            CloudProvider.PROVIDER_NAME, CompositionFixture.MOCKGATEWAY_NAME, Description, ApiKey);

        public static string NewDriveCommandWithEncryptionKey => string.Format(CultureInfo.InvariantCulture,
            "New-PSDrive -PSProvider {0} -Name X -Root '{1}' -Description '{2}' -ApiKey {3} -EncryptionKey {4}",
            CloudProvider.PROVIDER_NAME, CompositionFixture.MOCKGATEWAY_NAME, Description, ApiKey, EncryptionKey);

        public static RootName GetRootName(string user = "") => new RootName(CompositionFixture.MOCKGATEWAY_NAME + user);

        public static DriveInfoContract Drive => new DriveInfoContract("X", 6000000, 4000000);

        public static RootDirectoryInfoContract Root => new RootDirectoryInfoContract(@"\", DateTimeOffset.Now, DateTimeOffset.Now);

        public FileSystemInfoContract[] RootDirectoryItems { get; } = new FileSystemInfoContract[] {
                new DirectoryInfoContract(@"\SubDir", "SubDir", ToDateTime("2015-01-01 10:11:12"), ToDateTime("2015-01-01 20:21:22")),
                new DirectoryInfoContract(@"\SubDir2", "SubDir2", ToDateTime("2015-01-01 13:14:15"), ToDateTime("2015-01-01 23:24:25")),
                new FileInfoContract(@"\File.ext", "File.ext", ToDateTime("2015-01-02 10:11:12"), ToDateTime("2015-01-02 20:21:22"), 16384, GetHash("16384")),
                new FileInfoContract(@"\SecondFile.ext", "SecondFile.ext", ToDateTime("2015-01-03 10:11:12"), ToDateTime("2015-01-03 20:21:22"), 32768, GetHash("32768")),
                new FileInfoContract(@"\ThirdFile.ext", "ThirdFile.ext", ToDateTime("2015-01-04 10:11:12"), ToDateTime("2015-01-04 20:21:22"), 65536, GetHash("65536"))
            };

        public FileSystemInfoContract[] SubDirectoryItems { get; } = new FileSystemInfoContract[] {
                new DirectoryInfoContract(@"\SubDir\SubSubDir", "SubSubDir", ToDateTime("2015-02-01 10:11:12"), ToDateTime("2015-02-01 20:21:22")),
                new FileInfoContract(@"\SubDir\SubFile.ext", "SubFile.ext", ToDateTime("2015-02-02 10:11:12"), ToDateTime("2015-02-02 20:21:22"), 981256915, GetHash("981256915")),
                new FileInfoContract(@"\SubDir\SecondSubFile.ext", "SecondSubFile.ext", ToDateTime("2015-02-03 10:11:12"), ToDateTime("2015-02-03 20:21:22"), 30858025, GetHash("30858025")),
                new FileInfoContract(@"\SubDir\ThirdSubFile.ext", "ThirdSubFile.ext", ToDateTime("2015-02-04 10:11:12"), ToDateTime("2015-02-04 20:21:22"), 45357, GetHash("45357"))
            };

        public FileSystemInfoContract[] SubDirectory2Items { get; } = new FileSystemInfoContract[] {
                new DirectoryInfoContract(@"\SubDir2\SubSubDir2", "SubSubDir2", ToDateTime("2015-02-01 10:11:12"), ToDateTime("2015-02-01 20:21:22")),
                new FileInfoContract(@"\SubDir2\SubFile2.ext", "SubFile2.ext", ToDateTime("2015-02-02 10:11:12"), ToDateTime("2015-02-02 20:21:22"), 981256915, GetHash("981256915")),
                new FileInfoContract(@"\SubDir2\SecondSubFile2.ext", "SecondSubFile2.ext", ToDateTime("2015-02-03 10:11:12"), ToDateTime("2015-02-03 20:21:22"), 30858025, GetHash("30858025")),
                new FileInfoContract(@"\SubDir2\ThirdSubFile2.ext", "ThirdSubFile2.ext", ToDateTime("2015-02-04 10:11:12"), ToDateTime("2015-02-04 20:21:22"), 45357, GetHash("45357"))
            };

        public FileSystemInfoContract[] SubSubDirectoryItems { get; } = new FileSystemInfoContract[] {
                new FileInfoContract(@"\SubDir\SubSubDir\SubSubFile.ext", "SubSubFile.ext", ToDateTime("2015-03-01 10:11:12"), ToDateTime("2015-03-01 20:21:22"), 7198265, GetHash("7198265")),
                new FileInfoContract(@"\SubDir\SubSubDir\SecondSubSubFile.ext", "SecondSubSubFile.ext", ToDateTime("2015-03-02 10:11:12"), ToDateTime("2015-03-02 20:21:22"), 5555, GetHash("5555")),
                new FileInfoContract(@"\SubDir\SubSubDir\ThirdSubSubFile.ext", "ThirdSubSubFile.ext", ToDateTime("2015-03-03 10:11:12"), ToDateTime("2015-03-03 20:21:22"), 102938576, GetHash("102938576"))
            };

        public static string GetHash(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            byte[] byteValue = value.ToCharArray().Select(c => (byte)c).ToArray();

            var hashCode = sha1.ComputeHash(byteValue);

            return BitConverter.ToString(hashCode).Replace("-", string.Empty);
        }

        private static DateTimeOffset ToDateTime(string value) => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
    }
}
