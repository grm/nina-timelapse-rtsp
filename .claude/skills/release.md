---
name: release
description: Create a new release by bumping the version, tagging, and pushing to trigger the GitHub Actions release workflow.
---

# Release Skill

Creates a new release of the Timelapse RTSP plugin.

## Steps

1. Ask the user what kind of version bump they want (patch, minor, major) if not specified.
2. Read the current version from `NINA.Plugin.TimelapseRTSP/Properties/AssemblyInfo.cs` (both `AssemblyVersion` and `AssemblyFileVersion`).
3. Bump the version accordingly:
   - Format is `Major.Minor.Patch.Build` (e.g. `1.0.0.1`)
   - Patch bump: increment the 3rd number (1.0.0.1 -> 1.0.1.1)
   - Minor bump: increment the 2nd number, reset patch (1.0.1.1 -> 1.1.0.1)
   - Major bump: increment the 1st number, reset minor and patch (1.1.0.1 -> 2.0.0.1)
   - Build number (4th) always stays at 1.
4. Update both `AssemblyVersion` and `AssemblyFileVersion` in `Properties/AssemblyInfo.cs`.
5. Commit the version bump with message: `Bump version to X.Y.Z.1`
6. Create a git tag `vX.Y.Z` (without the build number).
7. Push the commit and tag to origin. The tag push triggers the `release.yml` GitHub Actions workflow which builds, zips, and creates the GitHub Release automatically.
8. Report the release URL: `https://github.com/grm/nina-timelapse-rtsp/releases/tag/vX.Y.Z`
