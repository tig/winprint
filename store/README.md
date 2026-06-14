# Store submission assets & workflow

Everything needed to publish **WinPrint** (paid) to the **Mac App Store** and the
**Microsoft Store**, managed as code. `brew` / `winget` / GitHub Releases remain the
free testing channels; the stores are the monetized channels.

> Guiding principle: **in-app/in-package assets are generated from source; marketing
> assets are metadata-as-code.** App icons & splash come from
> [`src/WinPrint.Maui/Resources/AppIcon`](../src/WinPrint.Maui/Resources/AppIcon/README.md)
> (single SVG → every size at build). This folder holds only the **store listing**
> assets (text + screenshots).

## Layout

```
store/
  capture-screenshots.sh        # regenerate store-sized screenshots (headless TUI)
  .gitignore                    # generated screenshots are NOT committed
  macos/
    fastlane/                   # fastlane `deliver` — App Store Connect listing as code
      Appfile Fastfile Deliverfile
      metadata/ … en-US/*.txt   # name, subtitle, description, keywords, urls, …
    screenshots/                # 2880×1800 (generated)
  windows/
    StoreBroker/                # Microsoft Store listing as code
      config.json  PDP/en-us/PDP.xml
    screenshots/                # 2560×1440 (generated)
```

## Screenshots

```bash
TUIREC=/path/to/tuirec ./capture-screenshots.sh
```
Produces exact store canvas sizes (Mac 2880×1800, Windows 2560×1440) from the headless
`wp` TUI. They're **git-ignored** — regenerate before each submission.

⚠️ The script currently captures the **TUI** (headless, reproducible). For listings that
should show the **desktop GUI**, add captures of the MacCatalyst app
(`dotnet build -t:Run -f net10.0-maccatalyst`) and the Windows MAUI app (the
`run-maui-app` skill), cropped to the sizes above. Apple wants 3–10 per size; Microsoft
recommends 5–8 (≥1 required, min 1366×768).

## 🍎 Mac App Store

**One-time setup (human):**
1. Apple Developer Program membership.
2. In App Store Connect, create the app record with bundle id **`com.kindel.winprint`**.
3. Create an App Store Connect **API key** (.p8) → set `ASC_KEY_ID`, `ASC_ISSUER_ID`,
   `ASC_KEY_CONTENT` (base64 of the .p8).

**Build gates (separate from this listing — these are the real work):**
- **App Sandbox** entitlement is **required** for the Mac App Store.
- Code-sign + **notarize**; use Apple Distribution / Mac App Store provisioning.
- The Mac App Store build must **not** ship a self-updater (Velopack) — the Store updates it.
- Set the app category to Developer Tools (note: the MacCatalyst `Info.plist`
  `LSApplicationCategoryType` is currently `public.app-category.lifestyle` — change it to
  `public.app-category.developer-tools` before submitting).

**Listing (this folder):**
```bash
cd store/macos
bundle exec fastlane mac verify     # validate metadata, no upload
bundle exec fastlane mac metadata   # push text + screenshots
PKG_PATH=/path/to/WinPrint.pkg bundle exec fastlane mac upload   # upload a built build
```
Pricing/availability are set in **App Store Connect** (Apple takes 15% under the Small
Business Program, otherwise 30%).

## 🪟 Microsoft Store

**One-time setup (human):**
1. Partner Center developer account; reserve the name **WinPrint**.
2. Put the app's **Store ID** into `windows/StoreBroker/config.json`.

**Packaging — choose one:**
- **MSIX** (Store-signed): MAUI's resizetizer already generates the in-package tile/icon
  assets from `MauiIcon`. Point `AppxPath` at the `.msixupload`.
- **Unpackaged MSI/EXE**: ship the existing Velopack installer via the Store's MSI/EXE
  support; manage the installer in Partner Center and use this folder for listing only.

**Listing (this folder):** edit `PDP/en-us/PDP.xml`, then submit via **StoreBroker**
(`Update-ApplicationSubmission`), the **Microsoft Store submission API**, or by uploading
manually in Partner Center. Pricing is set in **Partner Center**.

## Things only a human can do (checklist)

- [ ] Developer accounts (Apple Developer Program; Partner Center)
- [ ] Signing identities / certs; macOS notarization; App Store provisioning
- [ ] Reserve the app name; create the app records (bundle `com.kindel.winprint` / Store ID)
- [ ] **Pricing & availability** (paid tier) in each dashboard
- [ ] **Age rating** questionnaires (Apple; IARC for Microsoft)
- [ ] Apple **App Privacy** "nutrition label" — declare the optional Application
      Insights diagnostics (off by default); file contents are not collected.
      See [`docs/privacy.md`](../docs/privacy.md) (linked as the privacy-policy URL).
- [ ] Upload the **GitHub social-preview** image (`docs/winprint-social.png`) —
      Settings → Social preview (no API for this).
- [ ] Generate & review **screenshots** (`./capture-screenshots.sh`, plus GUI captures)

## Monetization notes

- **Model:** simplest is paid-up-front; both stores also support free + in-app purchase.
- **Fees:** Apple 15%/30%; Microsoft's app fee. Configure price tiers in each dashboard.
- Keep `brew`/`winget`/GitHub Releases as free testing channels; gate the store builds in
  CI so the MAS build excludes the self-updater and enables the sandbox.
