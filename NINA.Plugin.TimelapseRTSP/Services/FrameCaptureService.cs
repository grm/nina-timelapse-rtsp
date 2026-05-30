using NINA.Core.Utility;
using NINA.Plugin.TimelapseRTSP.Options;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Plugin.TimelapseRTSP.Services {

    public class FrameCaptureService {
        private string tempDirectory;
        private int frameCount;
        private CancellationTokenSource captureCts;
        private Task captureTask;

        public string TempDirectory => tempDirectory;
        public int FrameCount => frameCount;
        public bool IsCapturing => captureTask != null && !captureTask.IsCompleted;

        public void StartCapture(CancellationToken externalToken) {
            var options = TimelapseRTSPOptions.Instance;
            tempDirectory = Path.Combine(Path.GetTempPath(), "NINA_TimelapseRTSP", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(tempDirectory);

            frameCount = 0;
            captureCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);

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

            Logger.Info($"TimelapseRTSP: Stopped capture. {frameCount} frames captured.");
        }

        private async Task CaptureLoop(CancellationToken token) {
            var options = TimelapseRTSPOptions.Instance;
            var interval = TimeSpan.FromSeconds(options.CaptureIntervalSeconds);

            while (!token.IsCancellationRequested) {
                try {
                    await CaptureFrame(token);
                    await Task.Delay(interval, token);
                } catch (OperationCanceledException) {
                    break;
                } catch (Exception ex) {
                    Logger.Error($"TimelapseRTSP: Frame capture error: {ex.Message}");
                    await Task.Delay(TimeSpan.FromSeconds(5), token);
                }
            }
        }

        private async Task CaptureFrame(CancellationToken token) {
            var options = TimelapseRTSPOptions.Instance;
            var outputPath = Path.Combine(tempDirectory, $"frame_{frameCount:D6}.jpg");

            var rtspUrl = BuildRtspUrl(options);

            var scaleFilter = BuildScaleFilter(options.FrameResolution);
            var filterArgs = string.IsNullOrEmpty(scaleFilter) ? "" : $"-vf \"{scaleFilter}\" ";

            var startInfo = new ProcessStartInfo {
                FileName = options.FfmpegPath,
                Arguments = $"-y -rtsp_transport tcp -rtsp_flags prefer_tcp -stimeout 10000000 -i \"{rtspUrl}\" -frames:v 1 {filterArgs}-q:v 2 \"{outputPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = startInfo }) {
                process.Start();

                var completed = await WaitForProcessAsync(process, TimeSpan.FromSeconds(30), token);

                if (!completed) {
                    try { process.Kill(); } catch { }
                    Logger.Warning("TimelapseRTSP: Frame capture timed out");
                    return;
                }

                if (process.ExitCode != 0) {
                    var error = await process.StandardError.ReadToEndAsync();
                    Logger.Warning($"TimelapseRTSP: ffmpeg frame capture failed: {error}");
                    return;
                }
            }

            if (File.Exists(outputPath)) {
                Interlocked.Increment(ref frameCount);
                Logger.Trace($"TimelapseRTSP: Captured frame {frameCount}");
            }
        }

        private static string BuildScaleFilter(string resolution) {
            if (string.IsNullOrEmpty(resolution) || resolution == "Original") {
                return null;
            }
            var parts = resolution.Split('x');
            if (parts.Length == 2) {
                return $"scale={parts[0]}:{parts[1]}";
            }
            return null;
        }

        private static string BuildRtspUrl(TimelapseRTSPOptions options) {
            var url = options.RtspUrl;
            if (!string.IsNullOrEmpty(options.RtspUsername)) {
                var uri = new Uri(url);
                if (string.IsNullOrEmpty(uri.UserInfo)) {
                    var user = Uri.EscapeDataString(options.RtspUsername);
                    var pass = Uri.EscapeDataString(options.RtspPassword ?? "");
                    var port = uri.Port > 0 ? uri.Port : 554;
                    url = $"{uri.Scheme}://{user}:{pass}@{uri.Host}:{port}{uri.PathAndQuery}";
                }
            }
            return url;
        }

        private static async Task<bool> WaitForProcessAsync(Process process, TimeSpan timeout, CancellationToken token) {
            var tcs = new TaskCompletionSource<bool>();

            process.EnableRaisingEvents = true;
            process.Exited += (s, e) => tcs.TrySetResult(true);

            if (process.HasExited) {
                return true;
            }

            using (token.Register(() => tcs.TrySetCanceled())) {
                var timeoutTask = Task.Delay(timeout, token);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
                return completedTask == tcs.Task && tcs.Task.Result;
            }
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
