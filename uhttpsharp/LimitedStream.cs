using System.IO;

namespace uhttpsharp
{
    internal class LoggingStream : Stream
    {
        private readonly Stream child;

        private readonly string tempFileName = Path.GetTempFileName();
        public LoggingStream(Stream child)
        {
            this.child = child;
            //Console.WriteLine($"Logging to {_tempFileName}");
        }

        public override void Flush()
        {
            child.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return child.Seek(offset, origin);
        }
        public override void SetLength(long value)
        {
            child.SetLength(value);
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            int retVal = child.Read(buffer, offset, count);

            using FileStream stream = File.Open(tempFileName, FileMode.Append);
            stream.Seek(0, SeekOrigin.End);
            stream.Write(buffer, offset, retVal);

            return retVal;
        }

        public override int ReadByte()
        {
            int retVal = child.ReadByte();

            using FileStream stream = File.Open(tempFileName, FileMode.Append);
            stream.Seek(0, SeekOrigin.End);
            stream.WriteByte((byte)retVal);

            return retVal;
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            child.Write(buffer, offset, count);
        }
        public override void WriteByte(byte value)
        {
            child.WriteByte(value);
        }
        public override bool CanRead => child.CanRead;
        public override bool CanSeek => child.CanSeek;

        public override bool CanWrite => child.CanWrite;
        public override long Length => child.Length;
        public override long Position
        {
            get => child.Position;
            set => child.Position = value;
        }
        public override int ReadTimeout
        {
            get => child.ReadTimeout;
            set => child.ReadTimeout = value;
        }
        public override int WriteTimeout
        {
            get => child.WriteTimeout;
            set => child.WriteTimeout = value;
        }
    }

    internal class LimitedStream : Stream
    {
        private const string ExceptionMessageFormat = "The Stream has exceeded the {0} limit specified.";
        private readonly Stream child;
        private long readLimit;
        private long writeLimit;

        public LimitedStream(Stream child, long readLimit = -1, long writeLimit = -1)
        {
            this.child = child;
            this.readLimit = readLimit;
            this.writeLimit = writeLimit;
        }
        public override void Flush()
        {
            child.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return child.Seek(offset, origin);
        }
        public override void SetLength(long value)
        {
            child.SetLength(value);
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            int retVal = child.Read(buffer, offset, count);

            AssertReadLimit(retVal);

            return retVal;
        }
        private void AssertReadLimit(int coefficient)
        {
            if (readLimit == -1)
            {
                return;
            }

            readLimit -= coefficient;

            if (readLimit < 0)
            {
                throw new IOException(string.Format(ExceptionMessageFormat, "read"));
            }
        }

        private void AssertWriteLimit(int coefficient)
        {
            if (writeLimit == -1)
            {
                return;
            }

            writeLimit -= coefficient;

            if (writeLimit < 0)
            {
                throw new IOException(string.Format(ExceptionMessageFormat, "write"));
            }
        }

        public override int ReadByte()
        {
            int retVal = child.ReadByte();

            AssertReadLimit(1);

            return retVal;
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            child.Write(buffer, offset, count);

            AssertWriteLimit(count);
        }
        public override void WriteByte(byte value)
        {
            child.WriteByte(value);

            AssertWriteLimit(1);
        }
        public override bool CanRead => child.CanRead;
        public override bool CanSeek => child.CanSeek;

        public override bool CanWrite => child.CanWrite;
        public override long Length => child.Length;
        public override long Position
        {
            get => child.Position;
            set => child.Position = value;
        }
        public override int ReadTimeout
        {
            get => child.ReadTimeout;
            set => child.ReadTimeout = value;
        }
        public override int WriteTimeout
        {
            get => child.WriteTimeout;
            set => child.WriteTimeout = value;
        }
    }
}