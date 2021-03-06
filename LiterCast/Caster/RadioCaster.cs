﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LiterCast.AudioSources;
using LiterCast.RadioClients;
using NLog;
using static LiterCast.RadioCastServer;

namespace LiterCast.Caster
{
    internal sealed class RadioCaster
    {
        private static readonly ILogger LOGGER = LogManager.GetCurrentClassLogger();

        private const int BufferSize = 1024 * 64;

        public RadioInfo RadioInfo { get; private set; }

        public Task RunningTask { get; private set; }
        public bool IsStarted { get; private set; }
        public IAudioSource CurrentSource { get; private set; }

        public int TrackCount => Tracks.Count;
        public int ClientCount => RadioClients.Count;

        private LinkedList<IRadioClient> RadioClients { get; set; }
        private LinkedList<IAudioSource> Tracks { get; set; }

        public event EventHandler<ITrackChangedEventArgs> OnTrackChanged;

        private bool ShouldRun { get; set; }

        private ManualResetEvent NewTrackPossiblyAvailable { get; set; }

        public RadioCaster(RadioInfo radioInfo)
        {
            RadioInfo = radioInfo;
            RadioClients = new LinkedList<IRadioClient>();
            Tracks = new LinkedList<IAudioSource>();
            NewTrackPossiblyAvailable = new ManualResetEvent(false);
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

                byte[] buffer = new byte[BufferSize];

                int bytesRead = await CurrentSource.Stream.ReadAsync(buffer, 0, buffer.Length);
                LOGGER.Trace("Bytes read: {0}", bytesRead);

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
            NewTrackPossiblyAvailable.Set();
        }

        public void RemoveRadioClient(IRadioClient client)
        {
            RadioClients.Remove(client);
        }

        private void MoveToNextTrack()
        {
            IAudioSource oldTrack = CurrentSource;
            Tracks.Remove(CurrentSource);
            var newTrack = Tracks.Last?.Value;
            CurrentSource = newTrack;
            if(newTrack != oldTrack)
            {
                LOGGER.Debug("Changed audio source! New source: {0} BitRate={1}", CurrentSource, CurrentSource?.BitRate);
                OnTrackChanged?.Invoke(this, new TrackChangedEventArgs(oldTrack, newTrack));
            }
            else
            {
                NewTrackPossiblyAvailable.WaitOne();
                NewTrackPossiblyAvailable.Reset();
            }
        }

        private async Task RadioStreamTo(IRadioClient client, byte[] buffer, int count)
        {
            if(client.OutputStream.CanWrite == false)
            {
                client.OutputStream.Dispose();
                RadioClients.Remove(client);
                return;
            }
            try
            {
                await client.OutputStream.WriteAsync(buffer, 0, count);
                await client.OutputStream.FlushAsync();
            }
            catch (Exception e)
            {
                LOGGER.Error(e, "Error while writing to client OutputStream!");
                client.OutputStream.Dispose();
                RadioClients.Remove(client);
            }
        }

        private class TrackChangedEventArgs : ITrackChangedEventArgs
        {
            public IAudioSource OldTrack { get; private set; }
            public IAudioSource NewTrack { get; private set; }

            public TrackChangedEventArgs(IAudioSource oldTrack, IAudioSource newTrack)
            {
                OldTrack = oldTrack;
                NewTrack = newTrack;
            }
        }
    }
}