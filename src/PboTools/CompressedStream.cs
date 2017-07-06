using System;
using System.IO;

namespace PboTools
{
    internal class CompressedStream : Stream
    {
        private Stream s;
        private long originalSize;
        private long position;
        private BinaryReader reader;
        private byte[] buffer;
        private int unpackedBytesCount = 0;

        public CompressedStream(Stream s, long originalSize)
        {
            this.s = s;
            this.originalSize = originalSize;
            reader = new BinaryReader(s, System.Text.Encoding.ASCII, true);
            buffer = new byte[originalSize];
        }

        private void Unpack(int limit)
        {
            while (unpackedBytesCount < originalSize && unpackedBytesCount < limit)
            {
                var b = reader.ReadByte();
                for (int k = 0; k < 8 && unpackedBytesCount < originalSize; k++)
                {
                    var bit = b & 1;
                    b >>= 1;
                    if (bit == 1)
                    {
                        buffer[unpackedBytesCount++] = reader.ReadByte();
                    }
                    else
                    {
                        var pointer = reader.ReadUInt16();
                        var rpos = unpackedBytesCount - ((pointer & 0x00FF) + ((pointer & 0xF000) >> 4));
                        var rlen = ((pointer & 0x0F00) >> 8) + 3;
                        if (rpos < 0)
                        {
                            for (int j = 0; j < rlen; j++)
                            {
                                buffer[unpackedBytesCount++] = (byte)' ';
                            }
                        }
                        else if (rpos + rlen <= unpackedBytesCount)
                        {
                            Array.Copy(buffer, rpos, buffer, unpackedBytesCount, rlen);
                            unpackedBytesCount += rlen;
                        }
                        else
                        {
                            for (int j = 0; j < rlen; j++)
                            {
                                buffer[unpackedBytesCount++] = buffer[rpos + j];
                            }
                        }

                    }

                }
            }
        }


        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => originalSize;

        public override long Position { get => position; set => position = value; }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            checked
            {
                var l = (int)(originalSize - position);
                count = count < l ? count : l;
                Unpack((int)position + count);
                Array.Copy(this.buffer, (int)position, buffer, offset, count);
                position += count;
                return count;
            };
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    position = offset;
                    break;
                case SeekOrigin.Current:
                    position += offset;
                    break;
                case SeekOrigin.End:
                    position = originalSize - offset;
                    break;
                default:
                    throw new NotImplementedException();
            }
            return position;

        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}