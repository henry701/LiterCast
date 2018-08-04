using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace LiterCast
{
    internal class RadioCastConnectListener : IDisposable
    {
        private static readonly ILogger LOGGER = LogManager.GetCurrentClassLogger();

        private TcpListener TcpListener { get; set; }

        public RadioInfo RadioInfo { get; private set; }
        public bool IsStarted { get; private set; }

        public event EventHandler<INewClientEventArgs> OnNewClient;

        public RadioCastConnectListener(RadioInfo radioInfo, TcpListener httpListener)
        {
            RadioInfo = radioInfo;
            TcpListener = httpListener;
        }

        public RadioCastConnectListener(RadioInfo radioInfo, params string[] endpoints)
        {
            RadioInfo = radioInfo;
            TcpListener = CreateTcpListener(endpoints);
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
            TcpListener.Stop();
            IsStarted = false;
        }

        private async void ListenCallback(IAsyncResult result)
        {
            // Recursive call to process another request using this same callback
            TcpListener.BeginAcceptTcpClient(ListenCallback, null);

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

                if (!String.Equals(request.Verb, "GET", StringComparison.InvariantCultureIgnoreCase))
                {
                    await streamWriter.WriteAsync("HTTP 405 Method Not Allowed" + "\r\n");
                }

                bool isIcy;
                // Checks if is a SHOUTCast client, or a normal HTTP client
                if (!string.IsNullOrEmpty(icyMetaData) && Convert.ToInt64(icyMetaData) > 0)
                {
                    isIcy = true;
                    await streamWriter.WriteAsync("ICY 200 OK" + "\r\n");
                    await streamWriter.WriteAsync("icy-metaint: " + Convert.ToString(RadioInfo.MetadataInterval) + "\r\n");
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

                var radioClient = isIcy ? (IRadioClient) new IcyRadioClient(stream) : new RadioClient(stream);

                OnNewClient?.Invoke(this, new NewClientEventArgs(radioClient));
            }
        }

        private static TcpListener CreateTcpListener(string[] endpoints)
        {
            // TODO actually respect the endpoints and port and etc
            TcpListener tcpListener = new TcpListener(IPAddress.Any, 8081);
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
            public IRadioClient RadioClient { get; set; }

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
