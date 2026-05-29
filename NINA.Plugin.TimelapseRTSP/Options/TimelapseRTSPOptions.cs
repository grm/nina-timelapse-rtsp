using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NINA.Plugin.TimelapseRTSP.Options {

    public class TimelapseRTSPOptions : INotifyPropertyChanged {
        public static TimelapseRTSPOptions Instance { get; set; }

        private readonly IPluginOptionsAccessor optionsAccessor;

        public TimelapseRTSPOptions(IPluginOptionsAccessor optionsAccessor) {
            this.optionsAccessor = optionsAccessor;
        }

        public string RtspUrl {
            get => optionsAccessor.GetValueString(nameof(RtspUrl), "rtsp://");
            set { optionsAccessor.SetValueString(nameof(RtspUrl), value); OnPropertyChanged(); }
        }

        public string RtspUsername {
            get => optionsAccessor.GetValueString(nameof(RtspUsername), "");
            set { optionsAccessor.SetValueString(nameof(RtspUsername), value); OnPropertyChanged(); }
        }

        public string RtspPassword {
            get => optionsAccessor.GetValueString(nameof(RtspPassword), "");
            set { optionsAccessor.SetValueString(nameof(RtspPassword), value); OnPropertyChanged(); }
        }

        public double CaptureIntervalSeconds {
            get => optionsAccessor.GetValueDouble(nameof(CaptureIntervalSeconds), 30.0);
            set { optionsAccessor.SetValueDouble(nameof(CaptureIntervalSeconds), value); OnPropertyChanged(); }
        }

        public int TargetFps {
            get => optionsAccessor.GetValueInt32(nameof(TargetFps), 24);
            set { optionsAccessor.SetValueInt32(nameof(TargetFps), value); OnPropertyChanged(); }
        }

        public string FfmpegPath {
            get => optionsAccessor.GetValueString(nameof(FfmpegPath), "ffmpeg");
            set { optionsAccessor.SetValueString(nameof(FfmpegPath), value); OnPropertyChanged(); }
        }

        public string OutputDirectory {
            get => optionsAccessor.GetValueString(nameof(OutputDirectory), "");
            set { optionsAccessor.SetValueString(nameof(OutputDirectory), value); OnPropertyChanged(); }
        }

        public string DiscordWebhookUrl {
            get => optionsAccessor.GetValueString(nameof(DiscordWebhookUrl), "");
            set { optionsAccessor.SetValueString(nameof(DiscordWebhookUrl), value); OnPropertyChanged(); }
        }

        public bool SendToDiscord {
            get => optionsAccessor.GetValueBoolean(nameof(SendToDiscord), false);
            set { optionsAccessor.SetValueBoolean(nameof(SendToDiscord), value); OnPropertyChanged(); }
        }

        public bool SaveToFile {
            get => optionsAccessor.GetValueBoolean(nameof(SaveToFile), true);
            set { optionsAccessor.SetValueBoolean(nameof(SaveToFile), value); OnPropertyChanged(); }
        }

        public string VideoCodec {
            get => optionsAccessor.GetValueString(nameof(VideoCodec), "libx264");
            set { optionsAccessor.SetValueString(nameof(VideoCodec), value); OnPropertyChanged(); }
        }

        public int VideoQuality {
            get => optionsAccessor.GetValueInt32(nameof(VideoQuality), 23);
            set { optionsAccessor.SetValueInt32(nameof(VideoQuality), value); OnPropertyChanged(); }
        }

        public int MaxFileSizeMB {
            get => optionsAccessor.GetValueInt32(nameof(MaxFileSizeMB), 10);
            set { optionsAccessor.SetValueInt32(nameof(MaxFileSizeMB), value); OnPropertyChanged(); }
        }

        public int MaxEncodeRetries {
            get => optionsAccessor.GetValueInt32(nameof(MaxEncodeRetries), 3);
            set { optionsAccessor.SetValueInt32(nameof(MaxEncodeRetries), value); OnPropertyChanged(); }
        }

        public string FrameResolution {
            get => optionsAccessor.GetValueString(nameof(FrameResolution), "Original");
            set { optionsAccessor.SetValueString(nameof(FrameResolution), value); OnPropertyChanged(); }
        }

        public static string[] AvailableResolutions => new[] {
            "Original",
            "1920x1080",
            "1280x720",
            "854x480",
            "640x360"
        };

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
