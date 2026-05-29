---
name: beta
description: Create a pre-release test package by tagging and pushing a beta version to trigger a GitHub pre-release.
---

# Beta Skill

Creates a pre-release test package on GitHub.

## Steps

1. Read the current version from `NINA.Plugin.TimelapseRTSP/Properties/AssemblyInfo.cs`.
2. Check existing beta tags with `git tag -l "beta*"` to determine the next beta number.
   - Format: `beta-X.Y.Z.W-N` where X.Y.Z.W is the 4-part version from AssemblyInfo and N is an incrementing number (1, 2, 3...)
   - If no beta exists for this version, start at 1.
3. Create the tag `beta-X.Y.Z.W-N` on the current HEAD (no version bump, no changelog change).
4. Push the tag to origin. This triggers the `release.yml` workflow which builds, zips, and creates a GitHub **pre-release** automatically.
5. Report the pre-release URL: `https://github.com/grm/nina-timelapse-rtsp/releases/tag/beta-X.Y.Z.W-N`

## Notes

- Beta packages do NOT modify any files (no version bump, no changelog update).
- They are marked as pre-release on GitHub so they don't appear as the "latest" release.
- Testers can download the zip from the GitHub release page and extract to their NINA plugins folder.
- When ready for a real release, use `/release` instead.
