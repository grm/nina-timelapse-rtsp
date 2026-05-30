# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed
- Replace ffmpeg-based frame capture with LibVLCSharp for native RTSP Digest auth support

### Fixed
- RTSP password not being saved in plugin settings
- RTSP authentication failing on cameras requiring Digest auth (401 Unauthorized)

### Added
- Start RTSP Timelapse sequence instruction for the Advanced Sequencer
- Stop RTSP Timelapse sequence instruction (encode, deliver, cleanup)
- RTSP stream configuration (URL, username, password)
- Configurable capture interval (default: 30s)
- Configurable target FPS (default: 24)
- Frame resolution downscaling (Original, 1920x1080, 1280x720, 854x480, 640x360)
- Video codec and CRF quality settings
- Max file size targeting with configurable retry attempts
- Save to file output target
- Discord webhook output target
- FFmpeg path configuration
- In-app documentation for NINA plugin manager
- GitHub Actions CI build and release workflows
- Pre-release beta packaging via `/beta` skill
