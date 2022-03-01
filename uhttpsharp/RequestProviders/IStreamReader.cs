using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace uhttpsharp.RequestProviders
{
    public interface IStreamReader
    {
        Task<string> ReadLine();

        Task<byte[]> ReadBytes(int count);
    }

    internal class StreamReaderAdapter : IStreamReader
    {
        private readonly StreamReader reader;
        public StreamReaderAdapter(StreamReader reader)
        {
            this.reader = reader;
        }

        public async Task<string> ReadLine()
        {
            return await reader.ReadLineAsync().ConfigureAwait(false);
        }
        public async Task<byte[]> ReadBytes(int count)
        {
            char[] tempBuffer = new char[count];

            await reader.ReadBlockAsync(tempBuffer, 0, count).ConfigureAwait(false);

            byte[] retVal = new byte[count];

            for (int i = 0; i < tempBuffer.Length; i++)
            {
                retVal[i] = (byte)tempBuffer[i];
            }

            return retVal;
        }
    }

    internal class MyStreamReader : IStreamReader
    {
        private const int BufferSize = 8096 / 4;
        private readonly Stream underlyingStream;

        private readonly byte[] middleBuffer = new byte[BufferSize];
        private int index;
        private int count;

        public MyStreamReader(Stream underlyingStream)
        {
            this.underlyingStream = underlyingStream;
        }

        private async Task ReadBuffer()
        {
            do
            {
                count = await underlyingStream.ReadAsync(middleBuffer.AsMemory(0, BufferSize)).ConfigureAwait(false);

                if (count == 0)
                {
                    // Fix for 100% CPU
                    await Task.Delay(100).ConfigureAwait(false);
                }
            } while (count == 0);

            index = 0;
        }

        public async Task<string> ReadLine()
        {
            StringBuilder builder = new(64);

            if (index == count)
            {
                await ReadBuffer().ConfigureAwait(false);
            }

            byte readByte = middleBuffer[index++];

            while (readByte != '\n' && (builder.Length == 0 || builder[^1] != '\r'))
            {
                builder.Append((char)readByte);

                if (index == count)
                {
                    await ReadBuffer().ConfigureAwait(false);
                }

                readByte = middleBuffer[index++];
            }

            //Debug.WriteLine("Readline : " + sw.ElapsedMilliseconds);

            return builder.ToString(0, builder.Length - 1);
        }

        public async Task<byte[]> ReadBytes(int count)
        {
            byte[] buffer = new byte[count];
            int currentByte = 0;

            // Empty the buffer
            int bytesToRead = Math.Min(this.count - index, count) + index;
            for (int i = index; i < bytesToRead; i++)
            {
                buffer[currentByte++] = middleBuffer[i];
            }

            index = this.count;

            // Read from stream
            while (currentByte < count)
            {
                currentByte += await underlyingStream.ReadAsync(buffer.AsMemory(currentByte, count - currentByte))
                    .ConfigureAwait(false);
            }

            //Debug.WriteLine("ReadBytes(" + count + ") : " + sw.ElapsedMilliseconds);

            return buffer;
        }
    }
}