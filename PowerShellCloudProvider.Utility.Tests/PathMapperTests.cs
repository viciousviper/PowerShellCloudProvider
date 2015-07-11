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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IgorSoft.PowerShellCloudProvider.Utility.Tests
{
    [TestClass]
    [DeploymentItem(DEPLOYMENT_ITEM_PATH)]
    public class PathMapperTest
    {
        private const string DEPLOYMENT_ITEM_PATH = @"\PathMapperTests.cs.xml";

        private const string PROVIDER_INVARIANT_NAME = "Microsoft.VisualStudio.TestTools.DataSource.XML";

        private const string CONNECTION_STRING = @"|DataDirectory|\PathMapperTests.cs.xml";

        private class DefaultTestRecord
        {
            public string Replacement { get; }

            public string Input { get; }

            public string Expected { get; }

            public DefaultTestRecord(TestContext context)
            {
                var row = context.DataRow;

                Replacement = row["Replacement"].ToString();
                Input = row["Input"].ToString();
                Expected = row["Expected"].ToString();
            }
        }

        private class CustomTestRecord : DefaultTestRecord
        {
            public string Expression { get; }

            public CustomTestRecord(TestContext context) : base(context)
            {
                var row = context.DataRow;

                Expression = row["Expression"].ToString();
            }
        }

        /// <summary>
        /// Gets or sets the test context.
        /// </summary>
        /// <value>
        /// the test context which provides information about and functionality for the current test run.
        /// </value>
        public TestContext TestContext { get; set; }

        [TestMethod]
        [DataSource(PROVIDER_INVARIANT_NAME, CONNECTION_STRING, "DefaultPattern", DataAccessMethod.Sequential)]
        public void PathMapper_MapPathUsingDefaultPattern()
        {
            var record = new DefaultTestRecord(TestContext);

            var sut = new PathMapper(null, record.Replacement);
            var output = sut.MapPath(record.Input);

            Assert.AreEqual(record.Expected, output, "Unexpected output for replacement '{0}' on input '{1}'", record.Replacement, record.Input);
        }

        [TestMethod]
        [DataSource(PROVIDER_INVARIANT_NAME, CONNECTION_STRING, "CustomPattern", DataAccessMethod.Sequential)]
        public void PathMapper_MapPathUsingCustomPattern()
        {
            var record = new CustomTestRecord(TestContext);

            var sut = new PathMapper(record.Expression, record.Replacement);
            var output = sut.MapPath(record.Input);

            Assert.AreEqual(record.Expected, output, "Unexpected output for expression '{0}' and replacement '{1}' on input '{2}'", record.Expression, record.Replacement, record.Input);
        }
    }
}
