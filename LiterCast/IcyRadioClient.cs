using System.IO;

namespace LiterCast
{
    internal sealed class IcyRadioClient : IRadioClient
    {
        public Stream OutputStream { get; private set; }

        public IcyRadioClient(Stream outputStream)
        {
            // TODO intercept stream to write Icy Metadata to it occasionally
            OutputStream = outputStream;
        }
    }
}