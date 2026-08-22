[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [string]$RepoRoot = "",

    [string]$DeployRoot = (Join-Path $env:USERPROFILE "AppData\LocalLow\Glitch Pitch\Idol Manager\Mods"),

    [switch]$SkipBuild,

    # Existing *.config.ini files in an installed mod folder are treated as
    # user settings and preserved by default. Use this switch only when the
    # packaged defaults should replace the live configuration.
    [switch]$OverwriteConfig
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) {
        $RepoRoot = Split-Path -Parent $PSScriptRoot
    }
    else {
        $RepoRoot = (Get-Location).Path
    }
}

if (-not (Test-Path -LiteralPath $RepoRoot -PathType Container)) {
    throw "Repository root not found: $RepoRoot"
}

$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path

if ([string]::IsNullOrWhiteSpace($DeployRoot)) {
    throw "DeployRoot cannot be empty."
}

if (-not [System.IO.Path]::IsPathRooted($DeployRoot)) {
    $DeployRoot = Join-Path (Get-Location).Path $DeployRoot
}
$DeployRoot = [System.IO.Path]::GetFullPath($DeployRoot)

function Get-ProjectMetadata {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    try {
        [xml]$projectXml = Get-Content -LiteralPath $ProjectPath -Raw
    }
    catch {
        throw "Unable to read project file '$ProjectPath': $($_.Exception.Message)"
    }

    $propertyGroups = @($projectXml.Project.PropertyGroup)
    $modName = $null
    $assemblyName = $null
    $deployFolderName = $null

    foreach ($propertyGroup in $propertyGroups) {
        if (-not $modName -and $propertyGroup.ModName) {
            $modName = ([string]$propertyGroup.ModName).Trim()
        }

        if (-not $assemblyName -and $propertyGroup.AssemblyName) {
            $assemblyName = ([string]$propertyGroup.AssemblyName).Trim()
        }

        if (-not $deployFolderName -and $propertyGroup.DeployFolderName) {
            $deployFolderName = ([string]$propertyGroup.DeployFolderName).Trim()
        }
    }

    if ([string]::IsNullOrWhiteSpace($modName) -or [string]::IsNullOrWhiteSpace($assemblyName)) {
        return $null
    }

    if ([string]::IsNullOrWhiteSpace($deployFolderName)) {
        $deployFolderName = $null
    }

    $projectDir = Split-Path -Parent $ProjectPath
    $projectReferences = @()
    foreach ($itemGroup in @($projectXml.Project.ItemGroup)) {
        foreach ($projectReference in @($itemGroup.ProjectReference)) {
            if ($null -eq $projectReference -or [string]::IsNullOrWhiteSpace([string]$projectReference.Include)) {
                continue
            }

            $referencedPath = Join-Path $projectDir ([string]$projectReference.Include)
            $projectReferences += [System.IO.Path]::GetFullPath($referencedPath)
        }
    }

    return [PSCustomObject]@{
        ProjectPath       = [System.IO.Path]::GetFullPath($ProjectPath)
        ProjectDir        = $projectDir
        ModName           = $modName
        AssemblyName      = $assemblyName
        DeployFolderName  = $deployFolderName
        ProjectReferences = @($projectReferences)
        ArtifactDir       = Join-Path $RepoRoot ("artifacts\mods\{0}\{1}" -f $Configuration, $modName)
    }
}

function Get-ModInfoJson {
    param(
        [Parameter(Mandatory = $true)]
        [string]$InfoJsonPath,

        [switch]$AllowInvalid
    )

    if (-not (Test-Path -LiteralPath $InfoJsonPath -PathType Leaf)) {
        return $null
    }

    try {
        $info = Get-Content -LiteralPath $InfoJsonPath -Raw | ConvertFrom-Json
        return $info
    }
    catch {
        if ($AllowInvalid) {
            Write-Warning "Ignoring invalid info.json '$InfoJsonPath': $($_.Exception.Message)"
            return $null
        }

        throw "Invalid info.json '$InfoJsonPath': $($_.Exception.Message)"
    }
}

function Get-HarmonyIdFromInfoJson {
    param(
        [Parameter(Mandatory = $true)]
        [string]$InfoJsonPath,

        [switch]$AllowInvalid
    )

    $info = Get-ModInfoJson -InfoJsonPath $InfoJsonPath -AllowInvalid:$AllowInvalid
    if ($null -eq $info -or [string]::IsNullOrWhiteSpace([string]$info.HarmonyID)) {
        return $null
    }

    return ([string]$info.HarmonyID).Trim()
}

function Get-RelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BasePath,

        [Parameter(Mandatory = $true)]
        [string]$ChildPath
    )

    $resolvedBasePath = (Resolve-Path -LiteralPath $BasePath).Path
    $resolvedChildPath = (Resolve-Path -LiteralPath $ChildPath).Path
    $baseUri = New-Object System.Uri($resolvedBasePath.TrimEnd('\') + '\')
    $childUri = New-Object System.Uri($resolvedChildPath)
    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($childUri).ToString()).Replace('/', '\')
}

function Test-IsUserConfigFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $fileName = [System.IO.Path]::GetFileName($Path)
    return $fileName.EndsWith(
        ".config.ini",
        [System.StringComparison]::OrdinalIgnoreCase)
}

function Build-InstalledModMap {
    param(
        [Parameter(Mandatory = $true)]
        [string]$InstalledModsRoot
    )

    $map = @{}

    # A missing Mods directory is valid for a first-time deploy. Deploy-Project
    # will create the target directories when actual copying begins.
    if (-not (Test-Path -LiteralPath $InstalledModsRoot -PathType Container)) {
        return $map
    }

    foreach ($installedDirectory in Get-ChildItem -LiteralPath $InstalledModsRoot -Directory) {
        $infoJsonPath = Join-Path $installedDirectory.FullName "info.json"
        $harmonyId = Get-HarmonyIdFromInfoJson -InfoJsonPath $infoJsonPath -AllowInvalid
        if ([string]::IsNullOrWhiteSpace($harmonyId)) {
            continue
        }

        if ($map.ContainsKey($harmonyId)) {
            throw "Duplicate deployed HarmonyID '$harmonyId' found in '$($installedDirectory.FullName)' and '$($map[$harmonyId])'."
        }

        $map[$harmonyId] = $installedDirectory.FullName
    }

    return $map
}

function Resolve-DeployTargetDir {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Project,

        [Parameter(Mandatory = $true)]
        [string]$HarmonyId,

        [Parameter(Mandatory = $true)]
        [hashtable]$InstalledModMap,

        [Parameter(Mandatory = $true)]
        [string]$InstalledModsRoot
    )

    # Prefer an already-installed folder matched by HarmonyID so deploys stay aligned
    # with the live mod directory layout, even when it differs from the repo folder name.
    if ($InstalledModMap.ContainsKey($HarmonyId)) {
        return $InstalledModMap[$HarmonyId]
    }

    $folderName = if (-not [string]::IsNullOrWhiteSpace($Project.DeployFolderName)) {
        $Project.DeployFolderName
    }
    else {
        $Project.ModName
    }

    $candidate = Join-Path $InstalledModsRoot $folderName

    # If the preferred first-time folder already belongs to a different HarmonyID,
    # fail closed instead of overwriting an unrelated installed mod.
    if (Test-Path -LiteralPath $candidate -PathType Container) {
        $candidateInfoPath = Join-Path $candidate "info.json"
        $candidateHarmonyId = Get-HarmonyIdFromInfoJson -InfoJsonPath $candidateInfoPath -AllowInvalid

        if (-not [string]::IsNullOrWhiteSpace($candidateHarmonyId) -and
            -not [string]::Equals($candidateHarmonyId, $HarmonyId, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Deploy target collision: '$candidate' belongs to HarmonyID '$candidateHarmonyId', not '$HarmonyId'."
        }
    }

    return $candidate
}

function Resolve-ProjectBuildOrder {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Projects
    )

    $projectMap = @{}
    foreach ($project in $Projects) {
        $projectMap[$project.ProjectPath] = $project
    }

    $remaining = @($Projects)
    $ordered = @()
    $completed = @{}

    while ($remaining.Count -gt 0) {
        $ready = @(
            $remaining |
                Where-Object {
                    $project = $_
                    $blocked = $false

                    foreach ($referencePath in @($project.ProjectReferences)) {
                        if ($projectMap.ContainsKey($referencePath) -and -not $completed.ContainsKey($referencePath)) {
                            $blocked = $true
                            break
                        }
                    }

                    -not $blocked
                } |
                Sort-Object ProjectPath
        )

        if ($ready.Count -eq 0) {
            $cycleNames = ($remaining | Sort-Object ModName | ForEach-Object { $_.ModName }) -join ", "
            throw "ProjectReference dependency cycle detected among deployable mods: $cycleNames"
        }

        $readyMap = @{}
        foreach ($project in $ready) {
            $ordered += $project
            $completed[$project.ProjectPath] = $true
            $readyMap[$project.ProjectPath] = $true
        }

        $remaining = @($remaining | Where-Object { -not $readyMap.ContainsKey($_.ProjectPath) })
    }

    return $ordered
}

function Get-FileSha256 {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
}

function Assert-ArtifactAssetsMatchSource {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Project
    )

    $assetsDir = Join-Path $Project.ProjectDir "assets"
    if (-not (Test-Path -LiteralPath $assetsDir -PathType Container)) {
        return
    }

    $sourceFiles = @(
        Get-ChildItem -LiteralPath $assetsDir -Recurse -File |
            Sort-Object FullName
    )

    $sourceRelativePaths = @{}
    foreach ($sourceFile in $sourceFiles) {
        $relativePath = Get-RelativePath -BasePath $assetsDir -ChildPath $sourceFile.FullName
        $sourceRelativePaths[$relativePath] = $sourceFile.FullName

        $artifactPath = Join-Path $Project.ArtifactDir $relativePath
        if (-not (Test-Path -LiteralPath $artifactPath -PathType Leaf)) {
            throw "Packaged asset missing for '$($Project.ModName)': $relativePath"
        }

        $sourceHash = Get-FileSha256 -Path $sourceFile.FullName
        $artifactHash = Get-FileSha256 -Path $artifactPath
        if (-not [string]::Equals($sourceHash, $artifactHash, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Packaged asset is stale for '$($Project.ModName)': $relativePath"
        }
    }

    # The artifact directory should contain exactly the current assets plus the
    # compiled DLL/PDB. Anything else is stale packaging residue and should fail
    # the build rather than being deployed silently.
    foreach ($artifactFile in Get-ChildItem -LiteralPath $Project.ArtifactDir -Recurse -File) {
        $relativePath = Get-RelativePath -BasePath $Project.ArtifactDir -ChildPath $artifactFile.FullName
        $expectedDllName = $Project.AssemblyName + ".dll"
        $expectedPdbName = $Project.AssemblyName + ".pdb"

        if ([string]::Equals($relativePath, $expectedDllName, [System.StringComparison]::OrdinalIgnoreCase) -or
            [string]::Equals($relativePath, $expectedPdbName, [System.StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        if (-not $sourceRelativePaths.ContainsKey($relativePath)) {
            throw "Stale packaged asset found for '$($Project.ModName)': $relativePath"
        }
    }
}

function Remove-LegacyProjectArtifact {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Project
    )

    # Older invocations sometimes passed a relative ModOutputDir, which created
    # project-local trees such as mods/Cheats Mod/artifacts/mods/Release/....
    # They are generated output, not source, and can easily be mistaken for the
    # canonical repository-level artifacts directory.
    $legacyArtifactDir = Join-Path $Project.ProjectDir ("artifacts\mods\{0}\{1}" -f $Configuration, $Project.ModName)
    $legacyArtifactDir = [System.IO.Path]::GetFullPath($legacyArtifactDir)
    $canonicalArtifactDir = [System.IO.Path]::GetFullPath($Project.ArtifactDir)

    if ([string]::Equals($legacyArtifactDir, $canonicalArtifactDir, [System.StringComparison]::OrdinalIgnoreCase)) {
        return
    }

    if (Test-Path -LiteralPath $legacyArtifactDir -PathType Container) {
        Write-Host ("Removing legacy project-local artifact for {0}: {1}" -f $Project.ModName, $legacyArtifactDir)
        Remove-Item -LiteralPath $legacyArtifactDir -Recurse -Force
    }
}

function Build-Project {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Project
    )

    $sourceInputs = @(
        (Get-Item -LiteralPath $Project.ProjectPath)
    )

    $srcDir = Join-Path $Project.ProjectDir "src"
    if (Test-Path -LiteralPath $srcDir -PathType Container) {
        $sourceInputs += Get-ChildItem -LiteralPath $srcDir -Recurse -File |
            Where-Object { $_.Extension -eq ".cs" }
    }

    $assetsDir = Join-Path $Project.ProjectDir "assets"
    if (Test-Path -LiteralPath $assetsDir -PathType Container) {
        $sourceInputs += Get-ChildItem -LiteralPath $assetsDir -Recurse -File
    }

    # Directory.Build.props participates in every project build and can contain
    # shared compile items, references, and packaging rules.
    $directoryBuildProps = Join-Path $RepoRoot "Directory.Build.props"
    if (Test-Path -LiteralPath $directoryBuildProps -PathType Leaf) {
        $sourceInputs += Get-Item -LiteralPath $directoryBuildProps
    }

    $directoryBuildTargets = Join-Path $RepoRoot "Directory.Build.targets"
    if (Test-Path -LiteralPath $directoryBuildTargets -PathType Leaf) {
        $sourceInputs += Get-Item -LiteralPath $directoryBuildTargets
    }

    $latestInput = $sourceInputs | Sort-Object LastWriteTime -Descending | Select-Object -First 1

    Write-Host ("Building {0}" -f $Project.ModName)
    $artifactRoot = Join-Path $RepoRoot ("artifacts\mods\{0}" -f $Configuration)

    Remove-LegacyProjectArtifact -Project $Project

    # Do not rely on a prior MSBuild Clean having run. Starting from an empty
    # per-mod artifact directory guarantees removed/renamed localization files
    # cannot survive a rebuild as stale deployable content.
    if (Test-Path -LiteralPath $Project.ArtifactDir -PathType Container) {
        Remove-Item -LiteralPath $Project.ArtifactDir -Recurse -Force
    }

    & dotnet build $Project.ProjectPath -c $Configuration -t:Rebuild ("-p:ModOutputDir={0}" -f $artifactRoot)
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed for $($Project.ProjectPath)"
    }

    $artifactDll = Join-Path $Project.ArtifactDir ($Project.AssemblyName + ".dll")
    if (-not (Test-Path -LiteralPath $artifactDll -PathType Leaf)) {
        throw "Build succeeded but artifact DLL was not produced for '$($Project.ModName)': $artifactDll"
    }

    $artifactDllItem = Get-Item -LiteralPath $artifactDll
    if ($null -ne $latestInput -and $artifactDllItem.LastWriteTime -lt $latestInput.LastWriteTime) {
        throw ("Artifact DLL for '{0}' is older than the latest source input. DLL: {1:yyyy-MM-dd HH:mm:ss}, Source: {2:yyyy-MM-dd HH:mm:ss} ({3})" -f $Project.ModName, $artifactDllItem.LastWriteTime, $latestInput.LastWriteTime, $latestInput.FullName)
    }

}

function Get-ArtifactDeploymentInfo {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Project
    )

    if (-not (Test-Path -LiteralPath $Project.ArtifactDir -PathType Container)) {
        throw "Artifact directory not found for '$($Project.ModName)': $($Project.ArtifactDir)"
    }

    # Validate packaged assets even under -SkipBuild. A skip-build deploy must not
    # silently ship localization or other assets that no longer match the source tree.
    Assert-ArtifactAssetsMatchSource -Project $Project

    $artifactInfoPath = Join-Path $Project.ArtifactDir "info.json"
    $harmonyId = Get-HarmonyIdFromInfoJson -InfoJsonPath $artifactInfoPath
    if ([string]::IsNullOrWhiteSpace($harmonyId)) {
        throw "Artifact info.json is missing HarmonyID for '$($Project.ModName)': $artifactInfoPath"
    }

    $sourceDll = Join-Path $Project.ArtifactDir ($Project.AssemblyName + ".dll")
    if (-not (Test-Path -LiteralPath $sourceDll -PathType Leaf)) {
        throw "Built DLL not found for '$($Project.ModName)': $sourceDll"
    }

    return [PSCustomObject]@{
        Project   = $Project
        HarmonyId = $harmonyId
        SourceDll = $sourceDll
    }
}

function Deploy-Project {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Deployment,

        [Parameter(Mandatory = $true)]
        [hashtable]$InstalledModMap
    )

    $Project = $Deployment.Project
    $harmonyId = $Deployment.HarmonyId
    $sourceDll = $Deployment.SourceDll

    $targetDir = $Deployment.TargetDir
    $targetDll = Join-Path $targetDir ($Project.AssemblyName + ".dll")
    $copiedFiles = 0
    $preservedConfigs = 0

    if (-not (Test-Path -LiteralPath $targetDir -PathType Container)) {
        if ($PSCmdlet.ShouldProcess($targetDir, "Create deploy directory for $($Project.ModName)")) {
            New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
        }
    }

    if ($PSCmdlet.ShouldProcess($targetDll, "Copy mod DLL from $sourceDll")) {
        Copy-Item -LiteralPath $sourceDll -Destination $targetDll -Force
        $copiedFiles++
    }

    foreach ($artifactFile in Get-ChildItem -LiteralPath $Project.ArtifactDir -Recurse -File) {
        if ([string]::Equals($artifactFile.FullName, $sourceDll, [System.StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        if ([string]::Equals($artifactFile.Extension, ".pdb", [System.StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        $relativePath = Get-RelativePath -BasePath $Project.ArtifactDir -ChildPath $artifactFile.FullName
        $targetPath = Join-Path $targetDir $relativePath
        $targetPathParent = Split-Path -Parent $targetPath

        if ((Test-IsUserConfigFile -Path $artifactFile.FullName) -and
            -not $OverwriteConfig -and
            (Test-Path -LiteralPath $targetPath -PathType Leaf)) {
            Write-Host ("Preserving existing config for {0}: {1}" -f $Project.ModName, $targetPath)
            $preservedConfigs++
            continue
        }

        if (-not (Test-Path -LiteralPath $targetPathParent -PathType Container)) {
            if ($PSCmdlet.ShouldProcess($targetPathParent, "Create directory for $relativePath")) {
                New-Item -ItemType Directory -Path $targetPathParent -Force | Out-Null
            }
        }

        if ($PSCmdlet.ShouldProcess($targetPath, "Update from $($artifactFile.FullName)")) {
            Copy-Item -LiteralPath $artifactFile.FullName -Destination $targetPath -Force
            $copiedFiles++
        }
    }

    # Keep this map current during the run. This matters for first-time deploys and
    # also prevents later projects from accidentally treating the same HarmonyID as new.
    if (-not $WhatIfPreference) {
        $InstalledModMap[$harmonyId] = $targetDir
    }

    $statusLabel = if ($WhatIfPreference) { "Prepared deploy" } else { "Deployed" }
    $configSuffix = if ($preservedConfigs -gt 0) {
        ", $preservedConfigs config preserved"
    }
    else {
        ""
    }

    Write-Host ("{0} {1} -> {2} ({3} copied{4})" -f $statusLabel, $Project.ModName, $targetDir, $copiedFiles, $configSuffix)
}

$modsRoot = Join-Path $RepoRoot "mods"
if (-not (Test-Path -LiteralPath $modsRoot -PathType Container)) {
    throw "Mods directory not found: $modsRoot"
}

# A build requires dotnet; a pure -SkipBuild deploy does not.
if (-not $SkipBuild -and -not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet was not found in PATH. Install the .NET SDK, or use -SkipBuild with existing artifacts."
}

$projects = Get-ChildItem -LiteralPath $modsRoot -Recurse -Filter *.csproj |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj|templates)[\\/]' } |
    Sort-Object FullName |
    ForEach-Object { Get-ProjectMetadata -ProjectPath $_.FullName } |
    Where-Object { $null -ne $_ }

if (-not $projects) {
    throw "No deployable mod projects were found under $modsRoot"
}

$projects = @(Resolve-ProjectBuildOrder -Projects @($projects))
Write-Host ("Found {0} deployable mod projects." -f @($projects).Count)

$installedModMap = Build-InstalledModMap -InstalledModsRoot $DeployRoot

if (-not $SkipBuild) {
    foreach ($project in $projects) {
        Build-Project -Project $project
    }
}

# Preflight every artifact before copying anything. This avoids a partial deploy
# caused by a missing/invalid artifact discovered only halfway through the run.
$deployments = @()
$packagedHarmonyIds = @{}

foreach ($project in $projects) {
    $deployment = Get-ArtifactDeploymentInfo -Project $project

    if ($packagedHarmonyIds.ContainsKey($deployment.HarmonyId)) {
        $otherProject = $packagedHarmonyIds[$deployment.HarmonyId]
        throw "Duplicate packaged HarmonyID '$($deployment.HarmonyId)' found in '$($otherProject.ModName)' and '$($project.ModName)'."
    }

    $packagedHarmonyIds[$deployment.HarmonyId] = $project
    $deployments += $deployment
}

# Resolve and validate every target before the first copy. This catches folder-name
# collisions up front rather than leaving a half-updated live Mods directory.
$resolvedDeployments = @()
$targetPathMap = @{}

foreach ($deployment in $deployments) {
    $targetDir = Resolve-DeployTargetDir `
        -Project $deployment.Project `
        -HarmonyId $deployment.HarmonyId `
        -InstalledModMap $installedModMap `
        -InstalledModsRoot $DeployRoot

    if ($targetPathMap.ContainsKey($targetDir)) {
        $otherDeployment = $targetPathMap[$targetDir]
        throw "Multiple packaged mods resolve to the same deploy directory '$targetDir': '$($otherDeployment.Project.ModName)' and '$($deployment.Project.ModName)'."
    }

    $targetPathMap[$targetDir] = $deployment
    $resolvedDeployments += [PSCustomObject]@{
        Project   = $deployment.Project
        HarmonyId = $deployment.HarmonyId
        SourceDll = $deployment.SourceDll
        TargetDir = $targetDir
    }
}

foreach ($deployment in $resolvedDeployments) {
    Deploy-Project -Deployment $deployment -InstalledModMap $installedModMap
}
