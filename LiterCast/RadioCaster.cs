using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace LiterCast
{
    internal sealed class RadioCaster
    {
        private static readonly ILogger LOGGER = LogManager.GetCurrentClassLogger();

        public RadioInfo RadioInfo { get; private set; }

        public Task RunningTask { get; private set; }
        public bool IsStarted { get; private set; }
        public IAudioSource CurrentSource { get; private set; }

        private LinkedList<IRadioClient> RadioClients { get; set; }
        private LinkedList<IAudioSource> Tracks { get; set; }

        private bool ShouldRun { get; set; }

        public RadioCaster(RadioInfo radioInfo)
        {
            RadioInfo = radioInfo;
            RadioClients = new LinkedList<IRadioClient>();
            Tracks = new LinkedList<IAudioSource>();
        }

        public void Stop()
        {
            if(!IsStarted)
            {
                return;
            }
            ShouldRun = false;
            SpinWait.SpinUntil(() => !IsStarted);
        }

        public Task Start()
        {
            if(IsStarted)
            {
                return RunningTask;
            }
            RunningTask = Task.Run((Action) EventLoop);
            SpinWait.SpinUntil(() => IsStarted);
            return RunningTask;
        }

        private async void EventLoop()
        {
            ShouldRun = true;
            IsStarted = true;
            while (ShouldRun)
            {
                SpinWait.SpinUntil(() => TryGetReadableSource());

                byte[] buffer = new byte[2048];
                int bytesRead = await CurrentSource.Stream.ReadAsync(buffer, 0, buffer.Length);
                LOGGER.Debug("Bytes read: {0}", bytesRead);

                foreach (RadioClient client in RadioClients.ToList())
                {
                    var streamTask = RadioStreamTo(client, buffer, bytesRead);
                }
                // TODO: Fix this calculation, should be real time
                // 8 kbits por segundo bitrate
                // 1 kbyte por segundo byterate (divide por 8 da isso)
                // 2048 bytes de buffer (2kbytes)
                // dividindo 1 por 2 dá 0,5 segundos
                // ta certo isso?
                // a musica tem 2m16s = 136s
                // e tem 133kb kkk
                double secondsToSleep = (bytesRead / 1024d) / ((CurrentSource.BitRate / 8d));
                LOGGER.Debug("Seconds to sleep: {0}", secondsToSleep);
                await Task.Run(() => Thread.Sleep(TimeSpan.FromSeconds(Convert.ToDouble(secondsToSleep))));
            }
            IsStarted = false;
        }

        public void AddRadioClient(IRadioClient client)
        {
            RadioClients.AddLast(client);
        }

        public void AddTrack(IAudioSource track)
        {
            Tracks.AddLast(track);
        }

        public void RemoveRadioClient(IRadioClient client)
        {
            RadioClients.Remove(client);
        }

        private bool TryGetReadableSource()
        {
            if(!CanReadCurrentTrack())
            {
                MoveToNextTrack();
            }
            return CanReadCurrentTrack();
        }

        private void MoveToNextTrack()
        {
            Tracks.Remove(CurrentSource);
            var track = Tracks.Last?.Value;
            CurrentSource = track;
            if(CurrentSource != null)
            {
                LOGGER.Debug("Changed audio source! New source: {0} BitRate={1}", CurrentSource, CurrentSource.BitRate);
            }
        }

        private bool CanReadCurrentTrack()
        {
            return CurrentSource?.Stream.CanRead == true
                    &&
                    CurrentSource?.Stream.Position < CurrentSource?.Stream.Length;
        }

        private async Task RadioStreamTo(RadioClient client, byte[] buffer, int count)
        {
            try
            {
                await client.OutputStream.WriteAsync(buffer, 0, count);
                await client.OutputStream.FlushAsync();
            }
            catch (Exception e)
            {
                LOGGER.Error(e, "Error while writing to client OutputStream!");
                RadioClients.Remove(client);
                // TODO: Notify removal via event?
            }
        }
    }
}