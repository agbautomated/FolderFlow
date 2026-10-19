# Store preparation validation

Checked on 19 October 2026 using Windows 11 build 26200, .NET SDK 10.0.402, .NET runtime 10.0.13, and Microsoft.Windows.SDK.BuildTools 10.0.28000.2705.

## Completed

- Release WPF compilation passed. The prepared self-contained x64 executable launched on Windows. The new package-data helper and About dialog compiled successfully. The later publisher-branding build also passed Release publishing; executable metadata reports TECHDECKLABS LLP and the About screen visibly shows TechDeckLabs and TECHDECKLABS LLP.
- All **11 real RoboCopy integration groups passed**, with 0 failed and 0 skipped. This includes copying, mirroring, preview, cancellation, file failures, Unicode paths, counts, log handling, same/nested/root path guards, destination junction protection, and the new shared data-directory behavior. Fixtures stayed in a disposable workspace directory.
- A native read-only check verified that asking Windows for LocalAppData with `KF_FLAG_NO_PACKAGE_REDIRECTION` returns the physical user data root, avoiding a doubled package path when Windows redirects the ordinary managed known-folder result.
- The final development MSIX passed normal MakeAppx validation; validation was not suppressed. Package identity: `FolderFlow.Development`, x64, version `1.0.0.0`, Windows 11 minimum build 22000.
- Package contents were inspected: runtime licenses/notices are included, the app requests only `runFullTrust`, and no copy of Microsoft's RoboCopy executable is redistributed.
- Store builds reject a missing identity config, placeholder identities, and the special unsigned-development publisher OID before publishing.

Seven current-build Store screenshots were captured and visually reviewed at 2560 × 1392, using disposable sample folders. Actual UI transfers copied 24 files and 5,000 files with zero failures; the first preserved a destination-only fixture. See [screenshot evidence and captions](STORE-SCREENSHOTS.md). The recorded development MSIX predates the later publisher-branding change; rebuild the production candidate with the final Partner Center identity before certification.

The final development-package SHA-256 is `54455D627D4835A599FFA1D693FC691E51FF7FEA7510D2F9B1D7017DF2C45EFC`. This is a development identity and is not a Store submission artifact.

## Still required for release

- Complete the Company's publisher verification, reserve the product name, and copy the exact Partner Center identity into a local config. Build the Store candidate with that identity.
- Install and run the final package on a clean Windows 11 test system, ideally through a private Store flight once available. Exercise the physical package-owned settings/log directory, RoboCopy child output, external log opening, normal user folder access, upgrades, and uninstall. Portable launch and MakeAppx validation do not replace this check.
- Run the Windows App Certification Kit and retain its report. WACK was not available in this environment and has not been run.
- Repeat a connected USB HDD-to-SSD transfer, interrupted-device recovery, read-only/full-destination behavior, and representative display scaling and keyboard checks on release hardware.
- Compare the prepared screenshots against the final installed Store candidate and recapture changed screens. Verify privacy and support URLs, apply the approved US$4.99 / ₹299 India launch targets and review regional prices, choose launch markets, complete ratings and tax/payout setup, and review certification notes.
- Restore and check current package/runtime vulnerability information before release. During this preparation NuGet reported `NU1900` because its vulnerability metadata endpoint was unavailable; this is not a completed security scan.
- Review the then-effective Microsoft Store policies and obtain Microsoft certification. No Store submission or paid public release has been made.

No development certificate was trusted, no unsigned package was installed, and no Windows security settings or Developer Mode were changed during these checks.
