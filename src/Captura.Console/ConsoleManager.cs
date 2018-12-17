﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Captura.Models;
using Captura.ViewModels;
using Screna;
using static System.Console;

namespace Captura
{
    // ReSharper disable once ClassNeverInstantiated.Global
    class ConsoleManager : IDisposable
    {
        readonly Settings _settings;
        readonly MainModel _mainModel;
        readonly RecordingModel _recordingModel;
        readonly ScreenShotModel _screenShotModel;
        readonly VideoSourcesViewModel _videoSourcesViewModel;
        readonly IEnumerable<IVideoSourceProvider> _videoSourceProviders;
        readonly IWebCamProvider _webCamProvider;
        readonly VideoWritersViewModel _videoWritersViewModel;

        public ConsoleManager(Settings Settings,
            RecordingModel RecordingModel,
            MainModel MainModel,
            ScreenShotModel ScreenShotModel,
            VideoSourcesViewModel VideoSourcesViewModel,
            IEnumerable<IVideoSourceProvider> VideoSourceProviders,
            IWebCamProvider WebCamProvider, VideoWritersViewModel VideoWritersViewModel)
        {
            _settings = Settings;
            _recordingModel = RecordingModel;
            _mainModel = MainModel;
            _screenShotModel = ScreenShotModel;
            _videoSourcesViewModel = VideoSourcesViewModel;
            _videoSourceProviders = VideoSourceProviders;
            _webCamProvider = WebCamProvider;
            _videoWritersViewModel = VideoWritersViewModel;

            // Hide on Full Screen Screenshot doesn't work on Console
            Settings.UI.HideOnFullScreenShot = false;
        }

        public void Dispose()
        {
            _mainModel.Dispose();
        }

        public void CopySettings()
        {
            // Load settings dummy
            var dummySettings = new Settings();
            dummySettings.Load();

            _settings.WebcamOverlay = dummySettings.WebcamOverlay;
            _settings.MousePointerOverlay = dummySettings.MousePointerOverlay;
            _settings.Clicks = dummySettings.Clicks;
            _settings.Keystrokes = dummySettings.Keystrokes;
            _settings.Elapsed = dummySettings.Elapsed;

            // FFmpeg Path
            _settings.FFmpeg.FolderPath = dummySettings.FFmpeg.FolderPath;

            foreach (var overlay in dummySettings.Censored)
            {
                _settings.Censored.Add(overlay);
            }

            foreach (var overlay in dummySettings.TextOverlays)
            {
                _settings.TextOverlays.Add(overlay);
            }

            foreach (var overlay in dummySettings.ImageOverlays)
            {
                _settings.ImageOverlays.Add(overlay);
            }
        }

        public void Start(StartCmdOptions StartOptions)
        {
            _settings.IncludeCursor = StartOptions.Cursor;
            _settings.Clicks.Display = StartOptions.Clicks;
            _settings.Keystrokes.Display = StartOptions.Keys;

            if (File.Exists(StartOptions.FileName))
            {
                if (!StartOptions.Overwrite)
                {
                    if (!ServiceProvider.MessageProvider
                        .ShowYesNo("Output File Already Exists, Do you want to overwrite?", ""))
                        return;
                }

                File.Delete(StartOptions.FileName);
            }

            HandleVideoSource(StartOptions);

            HandleVideoEncoder(StartOptions);

            HandleAudioSource(StartOptions);

            HandleWebcam(StartOptions);

            if (StartOptions.FrameRate is int frameRate)
                _settings.Video.FrameRate = frameRate;

            if (StartOptions.AudioQuality is int aq)
                _settings.Audio.Quality = aq;

            if (StartOptions.VideoQuality is int vq)
                _settings.Video.Quality = vq;

            if (StartOptions.Delay > 0)
                Thread.Sleep(StartOptions.Delay);

            if (!_recordingModel.StartRecording(StartOptions.FileName))
                return;

            Task.Factory.StartNew(() =>
            {
                Loop(StartOptions);

                _recordingModel.StopRecording().Wait();

                Application.Exit();
            });

            // MouseKeyHook requires a Window Handle to register
            Application.Run(new ApplicationContext());
        }

        public void Shot(ShotCmdOptions ShotOptions)
        {
            _settings.IncludeCursor = ShotOptions.Cursor;

            // Screenshot Window with Transparency
            if (ShotOptions.Source != null && Regex.IsMatch(ShotOptions.Source, @"win:\d+"))
            {
                var ptr = int.Parse(ShotOptions.Source.Substring(4));

                try
                {
                    var bmp = _screenShotModel.ScreenShotWindow(new Window(new IntPtr(ptr)));

                    _screenShotModel.SaveScreenShot(bmp, ShotOptions.FileName).Wait();
                }
                catch
                {
                    // Suppress Errors
                }
            }
            else
            {
                HandleVideoSource(ShotOptions);

                _screenShotModel.CaptureScreenShot(ShotOptions.FileName);
            }
        }

        void HandleVideoSource(CommonCmdOptions CommonOptions)
        {
            if (CommonOptions.Source == null)
                return;

            var provider = _videoSourceProviders.FirstOrDefault(M => M.ParseCli(CommonOptions.Source));

            if (provider != null)
            {
                _videoSourcesViewModel.RestoreSourceKind(provider);
            }
        }

        void HandleAudioSource(StartCmdOptions StartOptions)
        {
            var audioSource = ServiceProvider.Get<AudioSource>();

            if (StartOptions.Microphone != -1 && StartOptions.Microphone < audioSource.AvailableRecordingSources.Count)
            {
                _settings.Audio.Enabled = true;
                audioSource.AvailableRecordingSources[StartOptions.Microphone].Active = true;
            }

            if (StartOptions.Speaker != -1 && StartOptions.Speaker < audioSource.AvailableLoopbackSources.Count)
            {
                _settings.Audio.Enabled = true;
                audioSource.AvailableLoopbackSources[StartOptions.Speaker].Active = true;
            }
        }

        void HandleVideoEncoder(StartCmdOptions StartOptions)
        {
            if (StartOptions.Encoder == null)
                return;

            // FFmpeg
            if (FFmpegService.FFmpegExists && Regex.IsMatch(StartOptions.Encoder, @"^ffmpeg:\d+$"))
            {
                var index = int.Parse(StartOptions.Encoder.Substring(7));

                _videoWritersViewModel.SelectedVideoWriterKind = ServiceProvider.Get<FFmpegWriterProvider>();

                if (index < _videoWritersViewModel.AvailableVideoWriters.Count)
                    _videoWritersViewModel.SelectedVideoWriter = _videoWritersViewModel.AvailableVideoWriters[index];
            }

            // SharpAvi
            else if (ServiceProvider.FileExists("SharpAvi.dll") && Regex.IsMatch(StartOptions.Encoder, @"^sharpavi:\d+$"))
            {
                var index = int.Parse(StartOptions.Encoder.Substring(9));

                _videoWritersViewModel.SelectedVideoWriterKind = ServiceProvider.Get<SharpAviWriterProvider>();

                if (index < _videoWritersViewModel.AvailableVideoWriters.Count)
                    _videoWritersViewModel.SelectedVideoWriter = _videoWritersViewModel.AvailableVideoWriters[index];
            }

            // Gif
            else if (StartOptions.Encoder == "gif")
            {
                _videoWritersViewModel.SelectedVideoWriterKind = ServiceProvider.Get<GifWriterProvider>();
            }
        }

        void HandleWebcam(StartCmdOptions StartOptions)
        {
            if (StartOptions.Webcam != -1 && StartOptions.Webcam < _webCamProvider.AvailableCams.Count - 1)
            {
                _webCamProvider.SelectedCam = _webCamProvider.AvailableCams[StartOptions.Webcam + 1];

                // Sleep to prevent AccessViolationException
                Thread.Sleep(500);
            }
        }

        void Loop(StartCmdOptions StartOptions)
        {
            if (StartOptions.Length > 0)
            {
                var elapsed = 0;

                Write(TimeSpan.Zero);

                while (elapsed++ < StartOptions.Length)
                {
                    Thread.Sleep(1000);
                    Write(new string('\b', 8) + TimeSpan.FromSeconds(elapsed));
                }

                Write(new string('\b', 8));
            }
            else
            {
                const string recordingText = "Press p to pause or resume, q to quit";

                WriteLine(recordingText);

                char ReadChar()
                {
                    if (IsInputRedirected)
                    {
                        var line = ReadLine();

                        if (line != null && line.Length == 1)
                            return line[0];

                        return char.MinValue;
                    }

                    return char.ToLower(ReadKey(true).KeyChar);
                }

                char c;

                do
                {
                    c = ReadChar();

                    if (c != 'p')
                        continue;

                    _recordingModel.OnPauseExecute();

                    if (_recordingModel.RecorderState != RecorderState.Paused)
                    {
                        WriteLine("Resumed");
                    }
                } while (c != 'q');
            }
        }
    }
}