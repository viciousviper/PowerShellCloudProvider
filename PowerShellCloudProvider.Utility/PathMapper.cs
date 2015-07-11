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
using System.Text;
using System.Text.RegularExpressions;

namespace IgorSoft.PowerShellCloudProvider.Utility
{
    internal class PathMapper
    {
        private const string GROUP_DRIVE = "Drive";

        private const string GROUP_PATH = "Path";

        private const string GROUP_FILENAME = "Filename";

        private const string GROUP_FILENAMEWITHOUTEXTENSION = "FilenameWithoutExtension";

        private const string GROUP_EXTENSION = "Extension";

        private const string GROUP_NUMERIC = "Numeric";

        private const string GROUP_TBS = "TBS";

        private static readonly Regex fileNameRegex = new Regex(@"(?<Drive>[a-zA-Z]:\\)?(?<Path>.*\\)?(?<Filename>(?<FilenameWithoutExtension>.+)(?<Extension>\.\w+)|(?<FilenameWithoutExtension>.+))", RegexOptions.Compiled);

        private static readonly Regex numericPartRegex = new Regex(@".*[^\d](?<Numeric>\d{3,})(?:\.\w+)", RegexOptions.Compiled);

        private static readonly Regex tenBasedSubdirectoriesPartRegex = new Regex(@"\$\{TBS(?::\d)?\}", RegexOptions.Compiled);

        private Regex expression;

        private string replacement;

        public PathMapper(string expression, string replacement)
        {
            if (replacement == null)
                throw new ArgumentNullException(nameof(replacement));

            this.expression = !string.IsNullOrEmpty(expression) ? new Regex(expression, RegexOptions.Compiled) : PathMapper.fileNameRegex;
            this.replacement = replacement;
        }

        internal string MapPath(string inputPath)
        {
            string result = this.expression.Replace(inputPath, this.replacement);
            var numericMatch = PathMapper.numericPartRegex.Match(result);
            return numericMatch.Success 
                ? PathMapper.tenBasedSubdirectoriesPartRegex.Replace(result, PathMapper.TenBasedSubdirectories(numericMatch.Groups["Numeric"].Value))
                : PathMapper.tenBasedSubdirectoriesPartRegex.Replace(result, string.Empty);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Language", "CSE0003:Use expression-bodied members")]
        internal static MatchEvaluator TenBasedSubdirectories(string number)
        {
            return delegate (Match m) {
                string tbsPattern = m.Value;
                int digits;
                if (!int.TryParse(tbsPattern.Substring(tbsPattern.Length - 2, 1), out digits))
                    digits = 2;

                var builder = new StringBuilder();
                int num;
                for (int i = 1; i <= number.Length - digits; i = num) {
                    builder.AppendFormat(CultureInfo.InvariantCulture, @"{0}\", number.Substring(0, i).PadRight(number.Length, '0'));
                    num = i + 1;
                }
                return builder.ToString();
            };
        }
    }
}
