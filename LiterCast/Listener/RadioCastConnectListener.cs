using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using LiterCast.RadioClients;
using LiterCast.Caster;
using NLog;

namespace LiterCast.Listener
{
    internal class RadioCastConnectListener : IDisposable
    {
        private static readonly ILogger LOGGER = LogManager.GetCurrentClassLogger();

        private TcpListener TcpListener { get; set; }

        public RadioCaster RadioCaster { get; private set; }
        public bool IsStarted { get; private set; }
        public IPEndPoint Endpoint { get; private set; }

        public event EventHandler<INewClientEventArgs> OnNewClient;

        public RadioCastConnectListener(RadioCaster radioCaster, IPEndPoint endpoint)
        {
            RadioCaster = radioCaster;
            Endpoint = endpoint;
            TcpListener = CreateTcpListener(endpoint);
        }

        public void Start()
        {
            if (IsStarted)
            {
                return;
            }
            IsStarted = true;
            TcpListener.Start();
            TcpListener.BeginAcceptTcpClient(ListenCallback, null);
        }

        public void Stop()
        {
            if(!IsStarted)
            {
                return;
            }
            IsStarted = false;
            TcpListener.Stop();
        }

        private async void ListenCallback(IAsyncResult result)
        {
            // Recursive call to process another request using this same callback
            try
            {
                TcpListener.BeginAcceptTcpClient(ListenCallback, null);
            }
            catch (Exception)
            {
                if (IsStarted) throw; //don't swallow too much!
                return;
            }

            // Call End to complete the asynchronous operation.
            TcpClient tcpClient = TcpListener.EndAcceptTcpClient(result);

            NetworkStream stream = tcpClient.GetStream();
            using (StreamReader streamReader = new StreamReader(stream, Encoding.ASCII, false, 500, true))
            using (StreamWriter streamWriter = new StreamWriter(stream, Encoding.ASCII, 5000, true))
            {
                string inputStr = await RequestHeadingParser.RetrieveHttpRequestHeadingFromStream(streamReader, 2500);

                LOGGER.Debug("Received request: {}", inputStr);

                var request = RequestHeadingParser.ParseHttpRequestHeading(inputStr);

                request.Headers.TryGetValue("Icy-Metadata", out string icyMetaData);

                if (!string.Equals(request.Verb, "GET", StringComparison.InvariantCultureIgnoreCase))
                {
                    await streamWriter.WriteAsync("HTTP 405 Method Not Allowed" + "\r\n");
                }

                bool isIcy;
                // Checks if is a SHOUTCast client, or a normal HTTP client
                if (!string.IsNullOrEmpty(icyMetaData) && Convert.ToInt64(icyMetaData) > 0)
                {
                    isIcy = true;
                    await streamWriter.WriteAsync("ICY 200 OK" + "\r\n");
                    await streamWriter.WriteAsync("icy-metaint: " + Convert.ToString(RadioCaster.RadioInfo.MetadataInterval) + "\r\n");
                }
                else
                {
                    isIcy = false;
                    await streamWriter.WriteAsync($"HTTP/{request.Version} 200 OK\r\n");
                }

                await streamWriter.WriteAsync("Content-Type: audio/mpeg" + "\r\n");

                // Begin body
                await streamWriter.WriteAsync("\r\n");

                await streamWriter.FlushAsync();

                var radioClient = isIcy ? (IRadioClient) new IcyRadioClient(stream, RadioCaster) : new RadioClient(stream);

                OnNewClient?.Invoke(this, new NewClientEventArgs(radioClient));
            }
        }

        private static TcpListener CreateTcpListener(IPEndPoint endpoint)
        {
            TcpListener tcpListener = new TcpListener(endpoint);
            return tcpListener;
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
                Stop();
            }
            // Clean native resources
        }

        ~RadioCastConnectListener()
        {
            Dispose(false);
        }

        private class NewClientEventArgs : INewClientEventArgs
        {
            public IRadioClient RadioClient { get; private set; }

            public NewClientEventArgs(IRadioClient radioClient)
            {
                RadioClient = radioClient;
            }
        }

        public interface INewClientEventArgs
        {
            IRadioClient RadioClient { get; }
        }
    }
}
