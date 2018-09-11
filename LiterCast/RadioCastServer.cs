using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Net.Sockets;
using NLog;

namespace LiterCast
{
    public class RadioCastServer : IDisposable
    {
        private static readonly ILogger LOGGER = LogManager.GetCurrentClassLogger();

        public IPEndPoint Endpoint { get; private set; }
        public RadioInfo RadioInfo { get; private set; }

        public long TrackCount => Caster.TrackCount;

        private RadioCastConnectListener Listener { get; set; }
        private RadioCaster Caster { get; set; }

        public RadioCastServer(IPEndPoint endpoint, RadioInfo radioInfo)
        {
            Endpoint = endpoint;
            RadioInfo = radioInfo;
            Caster = new RadioCaster(radioInfo);
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
    }
}
