using System.IO;

namespace LiterCast
{
    internal sealed class IcyRadioClient : IRadioClient
    {
        public Stream OutputStream { get; private set; }

        public IcyRadioClient(Stream outputStream)
        {
            OutputStream = new IcyInterceptorStream(outputStream);
        }

        private class IcyInterceptorStream : Stream
        {
            private Stream UnderlyingStream { get; set; }
            private int InterceptedBytes { get; set; }

            public IcyInterceptorStream(Stream stream)
            {
                this.UnderlyingStream = stream;
            }

            public override bool CanRead => false;

            public override bool CanSeek => false;

            public override bool CanWrite => true;

            public override long Length => UnderlyingStream.Length + InterceptedBytes; 

            public override long Position { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

            public override void Flush()
            {
                UnderlyingStream.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new System.NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new System.NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new System.NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                // TODO: intercept stream to write Icy Metadata to it occasionally
                UnderlyingStream.Write(buffer, offset, count);
            }
        }
    }
}