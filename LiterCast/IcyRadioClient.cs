using System.IO;

namespace LiterCast
{
    internal sealed class IcyRadioClient : IRadioClient
    {
        public Stream OutputStream { get; private set; }
        public RadioCaster RadioCaster { get; private set; }

        public IcyRadioClient(Stream outputStream, RadioCaster radioCaster)
        {
            RadioCaster = radioCaster;
            OutputStream = new IcyInterceptorStream(outputStream, radioCaster);
        }

        private class IcyInterceptorStream : Stream
        {
            private Stream UnderlyingStream { get; set; }

            public int TotalInterceptedBytes { get; private set; }
            public int InterceptedBytes { get; private set; }

            public RadioCaster RadioCaster { get; private set; }

            public IAudioSource AudioSource { get; set; }

            public IcyInterceptorStream(Stream stream, RadioCaster radioCaster)
            {
                UnderlyingStream = stream;
                RadioCaster = radioCaster;
                // First thing that should be sent is the metadata though
                // InterceptedBytes = RadioCaster.RadioInfo.MetadataInterval;
            }

            public override bool CanRead => UnderlyingStream.CanRead;

            public override bool CanSeek => UnderlyingStream.CanSeek;

            public override bool CanWrite => UnderlyingStream.CanWrite;

            public override long Length => UnderlyingStream.Length + InterceptedBytes; 

            public override long Position { get => UnderlyingStream.Position + TotalInterceptedBytes; set => UnderlyingStream.Position = value - TotalInterceptedBytes; }

            public override void Flush()
            {
                UnderlyingStream.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return UnderlyingStream.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return this.PositionSeek(offset, origin);
            }

            public override void SetLength(long value)
            {
                UnderlyingStream.SetLength(value - InterceptedBytes);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                int left = RadioCaster.RadioInfo.MetadataInterval - InterceptedBytes;
                if (count > left)
                {
                    WriteAndTrack(buffer, offset, left);
                    count -= left;
                    offset += left;
                    WriteMeta();
                    Write(buffer, offset, count);
                }
                else
                {
                    WriteAndTrack(buffer, offset, count);
                }
            }

            private void WriteMeta()
            {
                InterceptedBytes = 0;
                byte[] metadata = RadioCaster.CurrentSource.GetIcyMetaData();
                UnderlyingStream.Write(metadata, 0, metadata.Length);
            }

            private void WriteAndTrack(byte[] buffer, int offset, int count)
            {
                InterceptedBytes += count;
                TotalInterceptedBytes += count;
                UnderlyingStream.Write(buffer, offset, count);
            }
        }
    }
}