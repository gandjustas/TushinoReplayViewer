using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PboTools
{
    internal class PatrialStream : Stream
    {
        private Stream s;
        private long offset;
        private long size;
        private long position;

        public PatrialStream(Stream s, long offset, long size)
        {
            this.s = s;
            this.offset = offset;
            this.size = size;
        }

        public override bool CanRead => s.CanRead;

        public override bool CanSeek => s.CanSeek;

        public override bool CanWrite => s.CanWrite;

        public override long Length => size;

        public override long Position { get => position; set => position =value; }

        public override void Flush()
        {
            s.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (s.Position != this.offset + position)
            {
                s.Seek(this.offset + position, SeekOrigin.Begin);
            }
            var r = s.Read(buffer, offset, count < (size - position) ? count : (int)(size - position));
            position = s.Position - this.offset;
            return r;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    position = offset;
                    return s.Seek(this.offset + position, SeekOrigin.Begin);
                case SeekOrigin.Current:
                    position += offset;
                    return s.Seek(offset, SeekOrigin.Current);
                case SeekOrigin.End:
                    position = size - offset;
                    return s.Seek(this.offset + position, SeekOrigin.Begin);
                default:
                    throw new NotImplementedException();
            }
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (s.Position != this.offset + position)
            {
                s.Seek(this.offset + position, SeekOrigin.Begin);
            }
            s.Write(buffer, offset, count < (size - position) ? count : (int)(size - position));
            position = s.Position - this.offset;
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return s.FlushAsync(cancellationToken);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (s.Position != this.offset + position)
            {
                s.Seek(this.offset + position, SeekOrigin.Begin);
            }
            var r = await base.ReadAsync(buffer, offset, count < (size - position) ? count : (int)(size - position), cancellationToken);
            position = s.Position - this.offset;
            return r;
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (s.Position != this.offset + position)
            {
                s.Seek(this.offset + position, SeekOrigin.Begin);
            }
            await base.WriteAsync(buffer, offset, count < (size - position) ? count : (int)(size - position), cancellationToken);
            position = s.Position - this.offset;
        }

        public override bool CanTimeout => s.CanTimeout;
    }
}