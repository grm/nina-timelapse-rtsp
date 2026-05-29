using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Plugin.TimelapseRTSP.Options;
using NINA.Plugin.TimelapseRTSP.Services;
using NINA.Sequencer.SequenceItem;
using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Plugin.TimelapseRTSP.SequenceItems {

    [ExportMetadata("Name", "Start RTSP Timelapse")]
    [ExportMetadata("Description", "Begins grabbing frames from your RTSP camera at the configured interval. Place at the start of your sequence. Configure the stream URL and settings in the plugin options.")]
    [ExportMetadata("Icon", "CameraSVG")]
    [ExportMetadata("Category", "Timelapse RTSP")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class StartTimelapseCapture : SequenceItem {

        [JsonProperty]
        public double CaptureIntervalOverride { get; set; } = 0;

        [ImportingConstructor]
        public StartTimelapseCapture() {
        }

        private StartTimelapseCapture(StartTimelapseCapture cloneMe) : this() {
            CopyMetaData(cloneMe);
            CaptureIntervalOverride = cloneMe.CaptureIntervalOverride;
        }

        public override object Clone() {
            return new StartTimelapseCapture(this);
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            progress?.Report(new ApplicationStatus { Status = "Starting RTSP timelapse capture..." });

            if (CaptureIntervalOverride > 0) {
                TimelapseRTSPOptions.Instance.CaptureIntervalSeconds = CaptureIntervalOverride;
            }

            TimelapseSessionManager.StartNewSession(token);

            progress?.Report(new ApplicationStatus { Status = "RTSP timelapse capture active" });
        }

        public override string ToString() {
            var interval = CaptureIntervalOverride > 0
                ? CaptureIntervalOverride
                : TimelapseRTSPOptions.Instance?.CaptureIntervalSeconds ?? 30;
            return $"Start RTSP Timelapse (every {interval}s)";
        }
    }
}
