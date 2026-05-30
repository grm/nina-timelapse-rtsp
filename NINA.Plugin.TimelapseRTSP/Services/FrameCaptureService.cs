using LibVLCSharp.Shared;
using NINA.Core.Utility;
using NINA.Plugin.TimelapseRTSP.Options;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Plugin.TimelapseRTSP.Services {

    public class FrameCaptureService {
        private string tempDirectory;
        private int frameCount;
        private CancellationTokenSource captureCts;
        private Task captureTask;
        private LibVLC libVLC;
        private MediaPlayer mediaPlayer;

        public string TempDirectory => tempDirectory;
        public int FrameCount => frameCount;
        public bool IsCapturing => captureTask != null && !captureTask.IsCompleted;

        public void StartCapture(CancellationToken externalToken) {
            var options = TimelapseRTSPOptions.Instance;
            tempDirectory = Path.Combine(Path.GetTempPath(), "NINA_TimelapseRTSP", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(tempDirectory);

            frameCount = 0;
            captureCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);

            InitializeVlc(options);

            captureTask = Task.Run(() => CaptureLoop(captureCts.Token), captureCts.Token);
            Logger.Info($"TimelapseRTSP: Started frame capture to {tempDirectory}");
        }

        public async Task StopCapture() {
            if (captureCts != null && !captureCts.IsCancellationRequested) {
                captureCts.Cancel();
            }

            if (captureTask != null) {
                try {
                    await captureTask;
                } catch (OperationCanceledException) {
                }
            }

            DisposeVlc();
            Logger.Info($"TimelapseRTSP: Stopped capture. {frameCount} frames captured.");
        }

        private void InitializeVlc(TimelapseRTSPOptions options) {
            libVLC = new LibVLC("--no-audio", "--rtsp-tcp");

            var url = options.RtspUrl;
            var media = new Media(libVLC, new Uri(url));

            if (!string.IsNullOrEmpty(options.RtspUsername)) {
                media.AddOption($":rtsp-user={options.RtspUsername}");
                media.AddOption($":rtsp-pwd={options.RtspPassword}");
            }

            media.AddOption(":network-caching=1000");
            media.AddOption(":no-video-title-show");

            mediaPlayer = new MediaPlayer(media);
            mediaPlayer.EnableHardwareDecoding = false;

            mediaPlayer.Play();

            Thread.Sleep(2000);
        }

        private void DisposeVlc() {
            try {
                if (mediaPlayer != null) {
                    mediaPlayer.Stop();
                    mediaPlayer.Dispose();
                    mediaPlayer = null;
                }

                if (libVLC != null) {
                    libVLC.Dispose();
                    libVLC = null;
                }
            } catch (Exception ex) {
                Logger.Warning($"TimelapseRTSP: Error disposing VLC: {ex.Message}");
            }
        }

        private async Task CaptureLoop(CancellationToken token) {
            var options = TimelapseRTSPOptions.Instance;
            var interval = TimeSpan.FromSeconds(options.CaptureIntervalSeconds);

            while (!token.IsCancellationRequested) {
                try {
                    CaptureFrame(options);
                    await Task.Delay(interval, token);
                } catch (OperationCanceledException) {
                    break;
                } catch (Exception ex) {
                    Logger.Error($"TimelapseRTSP: Frame capture error: {ex.Message}");
                    await Task.Delay(TimeSpan.FromSeconds(5), token);
                }
            }
        }

        private void CaptureFrame(TimelapseRTSPOptions options) {
            if (mediaPlayer == null || !mediaPlayer.IsPlaying) {
                Logger.Warning("TimelapseRTSP: VLC media player not playing, skipping frame");
                return;
            }

            var outputPath = Path.Combine(tempDirectory, $"frame_{frameCount:D6}.jpg");

            var width = GetTargetWidth(options.FrameResolution);
            var height = GetTargetHeight(options.FrameResolution);

            bool success;
            if (width > 0 && height > 0) {
                success = mediaPlayer.TakeSnapshot(0, outputPath, (uint)width, (uint)height);
            } else {
                success = mediaPlayer.TakeSnapshot(0, outputPath, 0, 0);
            }

            if (success && File.Exists(outputPath)) {
                Interlocked.Increment(ref frameCount);
                Logger.Trace($"TimelapseRTSP: Captured frame {frameCount}");
            } else {
                Logger.Warning("TimelapseRTSP: Snapshot failed");
            }
        }

        private static int GetTargetWidth(string resolution) {
            if (string.IsNullOrEmpty(resolution) || resolution == "Original") return 0;
            var parts = resolution.Split('x');
            return parts.Length == 2 && int.TryParse(parts[0], out var w) ? w : 0;
        }

        private static int GetTargetHeight(string resolution) {
            if (string.IsNullOrEmpty(resolution) || resolution == "Original") return 0;
            var parts = resolution.Split('x');
            return parts.Length == 2 && int.TryParse(parts[1], out var h) ? h : 0;
        }

        public void Cleanup() {
            if (!string.IsNullOrEmpty(tempDirectory) && Directory.Exists(tempDirectory)) {
                try {
                    Directory.Delete(tempDirectory, true);
                    Logger.Info($"TimelapseRTSP: Cleaned up temp directory {tempDirectory}");
                } catch (Exception ex) {
                    Logger.Warning($"TimelapseRTSP: Failed to cleanup temp directory: {ex.Message}");
                }
            }
        }
    }
}
