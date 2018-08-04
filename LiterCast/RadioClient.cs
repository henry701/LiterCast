using System.IO;

namespace LiterCast
{
    internal sealed class RadioClient : IRadioClient
    {
        public Stream OutputStream { get; private set; }

        public RadioClient(Stream outputStream)
        {
            OutputStream = outputStream;
        }
    }
}