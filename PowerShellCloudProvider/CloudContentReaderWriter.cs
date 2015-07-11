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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Management.Automation.Provider;
using System.Text;
using IgorSoft.PowerShellCloudProvider.Parameters;
using Microsoft.PowerShell.Commands;

namespace IgorSoft.PowerShellCloudProvider
{
    internal sealed class CloudContentReaderWriter : IContentReader, IContentWriter
    {
        private Stream stream;

        private BinaryReader binaryReader;

        private BinaryWriter binaryWriter;

        private StreamReader streamReader;

        private StreamWriter streamWriter;

        private Encoding encoding;

        private Action<Stream> continuation;

        public CloudContentReaderWriter(Stream stream, CloudContentReaderWriterParameters parameters)
        {
            this.stream = stream;
            encoding = parameters != null ? ToTextEncoding(parameters.Encoding) : null;

            if (encoding == null)
                binaryReader = new BinaryReader(stream);
            else
                streamReader = new StreamReader(stream, encoding);
        }

        public CloudContentReaderWriter(Action<Stream> continuation, CloudContentReaderWriterParameters parameters)
        {
            this.stream = new MemoryStream();
            encoding = parameters != null ? ToTextEncoding(parameters.Encoding) : null;

            if (encoding == null)
                binaryWriter = new BinaryWriter(stream, Encoding.Default, true);
            else
                streamWriter = new StreamWriter(stream, encoding, 1024, true);

            this.continuation = continuation;
        }

        private static Encoding ToTextEncoding(FileSystemCmdletProviderEncoding providerEncoding)
        {
            switch (providerEncoding) {
                case FileSystemCmdletProviderEncoding.Unknown:
                case FileSystemCmdletProviderEncoding.Default:
                    return Encoding.Default;
                case FileSystemCmdletProviderEncoding.Ascii:
                    return Encoding.ASCII;
                case FileSystemCmdletProviderEncoding.BigEndianUnicode:
                    return Encoding.BigEndianUnicode;
                case FileSystemCmdletProviderEncoding.Byte:
                    return null;
                case FileSystemCmdletProviderEncoding.String:
                case FileSystemCmdletProviderEncoding.Unicode:
                    return Encoding.Unicode;
                case FileSystemCmdletProviderEncoding.UTF32:
                    return Encoding.UTF32;
                case FileSystemCmdletProviderEncoding.UTF7:
                    return Encoding.UTF7;
                case FileSystemCmdletProviderEncoding.UTF8:
                    return Encoding.UTF8;
                default:
                    throw new ArgumentOutOfRangeException(string.Format(CultureInfo.CurrentCulture, Resources.UnsupportedEncoding, providerEncoding));
            }
        }

        public void Close()
        {
            if (binaryReader != null) {
                binaryReader.Close();
                binaryReader = null;
            }

            if (binaryWriter != null) {
                binaryWriter.Close();
                binaryWriter = null;
            }

            if (streamReader != null) {
                streamReader.Close();
                streamReader = null;
            }

            if (streamWriter != null) {
                streamWriter.Close();
                streamWriter = null;
            }

            if (continuation != null) {
                stream.Flush();
                stream.Seek(0, SeekOrigin.Begin);
                continuation(stream);
                continuation = null;
            }

            stream.Close();
            stream = null;
        }

        public void Dispose()
        {
            Close();
        }

        public IList Read(long readCount)
        {
            if (readCount < 0)
                throw new ArgumentOutOfRangeException(Resources.NegativeReadCount);

            return encoding != null ? (IList)ReadByLine(readCount) : (IList)ReadByteEncoded(readCount);
        }

        private string[] ReadByLine(long readCount)
        {
            if (streamReader.EndOfStream)
                return new string[0];

            var buffer = new List<string>();

            do {
                var lineBuffer = new StringBuilder();

                do {
                    var c = (char)streamReader.Read();
                    if (c == '\n')
                        break;
                    if (c == '\r') {
                        if ((char)streamReader.Peek() == '\n')
                            streamReader.Read();
                        break;
                    }

                    lineBuffer.Append(c);
                } while (!streamReader.EndOfStream);

                buffer.Add(lineBuffer.ToString());
            } while (!streamReader.EndOfStream && (readCount == 0 || --readCount > 0));

            return buffer.ToArray();
        }

        private byte[] ReadByteEncoded(long readCount)
        {
            long chunkSize = readCount == 0 ? stream.Length - stream.Position : Math.Min(readCount, stream.Length - stream.Position);
            var buffer = new byte[chunkSize];
            binaryReader.Read(buffer, 0, (int)chunkSize);

            return buffer;
        }

        public void Seek(long offset, SeekOrigin origin)
        {
            stream.Seek(offset, origin);
        }

        public IList Write(IList content)
        {
            if (encoding != null)
                return WriteByLine(content);
            else
                return WriteByteEncoded(content);
        }

        private IList WriteByLine(IList content)
        {
            foreach (string s in content)
                streamWriter.WriteLine(s);

            return content;
        }

        private IList WriteByteEncoded(IList content)
        {
            foreach (byte b in content)
                binaryWriter.Write(b);

            return content;
        }
    }
}
