# Code signing runbooks

WinPrint's `release.yml` signs **Windows** installers with **Azure Trusted Signing** and the
**macOS** `.app` with an **Apple Developer ID** certificate + notarization. Each is independent
and gated on its own secret group, so a fork (or a repo missing one set) still builds — only the
matching signing step is skipped. Two runbooks below:

- [Windows code signing — Azure Trusted Signing](#windows-code-signing--azure-trusted-signing-runbook)
- [macOS code signing — Apple Developer ID + notarization](#macos-code-signing--apple-developer-id--notarization-runbook)

---

# Windows code signing — Azure Trusted Signing (runbook)

WinPrint's `release.yml` signs Windows installers with **Azure Trusted Signing**,
authenticating from GitHub Actions via **OIDC federation** — there is **no client secret**
and nothing sensitive to rotate. This doc is the single place that explains the whole
setup so any authorized operator (human or agent) can **recreate it in one shot**.

> Audience note for agents: the scriptable parts are idempotent — re-running is safe and
> converges. The only non-scriptable part is the one-time identity validation (step 1).
> Do not invent values; everything stable lives in `scripts/Azure.Config.ps1`.

## Files

| File | Role |
|------|------|
| `scripts/Azure.Config.ps1`   | Single source of truth — subscription, RG, account, profile, repo, branches. Edit here only. |
| `scripts/SetupAzure.ps1`     | Idempotent one-shot: creates app reg + SP + federated creds + role assignment; optionally pushes GitHub secrets. |
| `scripts/ValidateAzure.ps1`  | Read-only verification that the trust is correct. |
| `.github/workflows/release.yml` | Consumer — uses the six `AZURE_*` secrets, gated on `HAS_AZURE_SIGNING`. |

## What gets created

```
Subscription (Kindel LLC)
└─ Resource group: WinPrint_Resources
   └─ Trusted Signing account: winprint                 ← step 1, MANUAL (identity validation)
      └─ Certificate profile: WinPrint (PublicTrust)    ← step 1, MANUAL
Entra ID (tenant)
└─ App registration: winprint                           ← scripted
   ├─ Service principal                                 ← scripted
   ├─ Federated credentials                             ← scripted
   │    • gh-develop / gh-main : exact subject  repo:tig/winprint:ref:refs/heads/<branch>
   │    • gh-tags (FLEXIBLE)   : claimsMatchingExpression  matches refs/tags/*  (see gotchas)
   └─ Role: "Artifact Signing Certificate Profile Signer" @ cert-profile scope  ← scripted
GitHub repo: tig/winprint
└─ Secrets: AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_SUBSCRIPTION_ID,
            AZURE_SIGNING_ACCOUNT, AZURE_SIGNING_PROFILE, AZURE_SIGNING_ENDPOINT   ← scripted (-SetGitHubSecrets)
```

## Step 1 — the one manual prerequisite (cannot be fully scripted)

A **PublicTrust** certificate profile requires Microsoft to **validate the publisher's
identity** before any public-trust cert can be issued. That request is submitted and
completed in the **Azure Portal** (Trusted Signing → your account → Identity validation),
and Microsoft reviews it out-of-band. Because it gates on a human/Microsoft review, it is
**not** in the scripts. Do this once:

1. Portal → create a **Trusted Signing account** (`winprint`) in `WinPrint_Resources`.
2. Submit an **Identity validation** request; wait for it to reach **Completed**.
3. Create a **certificate profile** (`WinPrint`, type **Public Trust**) bound to that
   validated identity.

Everything below is fully automated and re-runnable. (`SetupAzure.ps1` will refuse to
proceed with a clear error if the account/profile from this step don't exist yet.)

## Step 2 — recreate the OIDC trust in one shot

Prereqs: `pwsh` 7+, `az` (`brew install azure-cli`), and — to push secrets — `gh`.

```bash
az login                                   # an account with App Registration + RBAC rights
pwsh scripts/SetupAzure.ps1 -SetGitHubSecrets
pwsh scripts/ValidateAzure.ps1             # confirm
```

`SetupAzure.ps1` is **find-or-create** at every step, so it's the same command whether
you're building fresh or reconciling drift (e.g. after editing `Branches` in the config).
Omit `-SetGitHubSecrets` to print the values without touching the repo. Pass
`-CreateResourceGroup` if even the RG is gone.

## Step 3 — verify

`ValidateAzure.ps1` asserts the federated-credential subjects and the signer role
assignment, and prints the six secret values. A clean run ends with `Validation complete.`
A failed run throws on the first missing piece.

## How `release.yml` consumes it

- `HAS_AZURE_SIGNING` (env) is true only when all six secrets are present; signing steps
  are gated on it, so forks/unsigned builds still work.
- The job requests `permissions: id-token: write` and `azure/login@v2` with
  `client-id`/`tenant-id`/`subscription-id` — no secret, the OIDC token is exchanged for
  the federated credential.
- Releases trigger on `v*` **tags** → covered by the flexible tag credential. Manual
  `workflow_dispatch` runs are covered by the per-branch credentials (`develop`, `main`).

## Gotchas / lessons baked into the scripts

- **Entra federated `subject` is EXACT-match — wildcards do not work.** A credential with
  subject `repo:tig/winprint:ref:refs/tags/*` never matches a real tag token
  (`…/refs/tags/v2.0.6`) and OIDC login fails with **AADSTS700213**. Tags therefore use a
  **flexible federated identity credential** (`claimsMatchingExpression`:
  `claims['sub'] matches 'repo:tig/winprint:ref:refs/tags/*'`), created via the **beta**
  Microsoft Graph endpoint (`/beta/applications/{id}/federatedIdentityCredentials`) — the
  `v1.0` endpoint and `az ad app federated-credential create` reject/omit it. Branches
  (`develop`, `main`) are fixed strings, so they keep plain exact-match subjects.
- **PowerShell `$var:` trap.** `"...$GhRepo:ref..."` makes PowerShell read `GhRepo:` as a
  scope qualifier and silently drops the repo name. Always brace: `${GhRepo}`. (This bit
  us once; both scripts now use the braced form.)
- **Role was renamed.** "Trusted Signing Certificate Profile Signer" →
  "Artifact Signing Certificate Profile Signer". `ValidateAzure.ps1` accepts both; the
  config uses the current name. If assignment fails with "role doesn't exist", list with
  `az role definition list --query "[?contains(roleName,'Signer')].roleName"`.
- **RBAC propagation.** A freshly created role assignment can take a few minutes before the
  first CI signing call succeeds.
- **No secrets to rotate.** Federation means there is no client secret. The only "rotation"
  is the cert profile's identity validation, which Trusted Signing renews on its own cadence.

## Teardown (if ever needed)

```bash
# Remove the CI trust (leaves the signing account/profile intact):
APPID=$(az ad app list --display-name winprint --query "[0].appId" -o tsv)
az ad app delete --id "$APPID"     # deletes app + SP + federated creds; role assignment is orphaned & GC'd
```

---

# macOS code signing — Apple Developer ID + notarization (runbook)

WinPrint's `release.yml` signs the MacCatalyst **`WinPrint.app`** with an **Apple Developer ID
Application** certificate, then **notarizes** and **staples** it, so the Homebrew **cask** ships a
Gatekeeper-accepted build (no "unidentified developer" / "damaged" warning, no `brew trust --cask`
needed). Unlike the Windows path (OIDC, no secret), Apple's toolchain needs the certificate's
private key and an app-specific password, so this path **does** use repo secrets.

> Audience note: the only non-scriptable parts are the Apple-portal/Xcode/Keychain GUI actions in
> Step 1, Step 2, and Step 4 (creating the cert, exporting the key, minting the app-specific
> password). Everything else is a command. Stable, non-secret values for this repo: Team ID
> `PXD393HTUZ`, signing identity `Developer ID Application: Kindel, LLC (PXD393HTUZ)`.

## Prerequisites

- A **paid Apple Developer Program** membership ($99/yr). A free Apple ID **cannot** create a
  Developer ID certificate or notarize.
- The **Account Holder** role — only it can create **Developer ID Application** certificates.
- Full **Xcode** installed (simplest cert path) — or Keychain Access + the developer portal for the
  manual CSR alternative.

## What gets created

```
Apple Developer account (Team PXD393HTUZ)
└─ Developer ID Application certificate + private key   ← step 1, Xcode/portal (GUI)
   └─ exported as a password-protected .p12             ← step 2, Keychain Access (GUI)
Apple ID (tig@kindel.com)
└─ App-specific password "winprint-notary"             ← step 4, appleid.apple.com (GUI)
GitHub repo: tig/winprint
└─ Secrets (gated as HAS_APPLE_SIGNING):
     APPLE_CERTIFICATE_BASE64       base64 of the .p12 (cert + private key)
     APPLE_CERTIFICATE_PASSWORD     the .p12 export password
     APPLE_SIGNING_IDENTITY         "Developer ID Application: Kindel, LLC (PXD393HTUZ)"
     APPLE_TEAM_ID                  PXD393HTUZ
     APPLE_ID                       tig@kindel.com
     APPLE_APP_SPECIFIC_PASSWORD    the "winprint-notary" password
```

## Step 1 — create the Developer ID Application certificate (GUI)

With full Xcode this also drops the private key into your login keychain automatically (no manual
CSR):

1. **Xcode ▸ Settings… (⌘,) ▸ Accounts** → **+** → **Apple ID** → sign in.
2. Select your **Team** → **Manage Certificates…**.
3. **+** → **Developer ID Application**. Close the sheet.

Confirm it landed (this also prints the exact identity string and Team ID):

```bash
security find-identity -v -p codesigning
# 1) <hash> "Developer ID Application: Kindel, LLC (PXD393HTUZ)"
```

> Manual alternative (no Xcode): Keychain Access ▸ *Certificate Assistant ▸ Request a Certificate
> from a CA* (save to disk) → developer.apple.com ▸ Certificates ▸ **+** ▸ **Developer ID
> Application** → upload the CSR → download and double-click the `.cer` to install it next to its key.

## Step 2 — export the cert + key to a .p12, base64 into the secret (GUI + CLI)

1. **Keychain Access** → **login** keychain → **My Certificates** → expand the cert (verify a private
   key is nested under it) → right-click the **certificate** → **Export…** → format **.p12** → save to
   e.g. `~/winprint-developer-id.p12` → set an **export password** (this is `APPLE_CERTIFICATE_PASSWORD`)
   → enter your Mac login password to release the key.
2. base64 it straight into the secret (the .p12 stays encrypted by the export password, so nothing
   sensitive is printed), then set the password yourself so it never leaves your machine:

```bash
base64 -i ~/winprint-developer-id.p12 | tr -d '\n' | gh secret set APPLE_CERTIFICATE_BASE64
gh secret set APPLE_CERTIFICATE_PASSWORD        # paste the export password at the prompt
rm ~/winprint-developer-id.p12                  # key is now in the secret; cert+key remain in your keychain
```

## Step 3 — Team ID + signing identity (CLI)

Both are read from the identity string in Step 1 — `Developer ID Application: <name> (<TEAM_ID>)`:

```bash
gh secret set APPLE_TEAM_ID         --body "PXD393HTUZ"
gh secret set APPLE_SIGNING_IDENTITY --body "Developer ID Application: Kindel, LLC (PXD393HTUZ)"
gh secret set APPLE_ID              --body "tig@kindel.com"
```

## Step 4 — app-specific password for notarization (GUI + CLI)

`notarytool` here authenticates with **Apple ID + Team ID + app-specific password** (not an App Store
Connect API key).

1. **appleid.apple.com** → **Sign-In and Security** → **App-Specific Passwords** → **+** → label
   `winprint-notary` → copy the `abcd-efgh-ijkl-mnop` value.
2. `gh secret set APPLE_APP_SPECIFIC_PASSWORD`  (paste at the prompt).

## Step 5 — verify

Local signing smoke test — exactly the `codesign` invocation `release.yml` runs (proves cert, key,
identity, entitlements, hardened runtime, and Apple's timestamp server all work):

```bash
T=/tmp/cs-test; cp /bin/echo "$T"
codesign --force --options runtime --timestamp \
  --entitlements packaging/macos/codesign.entitlements \
  --sign "Developer ID Application: Kindel, LLC (PXD393HTUZ)" "$T"
codesign --verify --deep --strict --verbose=2 "$T"     # → valid on disk; satisfies its Designated Requirement
codesign -dvvv "$T" 2>&1 | grep -E "Authority|TeamIdentifier|Timestamp|runtime"
rm -f "$T"
```

Notarization-credential check (authenticates without submitting anything; expect a — likely empty —
history list, not an auth error):

```bash
xcrun notarytool history --apple-id tig@kindel.com --team-id PXD393HTUZ --password <app-specific-password>
```

## How `release.yml` consumes it

- `HAS_APPLE_SIGNING` (env) is true only when **all six** `APPLE_*` secrets are present; the import and
  sign/notarize steps are gated on it. Without them the job still emits an **unsigned** `WinPrint.app.zip`
  (with a `::warning::`) so the rest of the release is exercised — that build is Gatekeeper-blocked on a
  real machine.
- **Import** step (`runner.os == 'macOS'`): decodes `APPLE_CERTIFICATE_BASE64` to a `.p12`, creates a
  throwaway keychain, imports the cert with `APPLE_CERTIFICATE_PASSWORD`, and stores a `winprint-notary`
  notarytool profile from `APPLE_ID` / `APPLE_TEAM_ID` / `APPLE_APP_SPECIFIC_PASSWORD`.
- **Sign + notarize** step: `codesign --force --deep --options runtime --timestamp` with
  `packaging/macos/codesign.entitlements` and `APPLE_SIGNING_IDENTITY`, then
  `notarytool submit … --wait` and `stapler staple`, then zips the `.app` into the cask artifact.
- The TUI `wp` is embedded at `WinPrint.app/Contents/Helpers/wp` **before** signing, so `--deep` +
  notarization cover it too (notarization rejects code under `Resources`).

## Gotchas / lessons

- **Hardened runtime is required for notarization**, and the .NET/Mono runtime JITs + maps executable
  memory, so `codesign.entitlements` must grant `allow-jit`, `allow-unsigned-executable-memory`, and
  `disable-library-validation` — otherwise the signed app is rejected/aborts at launch. App Sandbox is
  intentionally **off** (direct/cask distribution, and `wp` must read arbitrary CLI-passed files);
  `allow-dyld-environment-variables` is intentionally **not** set (.NET doesn't need it; it weakens the
  hardened runtime).
- **Notarization replaces the need for `brew trust --cask`.** The older cask "damaged/unidentified
  developer" caveat was the *unsigned* state; a notarized + stapled `.app` is accepted normally.
- **No App Store Connect API key needed** — the Apple-ID + app-specific-password path is what
  `notarytool store-credentials` uses here. The app-specific password doesn't expire unless revoked.
- **The Developer ID Application certificate is valid ~5 years.** Renewal = repeat Step 1–2 and update
  `APPLE_CERTIFICATE_BASE64` / `APPLE_CERTIFICATE_PASSWORD`; the identity string/Team ID are unchanged.
- **`security find-identity` showing 0 identities** after creating the cert usually means the private
  key isn't in this keychain (cert imported without its key) — re-export from the machine that generated
  the CSR, or recreate via the Xcode path which keeps cert+key together.

### CI signing-pipeline lessons (all baked into the `Sign, notarize, and zip` step)

Bringing this online took five rounds; each fix is in `release.yml` and a regression if removed:

1. **Register the temp signing keychain in the search list.** `codesign` resolves the `--sign` identity
   **name** against the keychain *search list*, not the `--keychain` path, so without
   `security list-keychains -d user -s "$KEYCHAIN_PATH" …` it fails with **`no identity found`** even
   though the cert imported fine.
2. **`codesign --deep` skips `Contents/MonoBundle`.** Those Mono dylibs are dynamically loaded (not
   linked), so `--deep` never signs them and notarization rejects them ("not signed" / "no secure
   timestamp"). **Sign the MonoBundle Mach-O explicitly** before sealing.
3. **`--deep` is required to *seal* the bundle.** The embedded `wp` is a self-contained CoreCLR payload —
   a directory of loose files (`.dll`/`.pdb`/`.json` + dylibs) under `Contents/Helpers`. A plain
   (non-`--deep`) bundle sign rejects those data files as unsigned "subcomponents"; only `--deep` seals
   them. So the recipe is: explicitly sign MonoBundle, **then** `codesign --deep` the whole bundle.
4. **Sign only what `--deep` misses (MonoBundle), not every nested binary.** Signing all nested Mach-O
   meant `--deep` re-timestamped the `Helpers` payload too; the two concurrent arch jobs then flooded
   Apple's timestamp server with `--timestamp` calls and the step **stalled 30+ minutes**.
5. **`notarytool … --wait` isn't network-resilient.** A transient blip mid-poll (`NSURLErrorDomain -1009`)
   makes it exit non-zero and fail the release even though notarization was progressing. So **submit
   once, retry the `wait` (no re-upload), then gate on the `info` status** and dump `notarytool log` on
   any non-`Accepted` result so failures self-diagnose.

## Teardown (if ever needed)

```bash
# Stop signing without deleting the cert: remove the secrets so HAS_APPLE_SIGNING goes false.
for s in APPLE_CERTIFICATE_BASE64 APPLE_CERTIFICATE_PASSWORD APPLE_SIGNING_IDENTITY \
         APPLE_TEAM_ID APPLE_ID APPLE_APP_SPECIFIC_PASSWORD; do gh secret delete "$s"; done
# Revoke the cert itself at developer.apple.com ▸ Certificates, and the app-specific password at appleid.apple.com.
```
