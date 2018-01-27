﻿using CommandLine;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Captura
{
    // ReSharper disable once ClassNeverInstantiated.Global
    [Verb("start", HelpText = "Start Recording")]
    class StartCmdOptions : CommonCmdOptions
    {
        public StartCmdOptions()
        {
            var settings = ServiceProvider.Get<Settings>();

            settings.Keystrokes.Display = settings.Clicks.Display = false;

            FrameRate = settings.FrameRate;
            VideoQuality = settings.VideoQuality;
            AudioQuality = settings.AudioQuality;
        }

        [Option("delay", HelpText = "Milliseconds to wait before starting recording.")]
        public int Delay { get; set; }

        [Option("length", HelpText = "Length of Recording in seconds.")]
        public int Length { get; set; }
        
        [Option("keys", HelpText = "Include Keystrokes in Recording.")]
        public bool Keys { get; set; }

        [Option("clicks", HelpText = "Include Mouse Clicks in Recording.")]
        public bool Clicks { get; set; }

        [Option("mic", Default = -1, HelpText = "Index of Microphone source.")]
        public int Microphone { get; set; }

        [Option("speaker", Default = -1, HelpText = "Index of Speaker output source.")]
        public int Speaker { get; set; }

        [Option('r', "framerate", HelpText = "Recording frame rate.")]
        public int FrameRate { get; set; }

        [Option("encoder", HelpText = "Video encoder to use.")]
        public string Encoder { get; set; }

        [Option("vq", HelpText = "Video Quality")]
        public int VideoQuality { get; set; }

        [Option("aq", HelpText = "Audio Quality")]
        public int AudioQuality { get; set; }
    }
}
