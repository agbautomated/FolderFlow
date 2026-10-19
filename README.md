# FolderFlow

By **TechDeckLabs** (**TECHDECKLABS LLP**).

A small Windows desktop application for copying folders between connected drives. Choose **Copy from**, choose **Copy to**, and select **Start Copy**. FolderFlow runs the real RoboCopy included with Windows.

## Run

This repository contains the application source, tests, and build scripts. Build the app using the steps below, or, if you already have `FolderFlow-Windows-x64.zip`, extract it and open `FolderFlow.exe` inside the extracted folder. Alternatively, open `Launch FolderFlow.cmd` alongside the source to start an existing portable build. Keep the complete portable folder together. The portable app does not need administrator rights or a separate .NET installation. The executable is unsigned, so Windows may identify it as an unfamiliar application.

To build from source, install the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) on Windows, open PowerShell in this folder, and run:

```powershell
dotnet build .\src\FolderFlow.App\FolderFlow.App.csproj -c Release
dotnet run --project .\src\FolderFlow.App\FolderFlow.App.csproj -c Release
```

The included `build.ps1`, `test.ps1`, and `publish.ps1` provide build, integration checks, and portable publish workflows. Open `FolderFlow.slnx` in a current Visual Studio release to work with all three projects.

To produce a portable Windows x64 build:

```powershell
dotnet publish .\src\FolderFlow.App\FolderFlow.App.csproj -c Release -r win-x64 --self-contained true -o .\portable
```

Keep all files in `portable` together. The source has no third-party package dependencies. Use Windows 10 or Windows 11 with a supported .NET 10 desktop environment. ARM64 builds can use `-r win-arm64` instead; the included portable build and validation target x64.

## Everyday use

1. Connect both drives and create a destination folder on the SSD if needed.
2. Choose the HDD folder under **Copy from** and the SSD folder under **Copy to**.
3. Leave **Copy files** selected for ordinary copying, then choose **Start Copy**.
4. Read the completion summary. Use **View Details** for a log or a specific problem.

Folder contents are copied directly into the selected destination. For example, choosing `E:\Photos` and `F:\Backup` copies `E:\Photos\Holiday.jpg` to `F:\Backup\Holiday.jpg`.

**Copy files** adds new files and replaces changed destination versions. It leaves unrelated destination files in place. It is not a versioned backup: an existing file at the same relative path can be overwritten.

**Mirror folders** also removes destination files and folders that are absent from the source. Before a real mirror run, the app displays both paths, explains the deletion, and requires explicit confirmation. Use a destination dedicated to this source. Choosing an empty source can empty the destination.

Use **Preview changes** beside Start Copy to see what would happen without copying or removing files. A preview reports candidate actions; its “files to copy” counts are not completed copies. It describes the folders at preview time, so later changes can alter the next real run.

**Stop** ends the active RoboCopy process. Some files may already have copied, and a current file may be incomplete. Run the same copy again to finish remaining work. A stopped mirror may already have removed destination extras; stopping does not undo completed operations.

## Settings and USB performance

The default settings are intended as a conservative starting point for a USB HDD reading into a USB SSD:

- Two simultaneous copy workers. This is a design choice to limit competing HDD reads, not a guarantee of maximum speed. Turning off simultaneous copying uses a single worker; try that if the HDD seeks heavily or becomes less responsive.
- Restartable copying for mixed folders, allowing RoboCopy to continue interrupted files on a later run.
- An optional large-file setting uses unbuffered copying instead. It may help when moving large videos or disk images; it does not promise a speed increase for small files.
- Two retries with a two-second wait. “No retries” and a more persistent choice are available for different needs.
- Save a log by default. Turn it off when a persistent diagnostic file is unnecessary.
- System light/dark appearance by default, with explicit Light and Dark choices.

Microsoft documents multithreading and unbuffered copying as RoboCopy features. The best worker count depends on the drives, USB bridge, shared USB connection, and files. Restartable copying adds overhead; Microsoft's migration guidance recommends choosing it with that tradeoff in mind. [RoboCopy reference](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/robocopy/), [Microsoft migration guidance](https://learn.microsoft.com/en-us/azure/storage/files/storage-files-migration-robocopy).

FolderFlow does not infer HDD/SSD identity or USB bandwidth. The user selects the folders. It copies ordinary file data, attributes, and timestamps; it does not request backup privileges, ownership, auditing, or full NTFS permission replication. Windows filesystem limitations still apply, including large-file limits on FAT32 and differences in metadata support.

## Safety and progress

FolderFlow validates existing local folders before starting. It refuses identical or nested source/destination folders, including aliases it can resolve to the same physical location. It blocks source and destination drive roots, and destinations in Windows/application locations. It also refuses linked selected folders and linked ancestors. Before copying or mirroring, preflight checks the entire destination and refuses links or folders it cannot inspect safely. Source links are excluded from copying. Network shares and device-style paths are intentionally outside this version's scope.

Keep the selected folders stable during a run. Preflight validation is not a transaction or a security boundary against another program changing the directory tree while RoboCopy is running. Destination NTFS hard links are not detected; use an ordinary, dedicated destination folder.

The progress view shows elapsed time, current operation or file when available, and an indeterminate activity indicator. RoboCopy does not supply a reliable overall percentage for a changing, multithreaded folder tree, so the app does not invent one. Copied/skipped/failed counts are shown from a recognized final summary. Unsupported localized summaries leave counts unavailable while the raw details remain accessible. A process exit code determines completion independently of summary parsing.

Codes 0 through 7 do not indicate a copy failure; 8 or above indicates at least one failure. Mismatches can still require attention even when there is no failed-file bit. [Microsoft return-code documentation](https://learn.microsoft.com/en-us/troubleshoot/windows-server/backup-and-storage/return-codes-used-robocopy-utility).

## Architecture

**Recommendation: C# and WPF on .NET 10.** WPF gives this small Windows utility mature folder selection, responsive XAML layout, data binding, and application styling without a Windows App SDK packaging dependency. WinUI 3 would also work, but its extra deployment surface is unnecessary for this scope. WPF is Windows-only and part of .NET. [WPF overview](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/).

The code separates responsibilities into two projects:

| Component | Responsibility |
| --- | --- |
| `FolderFlow.App` | Single-window workflow, pickers, progress/result views, settings, themes, confirmations, and preference storage. |
| `FolderFlow.Core/CopyModels.cs` | Immutable job, settings, progress, and result contracts. |
| `FolderFlow.Core/PathValidator.cs` | Folder normalization, canonical location checks, protected destinations, and mirror preflight. |
| `FolderFlow.Core/RobocopyArguments.cs` | A small, fixed allowlist of arguments derived from validated settings. |
| `FolderFlow.Core/RobocopyRunner.cs` | Background process lifetime, cancellation, Unicode output, bounded diagnostics, logging, progress, and result interpretation. |
| `FolderFlow.Core/RobocopyOutputParser.cs` | Conservative extraction of current-file information and structurally recognized final counts. |
| `FolderFlow.Tests` | Dependency-free console integration checks against Windows RoboCopy and disposable fixture folders. |

RoboCopy is launched from the Windows system folder with `UseShellExecute = false` and separate `ProcessStartInfo.ArgumentList` entries. User folder names never become shell commands. The app neither elevates itself nor accepts arbitrary options.

Copy mode uses `/E`; mirror uses `/E /PURGE` to synchronize contents without `/MIR`'s additional root security behavior. Both use `/COPY:DAT`, `/DCOPY:DAT`, link exclusions, bounded retries, full paths, and numeric byte sizes. The default worker setting adds `/MT:2`; normal copying uses `/Z`, the large-file setting chooses `/J`, and preview adds `/L`. These implementation details stay out of the main workflow.

Preferences normally live in `%LOCALAPPDATA%\FolderFlow\preferences.json` for the portable app. The MSIX app uses its package-owned `LocalState\FolderFlow` directory; Settings → About shows the exact location. Saved logs go in its `Logs` subfolder. A native UTF-16 `/UNILOG` file is used for reliable Unicode progress and result parsing because redirected `/UNICODE` console output can mix encodings on Windows. With saved logs disabled, cleanup of the private transcript is attempted when the run ends; an interruption or permission problem can leave it behind. FolderFlow needs a writable data folder to create that transcript; it stops before copying if this cannot be prepared. Test or development runs can set `FOLDERFLOW_DATA_DIR` to keep settings and diagnostics in a disposable location. Logs can contain full file paths; the app does not upload them.

The intentionally small workflow offers Copy/Mirror and Stop. It does not expose source-deleting move operations or process suspension. Rerunning an interrupted copy is the recovery path.

## Verify

Run the console integration checks on Windows:

```powershell
dotnet run --project .\tests\FolderFlow.Tests\FolderFlow.Tests.csproj -c Release -- --fixtures .\work\test-fixtures
```

Add `--keep-fixtures` after the fixture path to retain the copied folders for inspection. Development in a restricted environment may need a writable process-local `APPDATA` and `DOTNET_CLI_HOME` so the SDK can read its configuration; this is separate from running the portable application.

The checks use actual RoboCopy operations for copying, changed files, unrelated destination preservation, mirroring/deletion, non-mutating previews, Unicode and space-containing paths, locked-file failures, stopping a running process, result counts, guarded paths, and destination junction safety. All destructive fixtures are beneath the explicit fixture parent. The harness removes only its unique test subfolder after checking its resolved location.

For release validation, also launch the application and verify both folder pickers, Start Copy, Stop, mirror confirmation, preview, Settings persistence, Done, View Details, light/dark themes, and the minimum window size. Hardware-specific throughput and USB disconnection behavior should be checked on the actual drives before a long transfer.

See `VALIDATION.md` for the checks completed for the supplied build.
