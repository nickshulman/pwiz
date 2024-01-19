/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2024 University of Washington - Seattle, WA
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System.IO;
using System.Text;

namespace pwiz.Common.SystemUtil
{
    /// <summary>
    /// TextReader which keeps track of which line it is on
    /// </summary>
    public class LineReader : TextReader
    {
        private long _textPosition;
        public LineReader(TextReader reader, long length)
        {
            TextReader = reader;
            Length = length;
        }
       
        protected TextReader TextReader { get; }

        /// <summary>
        /// Current position within the text being read. This position may either be a
        /// count of character or bytes, but will be the same units as <see cref="Length"/>.
        /// </summary>
        public virtual long Position
        {
            get
            {
                return _textPosition;
            }
        }
        /// <summary>
        /// Total number of bytes or characters.
        /// </summary>
        public long Length { get; }

        public int ProgressValue
        {
            get
            {
                return (int)(Position * 100 / Length);
            }
        }

        public override int Read()
        {
            int result = TextReader.Read();
            if (result == '\r')
            {
                if (TextReader.Peek() != '\n')
                {
                    LineNumber++;
                }
            }
            else if (result == '\n')
            {
                LineNumber++;
            }

            _textPosition++;
            return result;
        }
        /// <summary>
        /// The 0-based line number of the current position.
        /// </summary>
        public long LineNumber { get; protected set; }
        public override int Peek()
        {
            return TextReader.Peek();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                TextReader.Dispose();
            }
            base.Dispose(disposing);
        }

        public class StreamLineReader : LineReader
        {
            private Stream _stream;
            public StreamLineReader(Stream stream, Encoding encoding) : base(new StreamReader(stream, encoding), stream.Length)
            {
                _stream = stream;
            }

            public StreamLineReader(Stream stream) : base(new StreamReader(stream), stream.Length)
            {
                _stream = stream;
            }

            public override long Position
            {
                get { return _stream.Position; }
            }
        }

        public static LineReader FromPath(string filename)
        {
            return new StreamLineReader(File.OpenRead(filename));
        }

        public static LineReader FromText(string text)
        {
            return new LineReader(new StringReader(text), text.Length);
        }
    }
}
