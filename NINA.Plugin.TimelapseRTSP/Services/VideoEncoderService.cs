using NINA.Core.Utility;
using NINA.Plugin.TimelapseRTSP.Options;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Plugin.TimelapseRTSP.Services {

    public class VideoEncoderService {

        public async Task<string> EncodeTimelapse(string framesDirectory, int frameCount, CancellationToken token) {
            var options = TimelapseRTSPOptions.Instance;

            var outputDir = !string.IsNullOrEmpty(options.OutputDirectory)
                ? options.OutputDirectory
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "NINA_Timelapse");

            Directory.CreateDirectory(outputDir);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var outputPath = Path.Combine(outputDir, $"timelapse_{timestamp}.mp4");
            var inputPattern = Path.Combine(framesDirectory, "frame_%06d.jpg");

            // First pass: encode with CRF
            await RunFfmpeg(options, BuildCrfArguments(options, inputPattern, outputPath), token);

            if (!File.Exists(outputPath)) {
                throw new FileNotFoundException($"TimelapseRTSP: Expected output file not found: {outputPath}");
            }

            var fileInfo = new FileInfo(outputPath);
            var maxBytes = (long)options.MaxFileSizeMB * 1024 * 1024;

            // If file exceeds target size, re-encode with progressively lower bitrate
            if (maxBytes > 0 && fileInfo.Length > maxBytes) {
                var durationSeconds = (double)frameCount / options.TargetFps;
                var attempt = 0;
                var maxAttempts = options.MaxEncodeRetries;
                var bitrateMultiplier = 0.95;

                while (fileInfo.Length > maxBytes && attempt < maxAttempts) {
                    attempt++;
                    var targetBitrateKbps = (int)((maxBytes * 8.0) / durationSeconds / 1000 * bitrateMultiplier);

                    Logger.Info($"TimelapseRTSP: Video ({fileInfo.Length / 1024 / 1024} MB) exceeds {options.MaxFileSizeMB} MB target. " +
                               $"Re-encoding attempt {attempt}/{maxAttempts} at {targetBitrateKbps} kbps...");

                    if (targetBitrateKbps < 100) {
                        Logger.Warning("TimelapseRTSP: Target bitrate too low for reasonable quality. Keeping current encode.");
                        break;
                    }

                    File.Delete(outputPath);
                    await RunFfmpeg(options, BuildBitrateArguments(options, inputPattern, outputPath, targetBitrateKbps), token);

                    if (!File.Exists(outputPath)) {
                        throw new FileNotFoundException($"TimelapseRTSP: Re-encoded output file not found: {outputPath}");
                    }

                    fileInfo = new FileInfo(outputPath);
                    bitrateMultiplier *= 0.75;
                }

                if (fileInfo.Length > maxBytes) {
                    Logger.Warning($"TimelapseRTSP: Could not fit video within {options.MaxFileSizeMB} MB after {options.MaxEncodeRetries} attempts. Final size: {fileInfo.Length / 1024 / 1024} MB");
                }
            }

            Logger.Info($"TimelapseRTSP: Timelapse encoded successfully. Size: {fileInfo.Length / 1024 / 1024} MB, Frames: {frameCount}");
            return outputPath;
        }

        private static string BuildCrfArguments(TimelapseRTSPOptions options, string inputPattern, string outputPath) {
            return $"-y -framerate {options.TargetFps} -i \"{inputPattern}\" " +
                   $"-c:v {options.VideoCodec} -crf {options.VideoQuality} " +
                   $"-pix_fmt yuv420p -movflags +faststart \"{outputPath}\"";
        }

        private static string BuildBitrateArguments(TimelapseRTSPOptions options, string inputPattern, string outputPath, int bitrateKbps) {
            return $"-y -framerate {options.TargetFps} -i \"{inputPattern}\" " +
                   $"-c:v {options.VideoCodec} -b:v {bitrateKbps}k -maxrate {bitrateKbps}k -bufsize {bitrateKbps * 2}k " +
                   $"-pix_fmt yuv420p -movflags +faststart \"{outputPath}\"";
        }

        private static async Task RunFfmpeg(TimelapseRTSPOptions options, string arguments, CancellationToken token) {
            Logger.Info($"TimelapseRTSP: Running ffmpeg: {arguments}");

            var startInfo = new ProcessStartInfo {
                FileName = options.FfmpegPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = startInfo }) {
                process.Start();

                var stderr = await process.StandardError.ReadToEndAsync();
                var completed = await WaitForProcessAsync(process, TimeSpan.FromMinutes(10), token);

                if (!completed) {
                    try { process.Kill(); } catch { }
                    throw new TimeoutException("TimelapseRTSP: Video encoding timed out after 10 minutes");
                }

                if (process.ExitCode != 0) {
                    throw new InvalidOperationException($"TimelapseRTSP: ffmpeg encoding failed: {stderr}");
                }
            }
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
    }
}
