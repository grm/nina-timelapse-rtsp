# Timelapse RTSP - NINA Plugin

Automatically creates timelapse videos from an RTSP camera stream (e.g. an all-sky camera) during your imaging sessions with [N.I.N.A](https://nighttime-imaging.eu/).

## How It Works

1. Place **Start RTSP Timelapse** at the beginning of your Advanced Sequencer sequence
2. Place **Stop RTSP Timelapse** at the end (e.g. after parking the scope)
3. The plugin captures a frame from your RTSP stream at regular intervals
4. When the sequence hits Stop, it encodes all frames into an MP4 video
5. The video is saved to disk and/or sent to Discord, then temp files are cleaned up

## Requirements

- **NINA 3.x** (.NET 8)
- **ffmpeg** installed and accessible (in your system PATH, or configure the full path in plugin settings)
- An RTSP camera stream URL (tested with IP cameras, all-sky cams, etc.)

## Installation

1. Download the latest release from [Releases](https://github.com/grm/nina-timelapse-rtsp/releases)
2. Extract the contents to `%localappdata%\NINA\Plugins\3.0.0\TimelapseRTSP\`
3. Restart NINA — the plugin appears under **Plugins > Timelapse RTSP**

Or build from source:

```bash
git clone https://github.com/grm/nina-timelapse-rtsp.git
cd nina-timelapse-rtsp
dotnet build -c Release
```

The post-build step copies the DLL to your NINA plugins folder automatically.

## Sequence Instructions

| Instruction | Description |
|---|---|
| **Start RTSP Timelapse** | Begins frame capture. Optional per-instruction interval override (set to 0 to use global setting). |
| **Stop RTSP Timelapse** | Stops capture, encodes video, delivers to targets, cleans up temp frames. |

## Plugin Settings

### RTSP Configuration

| Setting | Default | Description |
|---|---|---|
| RTSP URL | `rtsp://` | Full stream URL (e.g. `rtsp://192.168.1.100:554/stream1`) |
| Username | *(empty)* | Camera auth username |
| Password | *(empty)* | Camera auth password |

### Timelapse Settings

| Setting | Default | Description |
|---|---|---|
| Capture Interval | 30s | Seconds between frame grabs. A 6-hour session at 30s = 720 frames = ~30s video at 24fps. |
| Target FPS | 24 | Output video frame rate |
| Video Codec | libx264 | ffmpeg codec name |
| Video Quality (CRF) | 23 | Lower = better quality but larger file (typical range: 18-28) |
| Max File Size | 10 MB | Re-encodes at lower bitrate if exceeded (0 = no limit) |
| Max Encode Retries | 3 | How many times to retry encoding to hit the file size target |
| Frame Resolution | Original | Downscale on capture: Original, 1920x1080, 1280x720, 854x480, 640x360 |

### Output Targets

| Setting | Default | Description |
|---|---|---|
| Save to file | Enabled | Saves the final video to a directory (default: `My Videos\NINA_Timelapse`) |
| Send to Discord | Disabled | Uploads via webhook (respects max file size setting) |

## Tips

- **Discord free tier (10 MB limit):** use 1280x720 resolution + 10 MB max file size
- **Long sessions:** increase the capture interval or lower the resolution
- **Manual recovery:** if encoding fails or is interrupted, temp frames remain in `%TEMP%\NINA_TimelapseRTSP\` 
- **File size targeting:** the encoder progressively reduces bitrate across retries (x0.95, x0.71, x0.53...) to fit within your limit

## License

[MPL-2.0](https://www.mozilla.org/en-US/MPL/2.0/)
