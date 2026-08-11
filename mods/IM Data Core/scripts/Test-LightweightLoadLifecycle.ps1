param(
    [Parameter(Mandatory = $false)]
    [string]$RepoRoot = (Get-Location).Path
)

$ErrorActionPreference = 'Stop'

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$projectRoot = Join-Path $RepoRoot 'mods\IM Data Core'
$lifecyclePath = Join-Path $projectRoot 'src\Patches\Core\CoreLifecyclePatches.cs'
$persistencePath = Join-Path $projectRoot 'src\Controller\IMDataCoreController.PersistenceV2.cs'

Assert-True (Test-Path -LiteralPath $lifecyclePath -PathType Leaf) `
    "Missing lifecycle source: $lifecyclePath"
Assert-True (Test-Path -LiteralPath $persistencePath -PathType Leaf) `
    "Missing PersistenceV2 source: $persistencePath"

$lifecycle = [System.IO.File]::ReadAllText($lifecyclePath).Replace("`r`n", "`n")
$persistence = [System.IO.File]::ReadAllText($persistencePath).Replace("`r`n", "`n")

# Regression: old transaction/fingerprint load coordinator must not be wired to Harmony.
Assert-True (-not $lifecycle.Contains('ProcessMainThreadTransactions')) `
    'The obsolete per-frame transaction pump is still wired into CoreLifecyclePatches.cs.'
Assert-True (-not $lifecycle.Contains('.OnSaveLoadStarting(')) `
    'The obsolete staging/fingerprint load-start coordinator is still wired.'
Assert-True (-not $lifecycle.Contains('.OnSaveLoaded(')) `
    'The obsolete staging publish completion is still wired.'
Assert-True (-not $lifecycle.Contains('expectedBytes')) `
    'The save hook still constructs expected vanilla bytes.'
Assert-True (-not $lifecycle.Contains('JsonUtility.ToJson(savedData')) `
    'The save hook still serializes vanilla SavedData for IMDC identity.'

# Regression: exactly one production restore location should exist in the lifecycle source.
$restoreNeedle = '.OnVanillaSaveDataRead('
$restoreCount = ([regex]::Matches(
    $lifecycle,
    [regex]::Escape($restoreNeedle))).Count
Assert-True ($restoreCount -eq 1) `
    "Expected exactly one lifecycle OnVanillaSaveDataRead call; found $restoreCount."

Assert-True ($lifecycle.Contains(
    "saveManager.Data,`n                    state.RequestedSavePath")) `
    'The pre-LoadEvent restore is not receiving the resolved vanilla load path.'

Assert-True ($lifecycle.Contains('.OnVanillaLoadCompleted()')) `
    'The successful postfix is not using the lightweight completion hook.'

Assert-True ($lifecycle.Contains('state.RestorationPerformed = true;')) `
    'The per-LoadData idempotency marker is missing.'

# Defense in depth inside PersistenceV2.
Assert-True ($persistence.Contains(
    'ignored a duplicate SaveManager.Data restoration')) `
    'PersistenceV2 duplicate-restoration guard is missing.'
Assert-True ($persistence.Contains(
    'internal void CancelVanillaLoadPreparation()')) `
    'PersistenceV2 cancellation hook is missing.'
Assert-True ($persistence.Contains(
    'post-load chart-position seeding failed')) `
    'PersistenceV2 post-load seeding is not fail-soft.'

Write-Host 'PASS: lightweight load lifecycle source invariants are present.'
