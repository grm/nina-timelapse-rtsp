using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("Timelapse RTSP")]
[assembly: AssemblyDescription("Creates timelapses from RTSP camera streams during imaging sessions")]
[assembly: AssemblyCompany("Jeremie Klein")]
[assembly: AssemblyProduct("NINA.Plugin.TimelapseRTSP")]
[assembly: AssemblyCopyright("Copyright © 2026 Jeremie Klein")]
[assembly: Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890")]
[assembly: AssemblyVersion("0.0.1.0")]
[assembly: AssemblyFileVersion("0.0.1.0")]
[assembly: AssemblyMetadata("MinimumApplicationVersion", "3.0.0.2017")]
[assembly: AssemblyMetadata("License", "MPL-2.0")]
[assembly: AssemblyMetadata("LicenseURL", "https://www.mozilla.org/en-US/MPL/2.0/")]
[assembly: AssemblyMetadata("Repository", "https://github.com/grm/nina-timelapse-rtsp")]
[assembly: AssemblyMetadata("Tags", "Timelapse,RTSP,Video,Camera")]
[assembly: AssemblyMetadata("LongDescription", @"# Timelapse RTSP

Automatically creates timelapse videos from an RTSP camera stream (e.g. an all-sky camera) during your imaging sessions.

## How It Works

1. Place **Start RTSP Timelapse** at the beginning of your sequence
2. Place **Stop RTSP Timelapse** at the end (e.g. after parking the scope)
3. The plugin captures a frame from your RTSP stream at regular intervals
4. When the sequence hits Stop, it encodes all frames into an MP4 video
5. The video is saved to disk and/or sent to Discord, then temp files are cleaned up

## Requirements

- **ffmpeg** must be installed and accessible (in your system PATH, or configure the full path in plugin settings)
- An RTSP camera stream URL (tested with IP cameras, all-sky cams, etc.)

## Sequence Instructions

| Instruction | Category | Description |
|---|---|---|
| Start RTSP Timelapse | Timelapse RTSP | Begins frame capture. Optional per-instruction interval override (0 = use global setting). |
| Stop RTSP Timelapse | Timelapse RTSP | Stops capture, encodes video, delivers to targets, cleans up temp frames. |

## Plugin Settings

### RTSP Configuration
- **RTSP URL** — Full stream URL (e.g. rtsp://192.168.1.100:554/stream1)
- **Username / Password** — Credentials if your camera requires authentication

### Timelapse Settings
- **Capture Interval** — Seconds between frame grabs (default: 30s). A 6-hour session at 30s = 720 frames = ~30s video at 24fps.
- **Target FPS** — Output video frame rate (default: 24)
- **Video Codec** — ffmpeg codec name (default: libx264)
- **Video Quality (CRF)** — Constant Rate Factor, lower = better quality but larger file (default: 23, typical range 18-28)
- **Max File Size (MB)** — If the video exceeds this, it is re-encoded at a lower bitrate (default: 10 MB, set 0 to disable)
- **Max Encode Retries** — How many times to retry encoding to hit the file size target (default: 3)
- **Frame Resolution** — Downscale frames on capture: Original, 1920x1080, 1280x720, 854x480, or 640x360

### Output Targets
- **Save to file** — Save the final video to a directory (default: My Videos\NINA_Timelapse)
- **Send to Discord** — Upload via webhook (respects the max file size setting)

## Tips

- For Discord (free tier, 10 MB limit): use 1280x720 resolution + 10 MB max file size
- For longer sessions, increase the capture interval or lower the resolution
- The plugin handles the full lifecycle: capture → encode → deliver → cleanup
- If encoding fails or is interrupted, temp frames remain in %TEMP%\NINA_TimelapseRTSP for manual recovery
")]
