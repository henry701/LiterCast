using FmShell;
using LiterCast;
using NLog;

namespace ConsoleExecuterProj
{
    internal class ShellMethods
    {
        private static readonly ILogger LOGGER = LogManager.GetCurrentClassLogger();

        public RadioCastServer Server { get; private set; }

        public ShellMethods(RadioCastServer server)
        {
            Server = server;
        }

        public void AddTrack(FmShellArguments args)
        {
            LOGGER.Info("Adding Placeholder Track");
            var audioSource = new FileAudioSource("C:\\meus backups\\musicas\\Amadeus - Interlude.mp3");
            Server.AddTrack(audioSource);
        }

    }
}