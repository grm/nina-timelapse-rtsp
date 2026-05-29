using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Plugin.TimelapseRTSP.Options;
using NINA.Plugin.TimelapseRTSP.Services;
using NINA.Plugin.TimelapseRTSP.Targets;
using NINA.Sequencer.SequenceItem;
using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Plugin.TimelapseRTSP.SequenceItems {

    [ExportMetadata("Name", "Stop RTSP Timelapse")]
    [ExportMetadata("Description", "Stops RTSP frame capture, encodes the timelapse video, sends to targets, and cleans up")]
    [ExportMetadata("Icon", "StopSVG")]
    [ExportMetadata("Category", "Timelapse RTSP")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class StopTimelapseCapture : SequenceItem {

        [ImportingConstructor]
        public StopTimelapseCapture() {
        }

        private StopTimelapseCapture(StopTimelapseCapture cloneMe) : this() {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new StopTimelapseCapture(this);
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            var session = TimelapseSessionManager.GetActiveSession();

            if (session == null || !session.IsCapturing) {
                Logger.Warning("TimelapseRTSP: No active capture session to stop");
                return;
            }

            // Stop capturing frames
            progress?.Report(new ApplicationStatus { Status = "Stopping RTSP timelapse capture..." });
            await session.StopCapture();

            if (session.FrameCount == 0) {
                Logger.Warning("TimelapseRTSP: No frames were captured. Skipping encoding.");
                session.Cleanup();
                TimelapseSessionManager.ClearSession();
                return;
            }

            // Encode video
            progress?.Report(new ApplicationStatus { Status = $"Encoding timelapse ({session.FrameCount} frames)..." });
            var encoder = new VideoEncoderService();
            string videoPath;

            try {
                videoPath = await encoder.EncodeTimelapse(session.TempDirectory, session.FrameCount, token);
            } catch (Exception ex) {
                Logger.Error($"TimelapseRTSP: Encoding failed: {ex.Message}");
                session.Cleanup();
                TimelapseSessionManager.ClearSession();
                throw;
            }

            // Send to targets
            progress?.Report(new ApplicationStatus { Status = "Sending timelapse to targets..." });

            try {
                await DiscordTarget.SendVideo(videoPath, token);
            } catch (Exception ex) {
                Logger.Error($"TimelapseRTSP: Failed to send to Discord: {ex.Message}");
            }

            // Cleanup temp frames
            progress?.Report(new ApplicationStatus { Status = "Cleaning up temporary files..." });
            session.Cleanup();
            TimelapseSessionManager.ClearSession();

            progress?.Report(new ApplicationStatus { Status = "Timelapse complete" });
            Logger.Info($"TimelapseRTSP: Timelapse workflow complete. Video at: {videoPath}");
        }

        public override string ToString() {
            return "Stop RTSP Timelapse & Encode";
        }
    }
}
