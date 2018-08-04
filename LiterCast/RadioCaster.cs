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

        public void Start()
        {
            if(IsStarted)
            {
                return;
            }
            Task.Run((Action) EventLoop);
            SpinWait.SpinUntil(() => IsStarted);
        }

        private async void EventLoop()
        {
            ShouldRun = true;
            IsStarted = true;
            while (ShouldRun)
            {
                SpinWait.SpinUntil(() => ReadableSource());

                int bufferByteSize = (CurrentSource.BitRate / 4) * 1024;
                byte[] buffer = new byte[bufferByteSize];

                int bytesRead = await CurrentSource.Stream.ReadAsync(buffer, 0, bufferByteSize);

                foreach (RadioClient client in RadioClients.ToList())
                {
                    RadioStreamTo(client, buffer, bytesRead);
                }

                decimal secondsToSleep = bytesRead / (decimal) (CurrentSource.BitRate * 1024);
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

        private bool ReadableSource()
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
        }

        private bool CanReadCurrentTrack()
        {
            return CurrentSource?.Stream.CanRead == true
                    &&
                    CurrentSource?.Stream.Position < CurrentSource?.Stream.Length;
        }

        private async void RadioStreamTo(RadioClient client, byte[] buffer, int count)
        {
            try
            {
                await client.OutputStream.WriteAsync(buffer, 0, count);
                await client.OutputStream.FlushAsync();
            }
            catch (Exception e)
            {
                LOGGER.Error(e, "Error while writing to client OutputStream!");
            }
        }
    }
}