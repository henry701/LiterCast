using System;
using System.IO;
using LiterCast.Streams;

namespace LiterCast.AudioSources
{
    public sealed class FileAudioSource : IAudioSource
    {
        public string Title { get; private set; }
        public Stream Stream { get; private set; }
        public int BitRate { get; private set; }
        public int SampleRate { get; private set; }
        public string MimeType { get; private set; }

        public FileAudioSource(Stream fileStream, string title = null)
        {
            var tagFile = TagLib.File.Create(new TaglibFileAbstraction(fileStream, title ?? ""));

            Title = BuildTitle(title, tagFile, null);

            MimeType = tagFile.MimeType;
            BitRate = tagFile.Properties.AudioBitrate;
            SampleRate = tagFile.Properties.AudioSampleRate;
            
            long contentStartOffset = tagFile.InvariantStartPosition;
            fileStream.Position = contentStartOffset;

            Stream = new ThrottleRateStream(fileStream, BitRate * 125);
        }

        public FileAudioSource(string filePath, string title = null)
        {
            var tagFile = TagLib.File.Create(filePath);

            string filename = Path.GetFileNameWithoutExtension(filePath);

            Title = BuildTitle(title, tagFile, filename);

            MimeType = tagFile.MimeType;
            BitRate = tagFile.Properties.AudioBitrate;
            SampleRate = tagFile.Properties.AudioSampleRate;

            Stream fileStream = File.OpenRead(filePath);
            long contentStartOffset = tagFile.InvariantStartPosition;
            fileStream.Position = contentStartOffset;

            Stream = new ThrottleRateStream(fileStream, BitRate * 125);
        }

        private static string BuildTitle(string title, TagLib.File tagFile, string filename)
        {
            if(title != null)
            {
                return title;
            }
            string evalTry = tagFile?.Tag?.JoinedPerformers + " - " + tagFile?.Tag?.Title;
            if(string.CompareOrdinal(evalTry, " - ") != 0)
            {
                return evalTry;
            }
            return filename;
        }

        private class TaglibFileAbstraction : TagLib.File.IFileAbstraction
        {
            public string Name { get; set; }

            public Stream ReadStream { get; set; }

            public Stream WriteStream => throw new NotImplementedException();

            public TaglibFileAbstraction(Stream stream, string name = null)
            {
                ReadStream = stream;
                Name = (name ?? "UnNamed") + ".mp3";
            }

            public void CloseStream(Stream stream)
            {
                // Do not close the stream, it should be kept open.
                stream.Position = 0;
            }
        }
    }
}
