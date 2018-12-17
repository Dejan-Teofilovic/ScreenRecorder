﻿using System;
using System.Timers;
using Screna;

namespace Captura.ViewModels
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class TimerModel : NotifyPropertyChanged
    {
        readonly Settings _settings;

        Timer _timer;
        readonly Timing _timing = new Timing();

        TimeSpan _ts;

        public TimeSpan TimeSpan
        {
            get => _ts;
            private set
            {
                if (_ts == value)
                    return;

                _ts = value;

                OnPropertyChanged();
            }
        }

        int _countdown;

        public int Countdown
        {
            get => _countdown;
            set
            {
                _countdown = value;

                OnPropertyChanged();
            }
        }

        bool _waiting;

        public bool Waiting
        {
            get => _waiting;
            set
            {
                _waiting = value;

                OnPropertyChanged();
            }
        }

        public TimerModel(Settings Settings)
        {
            _settings = Settings;
        }

        void TimerOnElapsed(object Sender, ElapsedEventArgs Args)
        {
            if (Countdown > 0)
            {
                if (_timing.Elapsed.TotalSeconds > 1)
                {
                    _timing.Stop();

                    --Countdown;

                    _timing.Start();
                }

                return;
            }

            if (Waiting)
            {
                Waiting = false;

                CountdownElapsed?.Invoke();
            }

            TimeSpan = TimeSpan.FromSeconds((int)_timing.Elapsed.TotalSeconds);

            var duration = _settings.Duration;

            // If Capture Duration is set and reached
            if (duration > 0 && TimeSpan.TotalSeconds >= duration)
            {
                DurationElapsed?.Invoke();
            }
        }

        public event Action CountdownElapsed;

        public event Action DurationElapsed;

        public void Init()
        {
            _timer = new Timer(250);
            _timer.Elapsed += TimerOnElapsed;
        }

        public void Start()
        {
            _timer?.Stop();
            TimeSpan = TimeSpan.Zero;

            Waiting = false;

            if (_settings.PreStartCountdown > 0)
            {
                Countdown = _settings.PreStartCountdown;

                Waiting = true;
            }

            _timing?.Start();
            _timer?.Start();
        }

        public void Pause()
        {
            _timer?.Stop();
            _timing?.Pause();
        }

        public void Resume()
        {
            _timing?.Start();
            _timer?.Start();
        }

        public void Stop()
        {
            _timer?.Stop();
            _timing.Stop();

            Countdown = 0;
        }
    }
}