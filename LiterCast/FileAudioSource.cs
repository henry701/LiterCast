﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LiterCast
{
    public sealed class FileAudioSource : IAudioSource
    {
        public string Title { get; private set; }
        public Stream Stream { get; private set; }
        public int BitRate { get; private set; }
        public int SampleRate { get; private set; }
        public string MimeType { get; private set; }

        public FileAudioSource(string filePath, string title = null)
        {
            var tagFile = TagLib.File.Create(filePath);

            Title = title ?? tagFile.Tag.Title;

            MimeType = tagFile.MimeType;
            BitRate = tagFile.Properties.AudioBitrate;
            SampleRate = tagFile.Properties.AudioSampleRate;

            byte[] fileBytes = File.ReadAllBytes(filePath);
            long contentStartOffset = tagFile.InvariantStartPosition;
            int contentStartOffsetInt = Convert.ToInt32(contentStartOffset);
            Stream = new MemoryStream(fileBytes, contentStartOffsetInt, fileBytes.Length - contentStartOffsetInt, writable: false);
        }
    }
}
