# Noise File Detection Specification

## Purpose

This document lists the known noise file names filtered by the client inventory pipeline and their platform origin.

The runtime source of truth is the embedded JSON resource:
`src/ByteSync.Client/Services/Inventories/noise-files.json`.

## Known noise file names

| File name | Origin platform | Typical purpose |
| --- | --- | --- |
| `desktop.ini` | Windows | Folder customization metadata |
| `thumbs.db` | Windows | Thumbnail cache |
| `ehthumbs.db` | Windows | Media Center thumbnail cache |
| `ehthumbs_vista.db` | Windows | Vista Media Center thumbnail cache |
| `$RECYCLE.BIN` | Windows | Recycle bin folder marker |
| `.desktop.ini` | Windows/Linux legacy compatibility | Legacy hidden variant |
| `.thumbs.db` | Windows/Linux legacy compatibility | Legacy hidden variant |
| `.DS_Store` | macOS | Finder metadata |
| `.AppleDouble` | macOS | Resource fork metadata |
| `.AppleDB` | macOS | Apple database file |
| `.AppleDesktop` | macOS | Apple desktop database file |
| `.LSOverride` | macOS | Launch Services overrides |
| `.Spotlight-V100` | macOS | Spotlight indexing data |
| `.Trashes` | macOS | Trash metadata or folder marker |
| `.fseventsd` | macOS | File system event metadata |
| `.TemporaryItems` | macOS | Temporary items marker |
| `.VolumeIcon.icns` | macOS | Custom volume icon |
| `.directory` | Linux (KDE) | Directory display metadata |

## Matching behavior

- On Linux, matching is case-sensitive.
- On non-Linux platforms (including macOS), matching is case-insensitive; macOS remains intentionally case-insensitive for consistency with current product behavior and to reflect the deviation from the original issue wording.
