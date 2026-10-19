# FolderFlow — Store listing draft

Draft for the current Copy/Mirror/Stop application. This has not been submitted to Microsoft. Check every claim against the final installed package. Fields marked **Owner input** must not be replaced by guesses.

## Listing fields

| Field | Draft / status |
| --- | --- |
| Product name | FolderFlow, subject to successful name reservation |
| Publisher | **TechDeckLabs**, legal company **TECHDECKLABS LLP**. Company account verification is pending; exact package identity must come from Partner Center after onboarding. |
| Type | App, MSIX, Windows PC |
| Category | Utilities & tools, or the closest available utility category |
| Language | English, matching the current UI |
| Architecture | x64 for the first validated package |
| Price | **Owner-approved:** one-time **US$4.99 base / ₹299 India override**; not yet entered into Partner Center or published. See [pricing decisions](PRICING.md). |
| Trial | No free trial for the current version |
| Website | Existing project: <https://github.com/agbautomated/FolderFlow> |
| Support | **Owner input:** verified monitored public URL or email |
| Privacy policy | **Owner input:** verified public policy URL |
| Copyright / source license | **© 2026 TECHDECKLABS LLP**. Source license remains an owner decision and has not been chosen. |

## Short description

Copy folders between connected drives with clear controls, previews, and detailed results.

## Product description

FolderFlow uses the RoboCopy utility included with Windows to copy folders between drives connected to your PC.

Choose where to copy from, choose where to copy to, and start. It is designed for straightforward transfers such as copying photos, documents, or videos from a USB hard drive to a USB SSD.

Copy files adds new files and replaces changed destination versions while keeping unrelated destination files. Preview changes lets you inspect a proposed operation without copying or deleting files.

When you deliberately want the destination to match the source, choose Mirror folders. FolderFlow shows the selected paths and asks for confirmation before a mirror operation can delete destination items that are missing from the source.

During a transfer, see elapsed time and the current operation or file when available. Stop an active transfer and read the result. Open detailed diagnostics to investigate skipped or failed files, and save local logs when you need them.

Settings include a conservative option to copy a few files at once, an optional large-file setting, bounded retries, and light or dark appearance.

FolderFlow operates on existing local folders. It does not support network shares, source-deleting moves, or pausing a transfer. Stopping does not undo completed copying or mirror deletions; rerun a stopped copy to finish remaining work. Copying can replace existing files, and mirroring can delete destination files. Keep an independent backup of valuable data.

Transfer speed depends on your drives, USB hardware, and files. FolderFlow does not guarantee faster copying, provide file version history, or verify file contents by cryptographic hash. The activity view does not claim an overall completion percentage.

FolderFlow is an independent application and is not affiliated with or endorsed by Microsoft.

## Feature bullets

- Choose source and destination folders using Windows folder pickers.
- Copy new and changed files while keeping other destination files.
- Preview proposed copying and mirroring before changing files.
- Mirror folders with explicit deletion confirmation.
- View live activity, elapsed time, completion results, and diagnostic details.
- Stop a transfer and rerun a copy to recover remaining work.
- Save local logs and choose light, dark, or system appearance.

## Search terms

Proposed seven terms: `folder copy`, `file transfer`, `USB drive`, `external drive`, `folder mirror`, `copy preview`, `SSD transfer`.

## System requirements and limits

The first package targets Windows 11 x64, minimum build 22000. Use the final package's validated Windows requirements in Partner Center; do not advertise untested Windows releases. An x64 package is the only architecture prepared for the first release. Windows RoboCopy must be present. The self-contained package includes the .NET runtime. Normal file/folder permissions apply, and administrator rights are not requested.

Selected source and destination must be existing local subfolders. The app refuses several unsafe configurations, including matching/nested folders, drive roots, protected destinations, and detected destination links. These safeguards do not make copying transactional and cannot prevent another program from changing files during a run.

## Release notes draft

Initial Microsoft Store release: folder copying, deliberate mirroring, previews, live activity, Stop, local diagnostic logs, saved preferences, and light/dark appearance.

Only use “initial Microsoft Store release” once the product is actually submitted as its first Store version. Do not imply Microsoft approval while this remains a draft.

## Screenshot plan and captions

Capture the real installed app using sample folders such as `E:\Sample Photos` and `F:\Photo Copy`. Use a disposable destination for mirror demonstrations. PC screenshots must be PNG, at least 1366 × 768 pixels and no larger than 50 MB. Captions are limited to 200 characters. Do not add external marketing overlays. [Microsoft artwork requirements](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/screenshots-and-images)

| Screenshot | Proposed caption |
| --- | --- |
| Main window with sample source/destination | Choose the folders to copy from and copy to. |
| Completed preview with details | Preview proposed changes without copying or deleting files. |
| Active copy | Follow the current operation and elapsed time, and stop when needed. |
| Mirror confirmation | Review both paths and confirm destination deletion before mirroring. |
| Successful result and details | Read the result and inspect detailed copy diagnostics. |
| Settings in dark appearance | Choose copying options, local logs, and light or dark appearance. |

## Notes for certification draft

FolderFlow is a C# WPF desktop utility. It uses the RoboCopy executable already included with Windows, launched from the Windows system directory, to copy or mirror existing user-selected local folders. No Windows binaries are redistributed. The app runs as the signed-in user without elevation, has no account login, and does not request arbitrary command-line options from the user.

The `runFullTrust` capability is required for the existing desktop executable, normal desktop file access, and managing the RoboCopy process. Copying is the core product function. No unrelated restricted capability is requested. [Microsoft capability guidance](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/app-capability-declarations)

To test:

1. Create two disposable local subfolders; put several sample files in the source and an unrelated file in the destination.
2. Select both folders. Choose Copy files and Preview changes. Confirm no source/destination contents changed.
3. Start Copy. Confirm the source files appear in the destination and its unrelated file remains.
4. Choose Mirror folders with the same disposable folders. Start and inspect the explicit deletion confirmation. Cancel once, then repeat and confirm only if deletion of the sample extra file is intended.
5. Open View Details after a run. Use Settings to change theme/log preferences.
6. For Stop, use a large disposable source file or enough files to keep a run active; select Stop and read the result. Existing completed operations are not rolled back.

The app intentionally rejects drive roots, network paths, nested/equivalent folders, protected destinations, and detected links. Use ordinary local subfolders rather than a drive root for certification tests. No special USB hardware is required to test basic functionality.

## Owner review before copying this into Partner Center

Confirm the product name and publisher, price, markets, privacy/support destinations, source licensing, and supported Windows minimum. Replace or remove any claim changed by final packaging. Complete the IARC questionnaire in the portal rather than copying a presumed age rating. [Microsoft age-rating process](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/age-ratings)
