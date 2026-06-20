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
