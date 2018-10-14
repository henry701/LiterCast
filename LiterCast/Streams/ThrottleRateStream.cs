using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LiterCast.Streams
{
    public class ThrottleRateStream : Stream
    {
        private Stream UnderlyingStream { get; set; }

        private int AvailableBytes { get; set; }
        private int TotalAvailableBytes { get; set; }

        public int BytesPerSecond { get; private set; }
        public double RefreshNanos { get; private set; }

        private readonly Stopwatch Watch = new Stopwatch();
        private double SkewNanos { get; set; }

        public ThrottleRateStream(Stream stream, int bytesPerSecond, double refreshNanos = 1000000000.0d)
        {
            UnderlyingStream = stream;
            BytesPerSecond = bytesPerSecond;
            RefreshNanos = refreshNanos;

            Watch.Start();
            Task.Run(() =>
            {
                while (true)
                {
                    TickLoop();
                }
            });
        }

        private bool TimesUp()
        {
            // The amount of time in nanos we need to wait for this iteration's trigger
            double waitForNanos = ((1 * RefreshNanos) + SkewNanos);
            // The elapsed time, in nanosseconds
            double elapsedNanos = RefreshNanos * ((double)Watch.ElapsedTicks / Stopwatch.Frequency);
            // What's left for the trigger
            double leftNanos = waitForNanos - elapsedNanos;
            if (leftNanos <= 0) 
            {
                // If we're late, skew the next trigger earlier to compensate
                SkewNanos = leftNanos;
                // Restart the watch
                Watch.Restart();
                return true;
            }
            // Sleep tolerated if there's a lot of time left, not to strain the CPU
            // If more than 200ms left, sleep for 150ms before the next check.
            // Since Thread.Sleep is not very trustworthy, this is an acceptable compromise.
            if(leftNanos > 200000000.0d)
            {
                Thread.Sleep(150);
            }
            return false;
        }

        private void TickLoop()
        {
            SpinWait.SpinUntil(TimesUp);
            OnTick();
        }

        private void OnTick()
        {
            TotalAvailableBytes += BytesPerSecond;
            AvailableBytes += BytesPerSecond;
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
            // Wait for resources to become available on the other end, OR for the stream to end
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
