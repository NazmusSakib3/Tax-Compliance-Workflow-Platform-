# Pilot release automation: local CI, Docker deploy, and verification.
#
# Usage:
#   .\scripts\pilot-release.ps1                    # full run (CI + deploy + verify)
#   .\scripts\pilot-release.ps1 -SkipCi            # deploy and verify only
#   .\scripts\pilot-release.ps1 -NoCache           # rebuild images without cache (see docs/deployment.md)
#
# Skip flags: -SkipCi, -SkipDeploy, -SkipVerify
# Build flags: -NoCache (passes --no-cache --pull to docker compose build)

param(
    [string]$EnvFile = ".env.pilot",
    [string]$PublicHttpPort = "8088",
    [string]$PublicOrigin = "http://localhost:8088",
    [switch]$SkipCi,
    [switch]$SkipDeploy,
    [switch]$SkipVerify,
    [switch]$NoCache
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

function Write-Step([string]$Message) {
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function New-RandomSecret([int]$Length = 32) {
    $bytes = New-Object byte[] $Length
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', 'x').Replace('/', 'y').Substring(0, $Length)
}

function Ensure-Docker {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        throw "Docker is required for pilot deployment. Install Docker Desktop and retry."
    }
}

function Ensure-PilotEnvFile {
    $envPath = Join-Path $root $EnvFile
    if (-not (Test-Path $envPath)) {
        Write-Step "Creating $EnvFile from .env.pilot.example with generated secrets"
        $template = Get-Content (Join-Path $root ".env.pilot.example") -Raw
        $template = $template.Replace("replace-with-strong-database-password", (New-RandomSecret 24))
        $template = $template.Replace("replace-with-strong-redis-password", (New-RandomSecret 24))
        $template = $template.Replace("replace-with-strong-rabbitmq-password", (New-RandomSecret 24))
        $template = $template.Replace("replace-with-at-least-32-random-characters", (New-RandomSecret 48))
        $template = $template.Replace("replace-with-temporary-strong-admin-password", "PilotAdmin!$(New-RandomSecret 8)")
        Set-Content -Path $envPath -Value $template -NoNewline
        Write-Host "Created $EnvFile (not committed). Review before production use."
    }

    return $envPath
}

function Read-EnvValue([string]$EnvPath, [string]$Key) {
    $line = Get-Content $EnvPath | Where-Object { $_ -match "^\s*$([regex]::Escape($Key))\s*=" } | Select-Object -First 1
    if (-not $line) { return $null }
    return ($line -split '=', 2)[1].Trim()
}

function Invoke-PilotCiChecks {
    Write-Step "Running local CI checks (backend + frontend)"

    dotnet restore backend/TaxCompliance.sln
    dotnet build backend/TaxCompliance.sln --configuration Release -warnaserror
    dotnet test backend/TaxCompliance.sln --configuration Release --no-build --verbosity minimal `
        /p:CollectCoverage=true `
        /p:CoverletOutputFormat=opencover `
        /p:CoverletOutput=./TestResults/coverage/ `
        /p:Threshold=45 `
        /p:ThresholdType=line `
        /p:ThresholdStat=total

    Push-Location (Join-Path $root "frontend")
    try {
        npm ci
        npm run build:production
        npm run test:ci
    }
    finally {
        Pop-Location
    }

    Write-Host "Local CI checks passed." -ForegroundColor Green
}

function Invoke-PilotDeploy([string]$EnvPath, [switch]$NoCache) {
    Write-Step "Building and starting pilot stack"
    $composeArgs = @(
        "compose", "--env-file", $EnvPath,
        "-f", "docker-compose.production.yml",
        "-f", "docker-compose.pilot.yml",
        "up", "-d", "--build"
    )
    if ($NoCache) {
        Write-Host "Rebuilding with --no-cache --pull (fresh base images, no build cache)" -ForegroundColor Yellow
        $composeArgs += "--no-cache", "--pull"
    }
    & docker @composeArgs
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose up failed with exit code $LASTEXITCODE"
    }

    $deadline = (Get-Date).AddMinutes(5)
    do {
        try {
            $response = Invoke-WebRequest -Uri "$PublicOrigin/health" -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -eq 200) {
                Write-Host "Health check OK: $PublicOrigin/health" -ForegroundColor Green
                return
            }
        }
        catch {
            Start-Sleep -Seconds 3
        }
    } while ((Get-Date) -lt $deadline)

    throw "Pilot stack did not become healthy at $PublicOrigin/health within 5 minutes."
}

function Get-TotpCode([string]$SharedKey) {
    $totpProject = Join-Path $root "scripts\totp-code\TotpCode.csproj"
    if (-not (Test-Path $totpProject)) {
        throw "TOTP helper project not found at $totpProject"
    }

    $output = & dotnet run --project $totpProject --no-build -c Release -- $SharedKey 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to generate TOTP code: $output"
    }

    return ($output | Select-Object -Last 1).Trim()
}

function Ensure-TotpHelperBuilt {
    $totpProject = Join-Path $root "scripts\totp-code\TotpCode.csproj"
    & dotnet build $totpProject -c Release --verbosity quiet | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to build TOTP helper at $totpProject"
    }
}

function Invoke-ApiJson {
    param(
        [string]$Method,
        [string]$Uri,
        [hashtable]$Headers = @{},
        [object]$Body = $null
    )

    $params = @{
        Uri = $Uri
        Method = $Method
        Headers = $Headers
        ContentType = "application/json"
    }
    if ($null -ne $Body) {
        $params.Body = ($Body | ConvertTo-Json -Depth 6)
    }

    return Invoke-RestMethod @params
}

function Invoke-PilotMfaVerification {
    param(
        [string]$ApiBase,
        [string]$AdminEmail,
        [string]$AdminPassword
    )

    $login = Invoke-ApiJson -Method Post -Uri "$ApiBase/auth/login" -Body @{
        email = $AdminEmail
        password = $AdminPassword
    }
    $token = $login.data.accessToken
    $authHeaders = @{ Authorization = "Bearer $token" }

    $setup = Invoke-ApiJson -Method Post -Uri "$ApiBase/auth/mfa/setup" -Headers $authHeaders
    if (-not $setup.data.sharedKey) {
        throw "MFA setup did not return a shared key."
    }

    $mfaCode = Get-TotpCode $setup.data.sharedKey
    $enable = Invoke-ApiJson -Method Post -Uri "$ApiBase/auth/mfa/enable" -Headers $authHeaders -Body @{
        code = $mfaCode
    }
    if (-not $enable.success) {
        throw "MFA enable failed: $($enable.message)"
    }

    try {
        Invoke-ApiJson -Method Post -Uri "$ApiBase/auth/login" -Body @{
            email = $AdminEmail
            password = $AdminPassword
        } | Out-Null
        throw "Login succeeded without MFA code after MFA was enabled."
    }
    catch {
        $detail = $_.ErrorDetails.Message
        if (-not $detail -and $_.Exception.Response) {
            $reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
            $detail = $reader.ReadToEnd()
            $reader.Close()
        }
        if ($detail -notmatch "MFA|requiresMfa|401") {
            throw
        }
    }

    $mfaLoginCode = Get-TotpCode $setup.data.sharedKey
    $mfaLogin = Invoke-ApiJson -Method Post -Uri "$ApiBase/auth/login" -Body @{
        email = $AdminEmail
        password = $AdminPassword
        mfaCode = $mfaLoginCode
    }
    if (-not $mfaLogin.success) {
        throw "MFA login failed after enabling MFA."
    }

    $disableCode = Get-TotpCode $setup.data.sharedKey
    $disable = Invoke-ApiJson -Method Post -Uri "$ApiBase/auth/mfa/disable" -Headers $authHeaders -Body @{
        code = $disableCode
    }
    if (-not $disable.success) {
        throw "MFA disable failed: $($disable.message)"
    }

    Write-Host "MFA setup, challenge login, and disable OK." -ForegroundColor Green
}

function Invoke-PilotDashboardExportVerification {
    param(
        [string]$ApiBase,
        [string]$AdminEmail,
        [string]$AdminPassword
    )

    $login = Invoke-ApiJson -Method Post -Uri "$ApiBase/auth/login" -Body @{
        email = $AdminEmail
        password = $AdminPassword
    }
    $headers = @{ Authorization = "Bearer $($login.data.accessToken)" }

    $response = Invoke-WebRequest -Uri "$ApiBase/dashboard/export" -Headers $headers -UseBasicParsing
    if ($response.StatusCode -ne 200) {
        throw "Dashboard CSV export returned status $($response.StatusCode)."
    }

    $contentType = $response.Headers["Content-Type"]
    if ($contentType -notmatch "text/csv") {
        throw "Dashboard export content type was '$contentType', expected text/csv."
    }

    if ($response.Content -notmatch "Metric,Value") {
        throw "Dashboard CSV export did not contain expected header row."
    }

    Write-Host "Dashboard CSV export OK." -ForegroundColor Green
}

function Invoke-PilotRoleFlowVerification {
    param(
        [string]$ApiBase,
        [string]$AdminEmail,
        [string]$AdminPassword
    )

    $suffix = (Get-Date -Format "HHmmss")
    $contributorEmail = "pilot.contributor.$suffix@pilot.local"
    $contributorPassword = "PilotContributor!$(New-RandomSecret 8)"

    $adminLogin = Invoke-ApiJson -Method Post -Uri "$ApiBase/auth/login" -Body @{
        email = $AdminEmail
        password = $AdminPassword
    }
    $adminHeaders = @{ Authorization = "Bearer $($adminLogin.data.accessToken)" }

    $organization = Invoke-ApiJson -Method Post -Uri "$ApiBase/organizations" -Headers $adminHeaders -Body @{
        name = "Pilot Verification Org $suffix"
        code = "PILOT$suffix"
        description = "Created by pilot-release verification"
        isActive = $true
    }
    $orgHeaders = $adminHeaders.Clone()
    $orgHeaders["X-Organization-Id"] = $organization.id

    $jurisdiction = Invoke-ApiJson -Method Post -Uri "$ApiBase/jurisdictions" -Headers $orgHeaders -Body @{
        name = "Pilot Jurisdiction"
        countryCode = "US"
        regionCode = "NY"
        filingAuthority = "Pilot Authority"
        isActive = $true
    }

    $template = Invoke-ApiJson -Method Post -Uri "$ApiBase/compliance-templates" -Headers $orgHeaders -Body @{
        name = "Pilot Template"
        filingType = "VAT"
        description = "Pilot verification template"
        reminderDaysBeforeDue = 5
        isActive = $true
    }

    $legalEntity = Invoke-ApiJson -Method Post -Uri "$ApiBase/legal-entities" -Headers $orgHeaders -Body @{
        organizationId = $organization.id
        name = "Pilot Legal Entity"
        registrationNumber = "PILOT-REG-$suffix"
        taxIdentifier = "PILOT-TAX-$suffix"
        isActive = $true
    }

    Invoke-ApiJson -Method Post -Uri "$ApiBase/compliance-task-rules" -Headers $orgHeaders -Body @{
        legalEntityId = $legalEntity.id
        jurisdictionId = $jurisdiction.id
        complianceTemplateId = $template.id
        title = "Pilot Monthly Rule"
        description = "Pilot verification rule"
        recurrenceType = 1
        dueDayOfMonth = 20
        isActive = $true
    } | Out-Null

    Invoke-RestMethod -Uri "$ApiBase/compliance-task-occurrences/generate" -Method Post -Headers $orgHeaders | Out-Null

    $occurrences = Invoke-RestMethod -Uri "$ApiBase/compliance-task-occurrences?page=1&pageSize=10" -Method Get -Headers $orgHeaders
    if (-not $occurrences.items -or $occurrences.items.Count -lt 1) {
        throw "Role flow verification could not find generated task occurrences."
    }
    $occurrenceId = $occurrences.items[0].id

    $contributor = Invoke-ApiJson -Method Post -Uri "$ApiBase/users" -Headers $orgHeaders -Body @{
        email = $contributorEmail
        displayName = "Pilot Contributor"
        password = $contributorPassword
        role = "Contributor"
    }

    Invoke-ApiJson -Method Post -Uri "$ApiBase/compliance-task-occurrences/$occurrenceId/assignment" -Headers $orgHeaders -Body @{
        assignedToUserId = $contributor.userId
    } | Out-Null

    $contributorLogin = Invoke-ApiJson -Method Post -Uri "$ApiBase/auth/login" -Body @{
        email = $contributorEmail
        password = $contributorPassword
    }
    $contributorHeaders = @{
        Authorization = "Bearer $($contributorLogin.data.accessToken)"
        "X-Organization-Id" = $organization.id
    }

    $contributorOccurrences = Invoke-RestMethod -Uri "$ApiBase/compliance-task-occurrences?page=1&pageSize=50" -Method Get -Headers $contributorHeaders
    $visibleIds = @($contributorOccurrences.items | ForEach-Object { $_.id })
    if ($visibleIds -notcontains $occurrenceId) {
        throw "Contributor could not see the assigned task occurrence."
    }
    if ($visibleIds.Count -gt 1) {
        throw "Contributor saw $($visibleIds.Count) occurrences; expected only the assigned task."
    }

    Write-Host "Contributor role filter and assignment flow OK." -ForegroundColor Green
}

function Decode-QuotedPrintableBody([string]$Body) {
    if ([string]::IsNullOrWhiteSpace($Body)) {
        return $Body
    }

    $normalized = $Body -replace "=\r?\n", ""
    $bytes = New-Object System.Collections.Generic.List[byte]
    for ($i = 0; $i -lt $normalized.Length; $i++) {
        if ($normalized[$i] -eq '=' -and $i + 2 -lt $normalized.Length) {
            $hex = $normalized.Substring($i + 1, 2)
            if ($hex -match '^[0-9A-Fa-f]{2}$') {
                $bytes.Add([Convert]::ToByte($hex, 16))
                $i += 2
                continue
            }
        }

        $bytes.Add([byte][char]$normalized[$i])
    }

    return [Text.Encoding]::UTF8.GetString($bytes.ToArray())
}

function Decode-Base64Url([string]$Value) {
    $padded = $Value.Replace('-', '+').Replace('_', '/')
    switch ($padded.Length % 4) {
        2 { $padded += '==' }
        3 { $padded += '=' }
    }

    return [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($padded))
}

function Get-MailHogResetLink([string]$RecipientEmail, [int]$MailHogUiPort = 8025) {
    $messages = Invoke-RestMethod -Uri "http://localhost:$MailHogUiPort/api/v2/messages" -Method Get
    $message = $messages.items |
        Where-Object { $_.Content.Headers.To -contains $RecipientEmail } |
        Select-Object -First 1

    if (-not $message) {
        throw "No password reset email found in MailHog for $RecipientEmail."
    }

    $body = Decode-QuotedPrintableBody $message.Content.Body
    if ($body -match '(https?://[^\s<>"]+reset-password[^\s<>"]*)') {
        return $Matches[1]
    }

    throw "Password reset email did not contain a reset link."
}

function Invoke-PilotVerification([string]$EnvPath) {
    Write-Step "Verifying login, password reset email, and admin credential rotation"

    $adminEmail = Read-EnvValue $EnvPath "Seed__AdminEmail"
    $seedPassword = Read-EnvValue $EnvPath "Seed__AdminPassword"
    $mailHogPort = [int](Read-EnvValue $EnvPath "MAILHOG_UI_PORT")
    if (-not $mailHogPort) { $mailHogPort = 8025 }
    $apiBase = "$PublicOrigin/api"

    $login = Invoke-RestMethod -Uri "$apiBase/auth/login" -Method Post -ContentType "application/json" `
        -Body (@{ email = $adminEmail; password = $seedPassword } | ConvertTo-Json)
    if (-not $login.success) {
        throw "Initial admin login failed."
    }
    Write-Host "Admin login OK." -ForegroundColor Green

    Invoke-RestMethod -Uri "$apiBase/auth/forgot-password" -Method Post -ContentType "application/json" `
        -Body (@{ email = $adminEmail } | ConvertTo-Json) | Out-Null

    Start-Sleep -Seconds 2
    $resetLink = Get-MailHogResetLink -RecipientEmail $adminEmail -MailHogUiPort $mailHogPort
    $uri = [Uri]$resetLink
    $emailParam = [System.Web.HttpUtility]::ParseQueryString($uri.Query).Get("email")
    $tokenParam = [System.Web.HttpUtility]::ParseQueryString($uri.Query).Get("token")
    $decodedEmail = Decode-Base64Url $emailParam
    $rotatedPassword = "PilotRotated!$(New-RandomSecret 10)"

    $reset = Invoke-RestMethod -Uri "$apiBase/auth/reset-password" -Method Post -ContentType "application/json" `
        -Body (@{
            email = $decodedEmail
            token = $tokenParam
            newPassword = $rotatedPassword
        } | ConvertTo-Json)
    if (-not $reset.success) {
        throw "Admin password rotation via reset flow failed: $($reset.message)"
    }

    try {
        Invoke-RestMethod -Uri "$apiBase/auth/login" -Method Post -ContentType "application/json" `
            -Body (@{ email = $adminEmail; password = $seedPassword } | ConvertTo-Json) | Out-Null
        throw "Old admin password still works after rotation."
    }
    catch {
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode.value__ -ne 401) {
            throw
        }
    }

    $rotatedLogin = Invoke-RestMethod -Uri "$apiBase/auth/login" -Method Post -ContentType "application/json" `
        -Body (@{ email = $adminEmail; password = $rotatedPassword } | ConvertTo-Json)
    if (-not $rotatedLogin.success) {
        throw "Login with rotated admin password failed."
    }

    Write-Host "Password reset email delivered and admin credentials rotated." -ForegroundColor Green
    Write-Host "Store the rotated password securely. It is not written to disk by default."

    Write-Step "Verifying MFA, dashboard CSV export, and contributor role flows"
    Ensure-TotpHelperBuilt
    Invoke-PilotDashboardExportVerification -ApiBase $apiBase -AdminEmail $adminEmail -AdminPassword $rotatedPassword
    Invoke-PilotMfaVerification -ApiBase $apiBase -AdminEmail $adminEmail -AdminPassword $rotatedPassword
    Invoke-PilotRoleFlowVerification -ApiBase $apiBase -AdminEmail $adminEmail -AdminPassword $rotatedPassword

    $reportPath = Join-Path $root "pilot-release-report.txt"
    @(
        "Pilot release verification: $(Get-Date -Format o)"
        "Public origin: $PublicOrigin"
        "Health: $PublicOrigin/health"
        "MailHog UI: http://localhost:$mailHogPort"
        "Admin email: $adminEmail"
        "Rotated admin password: $rotatedPassword"
        ""
        "Verified:"
        "- GET /health"
        "- Forgot/reset password email via MailHog"
        "- Admin credential rotation"
        "- GET /api/dashboard/export (CSV)"
        "- MFA setup, challenge login, disable"
        "- Contributor assignment and task visibility filter"
        ""
        "Next steps:"
        "- Terminate TLS at your reverse proxy and update PUBLIC_APP_ORIGIN / PasswordReset__ClientResetUrl"
        "- Point DNS to the public origin"
        "- Confirm GitHub Actions CI is green on the release branch"
    ) | Set-Content -Path $reportPath
    Write-Host "Wrote $reportPath"
}

Write-Step "Pilot release automation"
$envPath = Ensure-PilotEnvFile

if (-not $SkipCi) {
    Invoke-PilotCiChecks
}

if (-not $SkipDeploy -or -not $SkipVerify) {
    Ensure-Docker
}

if (-not $SkipDeploy) {
    Invoke-PilotDeploy -EnvPath $envPath -NoCache:$NoCache
}

if (-not $SkipVerify) {
    Add-Type -AssemblyName System.Web
    Invoke-PilotVerification -EnvPath $envPath
}

Write-Host ""
Write-Host "Pilot release workflow completed." -ForegroundColor Green
