using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LiterCast
{
    public sealed class StreamAudioSource : IAudioSource
    {
        public string Title { get; }
        public Stream Stream { get; }
        public int BitRate { get; }
        public int SampleRate { get; }
        public string MimeType { get; }

        public StreamAudioSource(Stream stream, string title, int bitRate, int sampleRate, string mimeType)
        {
            Stream = stream;
            Title = title;
            BitRate = bitRate;
            SampleRate = sampleRate;
            MimeType = mimeType;
        }
    }
}
