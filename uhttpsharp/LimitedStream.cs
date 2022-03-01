using System;
using System.IO;
using System.Text;

namespace uhttpsharp
{
    internal class LoggingStream : Stream
    {
        private readonly Stream _child;

        private readonly string _tempFileName = Path.GetTempFileName();
        public LoggingStream(Stream child)
        {
            _child = child;
            //Console.WriteLine($"Logging to {_tempFileName}");
        }

        public override void Flush()
        {
            _child.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _child.Seek(offset, origin);
        }
        public override void SetLength(long value)
        {
            _child.SetLength(value);
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            int retVal = _child.Read(buffer, offset, count);

            using (FileStream stream = File.Open(_tempFileName, FileMode.Append))
            {
                stream.Seek(0, SeekOrigin.End);
                stream.Write(buffer, offset, retVal);
            }

            return retVal;
        }

        public override int ReadByte()
        {
            int retVal = _child.ReadByte();

            using (FileStream stream = File.Open(_tempFileName, FileMode.Append))
            {
                stream.Seek(0, SeekOrigin.End);
                stream.WriteByte((byte)retVal);
            }

            return retVal;
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            _child.Write(buffer, offset, count);
        }
        public override void WriteByte(byte value)
        {
            _child.WriteByte(value);
        }
        public override bool CanRead => _child.CanRead;
        public override bool CanSeek => _child.CanSeek;

        public override bool CanWrite => _child.CanWrite;
        public override long Length => _child.Length;
        public override long Position
        {
            get => _child.Position;
            set => _child.Position = value;
        }
        public override int ReadTimeout
        {
            get => _child.ReadTimeout;
            set => _child.ReadTimeout = value;
        }
        public override int WriteTimeout
        {
            get => _child.WriteTimeout;
            set => _child.WriteTimeout = value;
        }
    }

    internal class LimitedStream : Stream
    {
        private const string ExceptionMessageFormat = "The Stream has exceeded the {0} limit specified.";
        private readonly Stream _child;
        private long _readLimit;
        private long _writeLimit;

        public LimitedStream(Stream child, long readLimit = -1, long writeLimit = -1)
        {
            _child = child;
            _readLimit = readLimit;
            _writeLimit = writeLimit;
        }
        public override void Flush()
        {
            _child.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _child.Seek(offset, origin);
        }
        public override void SetLength(long value)
        {
            _child.SetLength(value);
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            int retVal = _child.Read(buffer, offset, count);

            AssertReadLimit(retVal);

            return retVal;
        }
        private void AssertReadLimit(int coefficient)
        {
            if (_readLimit == -1)
            {
                return;
            }

            _readLimit -= coefficient;

            if (_readLimit < 0)
            {
                throw new IOException(string.Format(ExceptionMessageFormat, "read"));
            }
        }

        private void AssertWriteLimit(int coefficient)
        {
            if (_writeLimit == -1)
            {
                return;
            }

            _writeLimit -= coefficient;

            if (_writeLimit < 0)
            {
                throw new IOException(string.Format(ExceptionMessageFormat, "write"));
            }
        }

        public override int ReadByte()
        {
            int retVal = _child.ReadByte();

            AssertReadLimit(1);

            return retVal;
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            _child.Write(buffer, offset, count);

            AssertWriteLimit(count);
        }
        public override void WriteByte(byte value)
        {
            _child.WriteByte(value);

            AssertWriteLimit(1);
        }
        public override bool CanRead => _child.CanRead;
        public override bool CanSeek => _child.CanSeek;

        public override bool CanWrite => _child.CanWrite;
        public override long Length => _child.Length;
        public override long Position
        {
            get => _child.Position;
            set => _child.Position = value;
        }
        public override int ReadTimeout
        {
            get => _child.ReadTimeout;
            set => _child.ReadTimeout = value;
        }
        public override int WriteTimeout
        {
            get => _child.WriteTimeout;
            set => _child.WriteTimeout = value;
        }
    }
}