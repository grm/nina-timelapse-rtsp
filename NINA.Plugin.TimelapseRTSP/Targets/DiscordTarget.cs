using NINA.Core.Utility;
using NINA.Plugin.TimelapseRTSP.Options;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Plugin.TimelapseRTSP.Targets {

    public static class DiscordTarget {
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task SendVideo(string videoPath, CancellationToken token) {
            var options = TimelapseRTSPOptions.Instance;

            if (!options.SendToDiscord || string.IsNullOrEmpty(options.DiscordWebhookUrl)) {
                return;
            }

            var fileInfo = new FileInfo(videoPath);
            var maxBytes = (long)options.MaxFileSizeMB * 1024 * 1024;

            if (maxBytes > 0 && fileInfo.Length > maxBytes) {
                Logger.Warning($"TimelapseRTSP: Video file ({fileInfo.Length / 1024 / 1024} MB) exceeds configured {options.MaxFileSizeMB} MB limit. Skipping Discord upload.");
                return;
            }

            Logger.Info($"TimelapseRTSP: Sending timelapse to Discord ({fileInfo.Length / 1024 / 1024} MB)");

            using (var content = new MultipartFormDataContent()) {
                var fileBytes = await ReadFileAsync(videoPath, token);
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("video/mp4");
                content.Add(fileContent, "file", Path.GetFileName(videoPath));

                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                content.Add(new StringContent($"Timelapse from imaging session ({timestamp})"), "content");

                var response = await httpClient.PostAsync(options.DiscordWebhookUrl, content, token);

                if (!response.IsSuccessStatusCode) {
                    var error = await response.Content.ReadAsStringAsync();
                    Logger.Error($"TimelapseRTSP: Discord webhook failed ({response.StatusCode}): {error}");
                } else {
                    Logger.Info("TimelapseRTSP: Timelapse sent to Discord successfully");
                }
            }
        }

        private static async Task<byte[]> ReadFileAsync(string path, CancellationToken token) {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true)) {
                var bytes = new byte[stream.Length];
                await stream.ReadAsync(bytes, 0, bytes.Length, token);
                return bytes;
            }
        }
    }
}
