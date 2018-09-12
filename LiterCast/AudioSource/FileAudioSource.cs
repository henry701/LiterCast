using System;
using System.IO;
using static TagLib.File;

namespace LiterCast
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
            if(String.CompareOrdinal(evalTry, " - ") != 0)
            {
                return evalTry;
            }
            return filename;
        }

        private class TaglibFileAbstraction : IFileAbstraction
        {
            public string Name { get; set; }

            public Stream ReadStream { get; set; }

            public Stream WriteStream => throw new NotImplementedException();

            public TaglibFileAbstraction(Stream stream, string name = "")
            {
                ReadStream = stream;
                Name = name;
            }

            public void CloseStream(Stream stream)
            {
                stream.Close();
            }
        }
    }
}
