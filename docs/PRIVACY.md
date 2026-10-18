# FolderFlow privacy policy

Effective date: 19 October 2026.

FolderFlow is a Windows desktop application maintained in the [FolderFlow project](https://github.com/agbautomated/FolderFlow). This policy describes the application in this repository, including its planned Microsoft Store distribution.

## Your files stay on your computer

FolderFlow uses the RoboCopy executable included with Windows to copy or mirror the local folders you select. It reads and writes file contents, names, attributes, and timestamps to perform that operation. Mirror mode can delete destination items after you confirm the operation.

FolderFlow does not upload your files, send file contents or file names to its maintainers, run analytics, display advertising, or include a telemetry service. It does not require an in-app account and does not provide cloud storage or synchronization.

## Settings and diagnostic information

The application saves your selected folder paths and preferences on your computer. When detailed logs are enabled, it also saves a local RoboCopy transcript. Logs can contain full file paths, file sizes, counts, timestamps, and Windows error messages. A path can include your Windows user name or other personal information.

Even with saved logs disabled, a private temporary transcript is used during a transfer to display progress and results accurately. The application attempts to remove that temporary transcript when the operation finishes. An interruption, crash, or file-permission problem may leave temporary files behind.

For an unpackaged copy of FolderFlow, the default data directory is `%LOCALAPPDATA%\FolderFlow`. For the packaged application, it is the package's `LocalState\FolderFlow` directory under `%LOCALAPPDATA%\Packages`. The About window shows the exact directory used by your installation. Development runs may use a different directory when explicitly configured.

You control whether detailed logs are saved through Settings. FolderFlow does not automatically upload logs or crash reports. You can remove saved logs when they are no longer needed; close the application first so you do not remove an active transcript.

## Microsoft Store and external links

If you obtain FolderFlow through Microsoft Store, Microsoft handles the Store account, purchase, payment, licensing, download, and update services under [Microsoft's privacy statement](https://privacy.microsoft.com/privacystatement). FolderFlow's maintainers do not receive your payment-card details through the application.

The application opens a web browser only when you choose a support or privacy link. GitHub and your browser may process information under their own policies when you visit those pages. See [GitHub's privacy statement](https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement).

## Support and privacy questions

Contact the project through [FolderFlow support](https://github.com/agbautomated/FolderFlow/issues). GitHub issues are public. Do not post file contents, complete logs, full personal paths, account details, or other sensitive information. If a question requires private information, ask the maintainer for an appropriate private contact method before sharing it.

## Changes

This policy will be updated if the application's data practices change. The effective date above identifies this version.
