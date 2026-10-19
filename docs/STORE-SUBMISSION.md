# Microsoft Store setup for FolderFlow

Prepared on 19 October 2026. This is a setup guide for the current WPF/.NET 10 application, not a completed Store submission. Account, identity, pricing, and certification fields remain owner-controlled.

## Recommended first release

Keep the existing WPF application and package its self-contained Windows x64 build as **MSIX**. Sell it as a **one-time paid download through Microsoft Store**, with **No free trial** initially. This avoids adding a payment backend, user accounts, subscriptions, or an untested trial gate. The Store provides checkout, binary hosting, signing, installation, and package updates. The MSI/EXE listing route has a different commerce and update model and is not the route prepared here. [Microsoft Store distribution options](https://learn.microsoft.com/en-us/windows/apps/publish/faq/get-started-with-the-microsoft-store)

MSIX packaging does not require rewriting WPF as WinUI. Microsoft documents a packaging-project route for WPF and WinForms, alongside other packaging options. [Publish your first Windows app](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/publish-first-app)

The existing public GitHub project can remain public while a Store build is paid. Decide the repository's source license and paid-product support terms before release; this preparation does not invent either. Do not describe the source as open source unless an appropriate license has actually been chosen and added.

## 1. Create or complete the developer account

Open [Microsoft Store developer registration](https://storedeveloper.microsoft.com/) and use the Microsoft account that should own the product. A GitHub login does not establish a Partner Center publisher identity.

- Microsoft's current guidance assigns **Individual** accounts to hobby/non-commercial distribution, and **Company** accounts to distribution connected with a business, trade, or profession, including independent developers and freelancers. For this commercial paid launch, use the eligible business route rather than automatically choosing Individual. Use genuine identity and contact information; if your personal/business situation does not fit the portal's requirements, resolve eligibility with Microsoft support before registering. An existing Individual account cannot simply be converted to Company. [Current account-type guidance](https://learn.microsoft.com/en-us/windows/apps/publish/partner-center/open-a-developer-account?tabs=individual)
- Individual onboarding is free in the new worldwide flow and uses government ID/selfie verification. [Individual onboarding](https://learn.microsoft.com/en-us/windows/apps/publish/whats-new-individual-developer)
- Company onboarding is also free in Microsoft's new flow, announced 7 May 2026; the old $99 company registration fee was removed. Company verification still applies. [Company onboarding announcement](https://blogs.windows.com/windowsdeveloper/2026/05/07/publish-to-microsoft-store-as-a-company-now-with-free-registration-and-faster-onboarding/)
- The owner completes sign-in, identity checks, agreement acceptance, and any bank/tax forms directly in Microsoft's portal.

Do not use the old $19/$99 fees as the budget for this setup. Account eligibility and verification status are determined by the portal.

## 2. Reserve the name and obtain the exact package identity

In Partner Center's **Apps and games**, create a new app, choose the MSIX submission path, and try to reserve **FolderFlow**. Name availability has not been established. If unavailable, choose a new product name before creating final artwork and packages.

Open the product's **Product management → Product identity** page and copy these values exactly, including punctuation and case:

| Value | Needed for |
| --- | --- |
| `Package/Identity/Name` | Stable package identity name |
| `Package/Identity/Publisher` | Exact publisher distinguished name, usually beginning `CN=` |
| `Package/Properties/PublisherDisplayName` | Publisher name displayed in the package |
| Store ID | Product links and later Store API integration |

These are public product identifiers, not passwords. Do not substitute `agbautomated` or a guessed `CN=` value. [View app identity details](https://learn.microsoft.com/en-us/windows/apps/publish/view-app-identity-details)

## 3. Enable sales and choose commercial settings

In **Account settings → Payout and tax**, create/complete the tax and payout profiles and assign them to the relevant developer program/seller ID. Use the account's real country of residence/business; an India-based owner should select India where the portal requests that country. Complete the forms selected by the portal and wait for validation. Bank beneficiary details must match the bank account. Do not put tax IDs, bank details, identity documents, or account credentials in this repository or chat. [Payout and tax setup](https://learn.microsoft.com/en-us/partner-center/account-settings/set-up-your-payout-account)

For a paid app, open the submission's **Pricing and availability**, choose an available base price, review the generated regional prices, select markets deliberately, and leave **No free trial** selected for this version. The approved launch targets are US$4.99 as the base price and ₹299 for India, paid once with no trial. Choose the corresponding allowed tier/range in the portal and review every regional price. Launch countries remain an owner decision. See [the pricing decision](PRICING.md). [MSIX pricing and availability](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/price-and-availability?pivots=store-installer-msix)

Microsoft currently documents a **15% fee for non-game apps using Microsoft's commerce platform**. This is not a promise that 85% of the customer-facing price reaches your bank: taxes, refunds, withholding, currency handling, and the agreement affect proceeds. [Store revenue model](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/why-distribute-through-store)

If a trial is added later, implement and test `Windows.Services.Store.StoreContext` licensing with the correct package identity and the WPF owner window handle. A setting or local date check is not a Store trial system. Microsoft requires a Store-associated acquisition for realistic Store license testing. This build does not implement trial enforcement; do not enable trials until that work is complete. [In-app purchases and trials](https://learn.microsoft.com/en-us/windows/uwp/monetize/in-app-purchases-and-trials)

## 4. Prepare the package and validate it

Use the packaging files and instructions included with the Store preparation change. Build an x64 MSIX using the exact identity from step 2. Keep local test identity packages separate from the final Store identity.

For Store submission, a public CA code-signing certificate purchase is unnecessary: Microsoft signs MSIX/AppX packages after certification. A locally sideloaded test package needs its own valid trusted signature; a development certificate is for controlled testing, not a customer distribution promise. Run the **Windows App Certification Kit (WACK)** before uploading and retain the report. Final manifest values must match Partner Center exactly. [App package requirements](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/app-package-requirements)

This is a full-trust desktop utility: it needs normal desktop access to user-selected folders and to launch Windows' installed RoboCopy. Declare `runFullTrust`, explain the use under **Submission options**, and request no unrelated restricted capabilities. Full trust is not administrator elevation. Microsoft reviews restricted capabilities during submission. Use the proposed explanation in [the listing draft](STORE-LISTING-DRAFT.md). [Capability declaration and approval](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/app-capability-declarations)

Before certification, test the **installed package**, not only the portable executable:

- First launch as a standard user; source/destination pickers; Copy; Preview; Mirror confirmation; Stop; Details; preferences; both themes.
- Unicode and space-containing paths, locked files, full/read-only destination, permission errors, disconnected USB drive, repeated copies, and interrupted-copy recovery.
- Destination preservation in Copy mode and intentional deletions only after Mirror confirmation, using disposable fixtures.
- App update over an earlier package with the same identity; settings/log behavior; uninstall; no effect on copied user data.
- Supported Windows versions, 100–200% display scaling, keyboard navigation, readable focus, and an accessible minimum window size.

The existing integration checks are useful evidence, but they do not establish USB hardware performance or packaged installation certification.

## 5. Complete the listing

Start with [STORE-LISTING-DRAFT.md](STORE-LISTING-DRAFT.md), then review it against the exact package that will be submitted.

- Choose the appropriate PC utility category, initially English only, and supported desktop hardware/OS values matching the package. The first packaging target is Windows 11 x64, minimum build 22000; claim only versions actually tested before release.
- Complete the **IARC age-rating questionnaire** truthfully. The generated rating is not a manually selected marketing age. [MSIX age ratings](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/age-ratings)
- Supply a public privacy policy: Win32/Desktop Bridge products require one even when copying remains local. It must explain local file/path access, settings, logs, and any voluntary support contact. [Microsoft Store policy 7.19, section 10.5.1](https://learn.microsoft.com/en-us/windows/apps/publish/store-policy-archive/store-policy-7-19#105-personal-information)
- Provide a real, monitored support page or support address. For a PC-only app this is recommended rather than universally mandatory; offering a paid product without a support route is poor launch preparation. Microsoft does not provide customer support for your app. [Privacy and support fields](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/support-info)
- Capture actual product screenshots with disposable sample filenames. For PC, upload PNGs at least **1366 × 768**, at most **50 MB** each; use 5–8 varied screenshots if available. A **300 × 300** app tile icon is recommended, in addition to package imagery. [Store screenshots and artwork](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/screenshots-and-images)

Do not show private folders, personal filenames, credentials, or bank/tax information in screenshots. Do not promise pause, move, guaranteed speed, automatic HDD/SSD detection, or an overall percentage: the current application has Copy/Mirror, Stop, conservative settings, and an activity indicator.

## 6. Submit with a deliberate publication hold

Upload the matching MSIX, finish the listing, ratings, commercial fields, and certification notes, and resolve all portal validation messages. Select a publishing hold/manual release option if offered so approval can be checked before the paid listing goes live. Only the owner authorizes final submission and release. Certification is a separate Microsoft decision; local packaging success does not imply Store approval. [Create an MSIX submission](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/create-app-submission?pivots=store-installer-msix)

As of 19 October 2026, the policy page has already published **version 7.20, effective 22 October 2026**. Recheck the effective policy at actual submission, including accurate representation and pricing proportionate to delivered functionality. [Published Store policies](https://learn.microsoft.com/en-us/windows/apps/publish/store-policies)

## Remaining owner inputs

| Field | Status |
| --- | --- |
| Microsoft developer account, account type, country and verification | Owner completes in portal |
| Reserved product name | Not yet confirmed |
| Exact identity name, publisher and publisher display name | Copy from Partner Center |
| Store ID | Assigned by Partner Center |
| One-time base price, regional prices and launch markets | US$4.99 base and ₹299 India approved for planning; portal entry/regional review and launch markets remain |
| Tax/payout validation | Owner completes in portal |
| Public privacy/support URLs or contact | Must be verified before submission |
| Source license and commercial support terms | Owner decision |
| Packaged WACK/device validation and final screenshots | Release checks |
| Submission and public release | Owner authorization after review |
