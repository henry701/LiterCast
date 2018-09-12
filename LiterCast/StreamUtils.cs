using System;
using System.IO;

namespace LiterCast
{
    public static class StreamUtils
    {
        public static long PositionSeek(this Stream stream, long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    return stream.Position = offset;
                case SeekOrigin.Current:
                    return stream.Position += offset;
                case SeekOrigin.End:
                    return stream.Position = stream.Length - offset;
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
