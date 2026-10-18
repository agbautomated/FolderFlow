# Validation notes

Validated on **19 October 2026** on Windows 11, build 26200, using .NET SDK **10.0.402** and runtime **10.0.13**. Transfers use the Windows system RoboCopy executable; no simulated copying is involved.

## Core integration checks

The final integration run completed with **10 passed, 0 failed, 0 skipped**. The checks use disposable source/destination folders inside this chat's workspace.

| Check group | Verified result |
| --- | --- |
| Ordinary copying | New files and empty folders copied; space-containing and Unicode paths preserved; unrelated destination files retained. Actual totals, copied, skipped, and failed counts matched expectations. |
| Changed files | Source content replaced the destination version. |
| Mirroring | Destination extras and their subfolders were deleted; source content remained intact. |
| Preview | Copy and mirror previews left both folder trees unchanged, verified with file hashes and directory snapshots. |
| Real copy failure | A source file locked by another process produced RoboCopy failure code 8 and a failed-file count. |
| Stopping | Cancellation stopped a running RoboCopy retry operation promptly and returned a stopped result. |
| Folder validation | Missing folders, files selected as folders, identical/nested paths, drive roots, network shares, and drive-relative paths were refused. |
| Settings and result handling | Conservative worker/retry arguments, large-file mode, one-worker mode, preview arguments, and exit codes 0–16 were checked. |
| Logs | Saved native Unicode logs retained Unicode filenames. Unsaved private transcripts were removed. An unavailable transcript directory stopped execution with a friendly error. |
| Linked-folder safety | Selected junctions and junction ancestors were refused. Copy and mirror both refused a destination junction that could otherwise redirect a write outside the selected destination; the outside sentinel stayed unchanged. |

The final retained fixtures are in `work/test-fixtures/folderflow-7c4bcc2849b946e0b49a2ab351ca4edb` relative to the chat workspace, outside the distributable source folder. The final run included the last logging correction and exercised junctions successfully with the necessary local test permissions.

Two Windows behaviors were reproduced and addressed during validation:

- Redirected `/UNICODE` console output mixed encodings on this Windows build. Native UTF-16 `/UNILOG` files preserved Unicode and reliable counts, and are now the authoritative source for progress/results.
- RoboCopy's junction exclusion arguments alone did not prevent a write through a matching destination junction. Destination preflight now refuses linked entries in both modes.

Preference storage was separately checked with **11 assertions passing**, covering persistence, normalization, fallback behavior, and isolated development data storage.

To repeat the core checks from the extracted source folder with the .NET 10 SDK installed:

```powershell
dotnet run --project .\tests\FolderFlow.Tests\FolderFlow.Tests.csproj -c Release -- --fixtures .\work\test-fixtures --keep-fixtures
```

The fixture parent is explicit, and each run uses a unique subfolder. Omit `--keep-fixtures` to remove that run's test files after validation. A restricted execution environment may refuse junction creation; the harness reports that check as skipped in such an environment. The final run above exercised junctions successfully.

## Live Windows application checks

The self-contained x64 executable was built and launched on Windows without a separately installed .NET runtime. The published app was exercised through its real interface:

- Both native folder pickers opened and could be cancelled. Full folder paths were entered in the main screen for the copy tests.
- Start Copy copied two files, displayed 2 copied / 0 skipped / 0 failed, and offered Done and View Details. Unicode content matched by SHA-256.
- View Details showed the actual RoboCopy transcript and saved log location. Done returned to folder selection.
- Preview reported one pending file and left the destination unchanged.
- Stop interrupted a live transfer while a fixture file was locked and explained that files might already have been copied. Rerunning completed the remaining file, showing 1 copied / 3 skipped / 0 failed. All four source/destination file hashes matched afterward.
- Mirror opened a separate warning showing both paths and an initially disabled confirmation button. Go back returned without running a mirror; real mirror deletion was covered by the core tests above.
- Appearance and retry choices were changed through Settings and saved. Saved paths and appearance survived restarting the executable. Light and dark layouts were inspected, including primary-button contrast.
- At the minimum 640 × 620 window size, the content scrolled and the copy controls remained reachable.

All UI transfers and saved test preferences used isolated folders under `work/`, away from the user's drives and normal application preferences.

## Scope

These checks verify behavior on local Windows fixture folders. They are not a performance benchmark of the user's physical USB HDD/SSD, a physical USB-disconnection test, a power-loss recovery test, or validation across every Windows display language. Live overall percentages remain intentionally unavailable because RoboCopy does not provide a reliable whole-folder percentage.
