using System.IO;

namespace LiterCast.AudioSources
{
    public interface IAudioSource
    {
        string Title { get; }
        Stream Stream { get; }
        int BitRate { get; }
        int SampleRate { get; }
        string MimeType { get; }
    }
}