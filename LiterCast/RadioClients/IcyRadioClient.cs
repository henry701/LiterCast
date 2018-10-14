using System.IO;
using LiterCast.AudioSources;
using LiterCast.Caster;
using LiterCast.Streams;

namespace LiterCast.RadioClients
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
    }
}