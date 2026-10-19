# FolderFlow Store screenshots

Prepared and visually reviewed on 19 October 2026 using the current TechDeckLabs desktop build. The owner release kit contains `FolderFlow-Store-Screenshots.zip`, with seven PNG images, captions, and a checksum manifest. Each image is **2560 × 1392** pixels and smaller than 1 MB. These exceed Microsoft's PC minimum of 1366 × 768 and remain below its 50 MB per-image limit. [Microsoft screenshot requirements](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/screenshots-and-images)

| File | Caption |
| --- | --- |
| `01-choose-folders.png` | Choose the folders to copy from and copy to. |
| `02-preview-changes.png` | Preview proposed changes without copying or deleting files. |
| `03-copy-complete.png` | Read the copied, skipped, and failed file counts after a transfer. |
| `04-mirror-confirmation.png` | Review both paths before confirming deletion in the destination. |
| `05-settings.png` | Choose copying options, local logs, and light or dark appearance. |
| `06-about-techdecklabs.png` | FolderFlow by TechDeckLabs, TECHDECKLABS LLP. |
| `07-active-copy.png` | Follow the current operation and elapsed time, and stop when needed. |

All captions are under 200 characters. The screenshot ZIP SHA-256 is `5EBC1E826B7A723C65C8DE100FC5330645DE405A3FD34B45EAC1B5794285B7FF`.

## Capture evidence

The images show the running WPF application. The capture tool's original JPEG frames were re-encoded as PNG without changing their dimensions, adding overlays, or generating UI elements. Sample drive letters R and S point to disposable workspace folders; they do not represent a USB performance benchmark. The About window uses an isolated sample settings directory.

The preview reported 24 planned files and no changes. The subsequent real copy reported 24 copied, zero failed, and preserved the destination-only fixture. A second real copy of 5,000 sample files supplied the active-copy image and finished with zero failures. The mirror image shows the acknowledgement unchecked and the destructive action disabled; the demonstration returned without starting a mirror.

The source, folder names, and captions were reviewed for private information and unrelated system overlays. Both publisher names are visible in About, and the main window credits TechDeckLabs.

## Final release check

These are preparation captures of the current desktop build. After Partner Center verification and product reservation, build and test the production MSIX with its exact Store identity. Compare the installed package with these images and recapture any changed screens before submission. Microsoft certification, launch markets, final regional price review, and publication are still pending.
