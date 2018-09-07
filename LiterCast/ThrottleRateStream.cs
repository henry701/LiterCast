using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiterCast
{
    public class ThrottleRateStream : Stream
    {
        private Stream UnderlyingStream { get; set; }

        private int AvailableBytes { get; set; }
        private int TotalAvailableBytes { get; set; }

        private int BytesPerSecond { get; set; }

        private Timer SecondsTimer { get; set; }
        
        public ThrottleRateStream(Stream stream, int bytesPerSecond)
        {
            UnderlyingStream = stream;
            BytesPerSecond = bytesPerSecond;
            SecondsTimer = new Timer(state =>
            {
                TotalAvailableBytes += bytesPerSecond;
                AvailableBytes += bytesPerSecond;
            }, null, 0, 999);
        }

        public override bool CanRead => UnderlyingStream.CanRead;

        public override bool CanSeek => UnderlyingStream.CanSeek;

        public override bool CanWrite => UnderlyingStream.CanWrite;

        public override long Length => TotalAvailableBytes;

        public override long Position
        {
            get => UnderlyingStream.Position;
            set
            {
                if(value > TotalAvailableBytes)
                {
                    value = TotalAvailableBytes;
                }
                UnderlyingStream.Position = value;
            }
        }

        public override void Flush()
        {
            UnderlyingStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            SpinWait.SpinUntil(() => AvailableBytes > 0 || (TotalAvailableBytes >= UnderlyingStream.Length));
            int readCount = Math.Min(count, AvailableBytes);
            int actualRead = UnderlyingStream.Read(buffer, offset, readCount);
            AvailableBytes -= actualRead;
            return actualRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.PositionSeek(offset, origin);
        }

        public override void SetLength(long value)
        {
            UnderlyingStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            UnderlyingStream.Write(buffer, offset, count);
        }
    }
}
