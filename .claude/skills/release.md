---
name: release
description: Create a new release by reviewing the changelog, proposing a version bump, tagging, and pushing to trigger the GitHub Actions release workflow.
---

# Release Skill

Creates a new release of the Timelapse RTSP plugin.

## Steps

1. Read `CHANGELOG.md` and show the user the `[Unreleased]` section.
2. Read the current version from `NINA.Plugin.TimelapseRTSP/Properties/AssemblyInfo.cs`.
3. Based on the changelog entries, recommend a version bump:
   - **Major** — if there are breaking changes or entries under `### Removed` / `### Changed` that break compatibility
   - **Minor** — if there are entries under `### Added` (new features)
   - **Patch** — if there are only entries under `### Fixed`, `### Security`, or `### Changed` (non-breaking tweaks)
4. Present the recommendation to the user with AskUserQuestion and let them choose (patch, minor, major).
5. Bump the version accordingly:
   - Format is `Major.Minor.Patch.Build` (e.g. `1.0.0.1`)
   - Patch bump: increment the 3rd number (1.0.0.1 -> 1.0.1.1)
   - Minor bump: increment the 2nd number, reset patch (1.0.1.1 -> 1.1.0.1)
   - Major bump: increment the 1st number, reset minor and patch (1.1.0.1 -> 2.0.0.1)
   - Build number (4th) always stays at 1.
6. Update both `AssemblyVersion` and `AssemblyFileVersion` in `Properties/AssemblyInfo.cs`.
7. Update `CHANGELOG.md`:
   - Rename `## [Unreleased]` to `## [X.Y.Z] - YYYY-MM-DD` (today's date)
   - Add a fresh empty `## [Unreleased]` section above it
8. Commit with message: `Release vX.Y.Z`
9. Create a git tag `vX.Y.Z`.
10. Push the commit and tag to origin. The tag push triggers the `release.yml` GitHub Actions workflow which builds, zips, and creates the GitHub Release automatically.
11. Report the release URL: `https://github.com/grm/nina-timelapse-rtsp/releases/tag/vX.Y.Z`
