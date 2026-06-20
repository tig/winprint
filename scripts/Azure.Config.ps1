<#
.SYNOPSIS
  Single source of truth for WinPrint's Azure Trusted Signing + GitHub OIDC trust.

.DESCRIPTION
  Dot-sourced/invoked by SetupAzure.ps1 and ValidateAzure.ps1. Edit values HERE only.
  Returns a hashtable when invoked:  $cfg = & "$PSScriptRoot/Azure.Config.ps1"

  None of these values are secret — they are Azure/GitHub identifiers, not credentials.
  The actual trust is the federated credential (no client secret is ever created).
#>
@{
    # --- Azure (Kindel LLC subscription) ---
    SubscriptionId = '7bee0c7c-3217-4628-a783-dd7d687112d3'   # Kindel LLC
    ResourceGroup  = 'WinPrint_Resources'
    Location       = 'eastus'

    # --- Trusted Signing account + certificate profile (created in the portal; see docs) ---
    SigningAccount = 'winprint'
    CertProfile    = 'WinPrint'

    # --- Entra ID app registration that GitHub Actions authenticates as via OIDC ---
    AppDisplayName = 'winprint'

    # --- GitHub repo + the refs allowed to mint an OIDC token ---
    GhOwner        = 'Kindel'
    GhRepo         = 'winprint'
    Branches       = @('develop', 'main')   # plus refs/tags/* (always added)

    # --- RBAC role granted to the app's service principal at the cert-profile scope.
    #     Microsoft renamed "Trusted Signing Certificate Profile Signer" ->
    #     "Artifact Signing Certificate Profile Signer"; both grant signing. ---
    SignerRole     = 'Artifact Signing Certificate Profile Signer'
}
