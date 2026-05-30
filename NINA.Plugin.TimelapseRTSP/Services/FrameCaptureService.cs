using LibVLCSharp.Shared;
using NINA.Core.Utility;
using NINA.Plugin.TimelapseRTSP.Options;
using System;
using System.IO;
using System.Runtime.InteropServices;
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

        private IntPtr currentFrameBuffer;
        private readonly object frameLock = new object();
        private int videoWidth;
        private int videoHeight;
        private int videoPitch;
        private bool hasFrame;

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
            libVLC = new LibVLC("--no-audio", "--rtsp-tcp", "--no-video-title-show");

            var media = new Media(libVLC, new Uri(options.RtspUrl));

            if (!string.IsNullOrEmpty(options.RtspUsername)) {
                media.AddOption($":rtsp-user={options.RtspUsername}");
                media.AddOption($":rtsp-pwd={options.RtspPassword}");
            }

            media.AddOption(":network-caching=1000");

            mediaPlayer = new MediaPlayer(media);

            mediaPlayer.SetVideoFormatCallbacks(VideoFormatSetup, null);
            mediaPlayer.SetVideoCallbacks(LockVideo, UnlockVideo, DisplayVideo);

            mediaPlayer.Play();
        }

        private uint VideoFormatSetup(ref IntPtr opaque, IntPtr chroma, ref uint width, ref uint height, ref uint pitches, ref uint lines) {
            var chromaBytes = System.Text.Encoding.ASCII.GetBytes("RV24");
            Marshal.Copy(chromaBytes, 0, chroma, 4);

            videoWidth = (int)width;
            videoHeight = (int)height;
            videoPitch = (int)width * 3;

            pitches = (uint)videoPitch;
            lines = height;

            lock (frameLock) {
                if (currentFrameBuffer != IntPtr.Zero) {
                    Marshal.FreeHGlobal(currentFrameBuffer);
                }
                currentFrameBuffer = Marshal.AllocHGlobal(videoPitch * videoHeight);
            }

            return 1;
        }

        private IntPtr LockVideo(IntPtr opaque, IntPtr planes) {
            lock (frameLock) {
                Marshal.WriteIntPtr(planes, currentFrameBuffer);
            }
            return IntPtr.Zero;
        }

        private void UnlockVideo(IntPtr opaque, IntPtr picture, IntPtr planes) {
        }

        private void DisplayVideo(IntPtr opaque, IntPtr picture) {
            hasFrame = true;
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

                lock (frameLock) {
                    if (currentFrameBuffer != IntPtr.Zero) {
                        Marshal.FreeHGlobal(currentFrameBuffer);
                        currentFrameBuffer = IntPtr.Zero;
                    }
                }
            } catch (Exception ex) {
                Logger.Warning($"TimelapseRTSP: Error disposing VLC: {ex.Message}");
            }
        }

        private async Task CaptureLoop(CancellationToken token) {
            var options = TimelapseRTSPOptions.Instance;
            var interval = TimeSpan.FromSeconds(options.CaptureIntervalSeconds);

            // Wait for first frame
            var waitStart = DateTime.UtcNow;
            while (!hasFrame && !token.IsCancellationRequested && (DateTime.UtcNow - waitStart).TotalSeconds < 15) {
                await Task.Delay(200, token);
            }

            if (!hasFrame) {
                Logger.Error("TimelapseRTSP: Timed out waiting for first frame from RTSP stream");
                return;
            }

            Logger.Info("TimelapseRTSP: Stream connected, starting frame capture");

            while (!token.IsCancellationRequested) {
                try {
                    SaveCurrentFrame(options);
                    await Task.Delay(interval, token);
                } catch (OperationCanceledException) {
                    break;
                } catch (Exception ex) {
                    Logger.Error($"TimelapseRTSP: Frame capture error: {ex.Message}");
                    await Task.Delay(TimeSpan.FromSeconds(5), token);
                }
            }
        }

        private void SaveCurrentFrame(TimelapseRTSPOptions options) {
            if (!hasFrame || currentFrameBuffer == IntPtr.Zero) {
                Logger.Warning("TimelapseRTSP: No frame available to save");
                return;
            }

            var outputPath = Path.Combine(tempDirectory, $"frame_{frameCount:D6}.bmp");

            lock (frameLock) {
                try {
                    var targetWidth = GetTargetWidth(options.FrameResolution);
                    var targetHeight = GetTargetHeight(options.FrameResolution);
                    var w = targetWidth > 0 ? targetWidth : videoWidth;
                    var h = targetHeight > 0 ? targetHeight : videoHeight;

                    WriteBmp(outputPath, currentFrameBuffer, videoWidth, videoHeight, videoPitch, w, h);
                } catch (Exception ex) {
                    Logger.Warning($"TimelapseRTSP: Failed to save frame: {ex.Message}");
                    return;
                }
            }

            if (File.Exists(outputPath)) {
                Interlocked.Increment(ref frameCount);
                Logger.Trace($"TimelapseRTSP: Captured frame {frameCount}");
            }
        }

        private static void WriteBmp(string path, IntPtr rgbData, int srcWidth, int srcHeight, int srcPitch, int dstWidth, int dstHeight) {
            // Write a BMP file from RGB24 data (bottom-up format)
            var rowSize = ((dstWidth * 3 + 3) / 4) * 4;
            var imageSize = rowSize * dstHeight;
            var fileSize = 54 + imageSize;

            using (var fs = new FileStream(path, FileMode.Create)) {
                using (var bw = new BinaryWriter(fs)) {
                    // BMP header
                    bw.Write((ushort)0x4D42); // 'BM'
                    bw.Write(fileSize);
                    bw.Write(0); // reserved
                    bw.Write(54); // pixel data offset

                    // DIB header
                    bw.Write(40); // header size
                    bw.Write(dstWidth);
                    bw.Write(dstHeight);
                    bw.Write((ushort)1); // planes
                    bw.Write((ushort)24); // bpp
                    bw.Write(0); // no compression
                    bw.Write(imageSize);
                    bw.Write(2835); // h resolution (72 DPI)
                    bw.Write(2835); // v resolution
                    bw.Write(0); // colors
                    bw.Write(0); // important colors

                    // Pixel data - BMP is bottom-up, VLC RV24 is top-down BGR
                    var rowBuffer = new byte[srcPitch];
                    var paddedRow = new byte[rowSize];

                    for (int y = dstHeight - 1; y >= 0; y--) {
                        var srcY = (srcHeight == dstHeight) ? y : (int)((long)y * srcHeight / dstHeight);
                        Marshal.Copy(rgbData + srcY * srcPitch, rowBuffer, 0, Math.Min(srcPitch, rowBuffer.Length));

                        if (srcWidth == dstWidth) {
                            Buffer.BlockCopy(rowBuffer, 0, paddedRow, 0, dstWidth * 3);
                        } else {
                            for (int x = 0; x < dstWidth; x++) {
                                var srcX = (int)((long)x * srcWidth / dstWidth);
                                paddedRow[x * 3] = rowBuffer[srcX * 3];
                                paddedRow[x * 3 + 1] = rowBuffer[srcX * 3 + 1];
                                paddedRow[x * 3 + 2] = rowBuffer[srcX * 3 + 2];
                            }
                        }

                        bw.Write(paddedRow);
                    }
                }
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
