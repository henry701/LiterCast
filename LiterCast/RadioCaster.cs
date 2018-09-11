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

        public int TrackCount => Tracks.Count;

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
                SpinWait.SpinUntil(() => EnsureCurrentSource());

                byte[] buffer = new byte[2048];
                int bytesRead = await CurrentSource.Stream.ReadAsync(buffer, 0, buffer.Length);
                LOGGER.Debug("Bytes read: {0}", bytesRead);

                if(bytesRead == 0)
                {
                    MoveToNextTrack();
                    continue;
                }

                foreach (IRadioClient client in RadioClients.ToList())
                {
                    var streamTask = RadioStreamTo(client, buffer, bytesRead);
                }
            }
            IsStarted = false;
        }

        private bool EnsureCurrentSource()
        {
            bool hasSource = HasSource();
            if(!hasSource)
            {
                MoveToNextTrack();
            }
            return hasSource;
        }

        private bool HasSource()
        {
            return CurrentSource != null && CurrentSource.Stream != null;
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

        private async Task RadioStreamTo(IRadioClient client, byte[] buffer, int count)
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