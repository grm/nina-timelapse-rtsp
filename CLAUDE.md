# NINA Timelapse RTSP Plugin

## Changelog Policy

**Every time you make a code change (feature, fix, refactor, removal), you MUST update `CHANGELOG.md`:**

1. Add the change under the `## [Unreleased]` section
2. Use the appropriate subsection:
   - `### Added` — new features or capabilities
   - `### Changed` — changes to existing functionality
   - `### Fixed` — bug fixes
   - `### Removed` — removed features
   - `### Security` — security-related fixes
3. Each entry should be a single concise line describing what changed from the user's perspective
4. Do NOT create entries for internal refactors that don't affect user-facing behavior

This changelog is used by the `/release` skill to determine the appropriate version bump and generate release notes.

## Project Structure

- `NINA.Plugin.TimelapseRTSP/` — Main plugin project (.NET 8, WPF)
- `Properties/AssemblyInfo.cs` — Version and plugin manifest metadata
- `Options/` — Plugin settings (options page UI + backing model)
- `SequenceItems/` — Advanced Sequencer instructions (Start/Stop)
- `Services/` — Frame capture, video encoding, session management
- `Targets/` — Output delivery (Discord, file)
- `.github/workflows/` — CI build + release automation
