using System;
using System.Threading.Tasks;
using System.Net;
using NLog;
using LiterCast.AudioSources;
using LiterCast.Listener;
using LiterCast.Caster;

namespace LiterCast
{
    public class RadioCastServer : IDisposable
    {
        private static readonly ILogger LOGGER = LogManager.GetCurrentClassLogger();

        public IPEndPoint Endpoint { get; private set; }
        public RadioInfo RadioInfo { get; private set; }

        public event EventHandler<ITrackChangedEventArgs> OnTrackChanged;

        public int TrackCount => Caster.TrackCount;
        public int ClientCount => Caster.ClientCount;

        private RadioCastConnectListener Listener { get; set; }
        private RadioCaster Caster { get; set; }

        public RadioCastServer(IPEndPoint endpoint, RadioInfo radioInfo)
        {
            Endpoint = endpoint;
            RadioInfo = radioInfo;
            Caster = new RadioCaster(radioInfo);
            Caster.OnTrackChanged += (_, eventData) =>
            {
                OnTrackChanged?.Invoke(this, eventData);
            };
            Listener = new RadioCastConnectListener(Caster, Endpoint);
            Listener.OnNewClient += (_, eventData) =>
            {
                Caster.AddRadioClient(eventData.RadioClient);
            };
        }

        public void AddTrack(IAudioSource track)
        {
            Caster.AddTrack(track);
        }

        public Task Start()
        {
            Listener.Start();
            var casterTask = Caster.Start();
            return casterTask;
        }

        public void Stop()
        {
            Listener.Stop();
            Caster.Stop();
        }

        private void ValidateMetadataInterval(int metadataInterval)
        {
            if (metadataInterval < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(metadataInterval), "The provided value cannot be negative!");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Clean managed resources
                Listener.Dispose();
            }
            // Clean native resources
        }

        ~RadioCastServer()
        {
            Dispose(false);
        }

        public interface ITrackChangedEventArgs
        {
            IAudioSource OldTrack { get; }
            IAudioSource NewTrack { get; }
        }
    }
}
