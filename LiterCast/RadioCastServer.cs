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

        public int MetadataInterval { get; private set; } = 8192;

        private RadioCastConnectListener Listener { get; set; }
        private RadioCaster Caster { get; set; }

        public RadioCastServer(TcpListener tcpListener)
        {
            Listener = new RadioCastConnectListener(new RadioInfo(MetadataInterval), tcpListener);
            Init();
        }

        public RadioCastServer(params string[] endpoints)
        {
            Listener = new RadioCastConnectListener(new RadioInfo(MetadataInterval), endpoints);
            Init();
        }

        public RadioCastServer(int metadataInterval, TcpListener tcpListener) : this(tcpListener)
        {
            ValidateMetadataInterval(metadataInterval);
            MetadataInterval = metadataInterval;
        }

        public RadioCastServer(int metadataInterval, params string[] endpoints) : this(endpoints)
        {
            ValidateMetadataInterval(metadataInterval);
            MetadataInterval = metadataInterval;
        }

        public void AddTrack(IAudioSource track)
        {
            Caster.AddTrack(track);
        }

        public void Start()
        {
            Listener.Start();
            Caster.Start();
        }

        public void Stop()
        {
            Listener.Stop();
            Caster.Stop();
        }

        private void Init()
        {
            Caster = new RadioCaster(new RadioInfo(MetadataInterval));
            Listener.OnNewClient += (_, eventData) =>
            {
                Caster.AddRadioClient(eventData.RadioClient);
            };
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
