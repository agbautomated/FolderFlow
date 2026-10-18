# Microsoft Store packaging

FolderFlow stays a WPF/.NET 10 desktop app. MSIX gives it a package identity, Store delivery and Store-managed updates; a rewrite in WinUI 3 is unnecessary. The package is self-contained, so users do not need to install .NET separately. The Store version targets Windows 11, x64 by default; an arm64 package can be built with a separate identity config using the same Store identity and version.

## Tools

- Windows with a .NET 10 SDK (`dotnet --info`).
- Windows SDK MSIX Packaging Tools (`MakeAppx.exe`), available through the Windows SDK or the official `Microsoft.Windows.SDK.BuildTools` NuGet package. Point `-MakeAppxPath` at its x64 tool if it is not on PATH.
- Network access for the first self-contained runtime restore, unless already cached.

## Validate packaging before you have a Store identity

From the repository folder:

```powershell
.\build-store.ps1 -MakeAppxPath 'C:\path\to\x64\MakeAppx.exe'
```

This creates an **unsigned development MSIX** with identity `FolderFlow.Development`, publisher `CN=FolderFlowDevelopment, OID.2.25.311729368913984317654407730594956997722=1`, and the display name `FolderFlow Development`. The OID marks an isolated unsigned-test identity, and the script forbids it in Store builds. It cannot be submitted to the Store. Each run has a separate `packaging\work\<timestamp-id>` staging folder and creates a package plus SHA-256 hash. It does not overwrite the portable build, install packages, create or trust certificates, or change Developer Mode.

An unsigned MSIX cannot be installed by double-clicking it. On a disposable Windows 11 VM, this development package can be installed with `Add-AppxPackage -Path '<package path>' -AllowUnsigned` from an elevated PowerShell. Because the package contains executable code, this operation affects all users. Microsoft documents this facility for testing only. The script does not perform installation. `MakeAppx` success checks package structure and manifest validity; it does not prove Store certification or packaged runtime behavior. See [Microsoft's unsigned test package guide](https://learn.microsoft.com/en-us/windows/msix/package/unsigned-package).

## Build the Store candidate

1. Reserve the app name in Microsoft Partner Center.
2. Open the product's **Product identity** page. Copy the exact **Package/Identity/Name**, **Package/Identity/Publisher**, and **Package/Properties/PublisherDisplayName**. Do not substitute your GitHub username, email address, company name, or a guessed certificate subject.
3. Copy `identity.config.example.json` to `identity.config.json` in this folder and replace the example values. The real config is excluded from Git. Use the app's reserved display name and a four-part version such as `1.0.0.0`. For subsequent updates, increase the version while keeping the final component `0`. Keep the same identity and publisher for updates.
4. Build:

```powershell
.\build-store.ps1 -Store `
  -IdentityConfig .\packaging\identity.config.json `
  -MakeAppxPath 'C:\path\to\x64\MakeAppx.exe' `
  -OutputDirectory .\packaging\work\submission
```

Use `-DotNetPath 'C:\path\to\dotnet.exe'` if the SDK is not on PATH. The JSON schema (`identity.schema.json`) helps editors validate values. The script independently checks required fields, placeholders, package-name syntax, publisher syntax, version ranges and target architecture. It cannot authenticate the values with Partner Center: verify they exactly match the reserved product before uploading.

The manifest is rendered from an XML template with XML escaping. `MakeAppx` performs normal manifest validation; validation is never suppressed. Runtime licenses and notices are copied from the exact restored .NET runtime packs. Package PNG assets are included in source and can be regenerated with `packaging\generate-assets.ps1`.

Upload the unsigned Store candidate to the matching product's **Packages** page after testing and review. Microsoft signs packages accepted for Store distribution. You do not need to create a self-signed certificate or trust a developer certificate on a customer's PC. An MSIX build is preparation for submission, not a published Store listing.

## Full-trust declaration and certification

The manifest uses `Windows.Desktop`, `Windows.FullTrustApplication`, and only the restricted `runFullTrust` capability. That capability is required because the WPF app launches Windows RoboCopy and accesses folders selected by the user. No `broadFileSystemAccess`, elevation, startup task, background service, file association, or automatic update service is requested. Explain the RoboCopy child process and folder access in certification notes, and answer any restricted-capability questions honestly.

Suggested certification note:

> FolderFlow is a WPF desktop utility that copies or mirrors folders selected by the user. It invokes the RoboCopy executable supplied by Windows, without administrator elevation. It needs runFullTrust to launch that child process and access the selected source and destination folders. It does not download executable code, run at startup, or run a background service. Mirror operations require an explicit confirmation because they can delete destination items.

Before submission, run the Windows App Certification Kit, install and test an appropriately signed package on a clean Windows 11 VM, and exercise preview, copy, stop, mirror confirmation, log export, settings persistence, upgrade, and uninstall. Confirm that USB HDD-to-SSD copying works when both devices are attached. Keep incomplete copies recoverable after unplugging a device; no automated test substitutes for a hardware test.

Package minimum OS versions are technical manifest constraints. They are not a promise of lifecycle support for every Windows release. This Store configuration starts at Windows 11 build 22000; recommend a currently supported Windows 11 release to customers. Rebuild with supported .NET 10 servicing updates before each release, because this self-contained package owns its included runtime updates.

Official references: [manual MSIX packaging](https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-manual-conversion), [MakeAppx](https://learn.microsoft.com/en-us/windows/msix/package/create-app-package-with-makeappx-tool), [Store package requirements](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/app-package-requirements), [packaged desktop behavior](https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-behind-the-scenes), and [.NET Windows support](https://learn.microsoft.com/en-us/dotnet/core/install/windows).
