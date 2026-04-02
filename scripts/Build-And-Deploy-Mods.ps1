[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [string]$RepoRoot = "",

    [string]$DeployRoot = (Join-Path $env:USERPROFILE "AppData\LocalLow\Glitch Pitch\Idol Manager\Mods"),

    [switch]$SkipBuild
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

function Get-ProjectMetadata {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    [xml]$projectXml = Get-Content -Path $ProjectPath -Raw
    $propertyGroups = @($projectXml.Project.PropertyGroup)
    $modName = $null
    $assemblyName = $null
    $deployFolderName = $null

    foreach ($propertyGroup in $propertyGroups) {
        if (-not $modName -and $propertyGroup.ModName) {
            $modName = $propertyGroup.ModName.Trim()
        }

        if (-not $assemblyName -and $propertyGroup.AssemblyName) {
            $assemblyName = $propertyGroup.AssemblyName.Trim()
        }

        if (-not $deployFolderName -and $propertyGroup.DeployFolderName) {
            $deployFolderName = $propertyGroup.DeployFolderName.Trim()
        }
    }

    if ([string]::IsNullOrWhiteSpace($modName) -or [string]::IsNullOrWhiteSpace($assemblyName)) {
        return $null
    }

    if ([string]::IsNullOrWhiteSpace($deployFolderName)) {
        $deployFolderName = $null
    }

    return [PSCustomObject]@{
        ProjectPath   = $ProjectPath
        ProjectDir    = Split-Path -Parent $ProjectPath
        ModName       = $modName
        AssemblyName  = $assemblyName
        DeployFolderName = $deployFolderName
        ArtifactDir   = Join-Path $RepoRoot ("artifacts\mods\{0}\{1}" -f $Configuration, $modName)
    }
}

function Get-HarmonyIdFromInfoJson {
    param(
        [Parameter(Mandatory = $true)]
        [string]$InfoJsonPath
    )

    if (-not (Test-Path -LiteralPath $InfoJsonPath -PathType Leaf)) {
        return $null
    }

    $info = Get-Content -LiteralPath $InfoJsonPath -Raw | ConvertFrom-Json
    if ($null -eq $info -or [string]::IsNullOrWhiteSpace($info.HarmonyID)) {
        return $null
    }

    return $info.HarmonyID.Trim()
}

function Get-RelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BasePath,

        [Parameter(Mandatory = $true)]
        [string]$ChildPath
    )

    $baseUri = New-Object System.Uri((Resolve-Path -LiteralPath $BasePath).Path.TrimEnd('\') + '\')
    $childUri = New-Object System.Uri((Resolve-Path -LiteralPath $ChildPath).Path)
    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($childUri).ToString()).Replace('/', '\')
}

function Build-InstalledModMap {
    param(
        [Parameter(Mandatory = $true)]
        [string]$InstalledModsRoot
    )

    $map = @{}
    if (-not (Test-Path -LiteralPath $InstalledModsRoot -PathType Container)) {
        throw "Installed mods directory not found: $InstalledModsRoot"
    }

    Get-ChildItem -LiteralPath $InstalledModsRoot -Directory | ForEach-Object {
        $infoJsonPath = Join-Path $_.FullName "info.json"
        $harmonyId = Get-HarmonyIdFromInfoJson -InfoJsonPath $infoJsonPath
        if ([string]::IsNullOrWhiteSpace($harmonyId)) {
            return
        }

        if ($map.ContainsKey($harmonyId)) {
            throw "Duplicate deployed HarmonyID '$harmonyId' found in '$($_.FullName)' and '$($map[$harmonyId])'."
        }

        $map[$harmonyId] = $_.FullName
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

    if (-not [string]::IsNullOrWhiteSpace($Project.DeployFolderName)) {
        return Join-Path $InstalledModsRoot $Project.DeployFolderName
    }

    return $null
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
        $sourceInputs += Get-ChildItem -LiteralPath $srcDir -Recurse -File | Where-Object { $_.Extension -eq ".cs" }
    }

    $latestInput = $sourceInputs | Sort-Object LastWriteTime -Descending | Select-Object -First 1

    Write-Host ("Building {0}" -f $Project.ModName)
    & dotnet build $Project.ProjectPath -c $Configuration -t:Rebuild
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

function Deploy-Project {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Project,

        [Parameter(Mandatory = $true)]
        [hashtable]$InstalledModMap
    )

    if (-not (Test-Path -LiteralPath $Project.ArtifactDir -PathType Container)) {
        throw "Artifact directory not found for '$($Project.ModName)': $($Project.ArtifactDir)"
    }

    $artifactInfoPath = Join-Path $Project.ArtifactDir "info.json"
    $harmonyId = Get-HarmonyIdFromInfoJson -InfoJsonPath $artifactInfoPath
    if ([string]::IsNullOrWhiteSpace($harmonyId)) {
        throw "Artifact info.json is missing HarmonyID for '$($Project.ModName)': $artifactInfoPath"
    }

    $targetDir = Resolve-DeployTargetDir -Project $Project -HarmonyId $harmonyId -InstalledModMap $InstalledModMap -InstalledModsRoot $DeployRoot
    if ([string]::IsNullOrWhiteSpace($targetDir)) {
        Write-Warning ("Skipping deploy for '{0}' because no installed mod folder with HarmonyID '{1}' was found under '{2}', and the project does not define DeployFolderName." -f $Project.ModName, $harmonyId, $DeployRoot)
        return
    }

    $sourceDll = Join-Path $Project.ArtifactDir ($Project.AssemblyName + ".dll")
    $targetDll = Join-Path $targetDir ($Project.AssemblyName + ".dll")

    if (-not (Test-Path -LiteralPath $sourceDll -PathType Leaf)) {
        throw "Built DLL not found for '$($Project.ModName)': $sourceDll"
    }

    $copiedFiles = 0

    if (-not (Test-Path -LiteralPath $targetDir -PathType Container)) {
        if ($PSCmdlet.ShouldProcess($targetDir, "Create deploy directory for $($Project.ModName)")) {
            New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
        }
    }

    if ($PSCmdlet.ShouldProcess($targetDll, "Copy mod DLL from $sourceDll")) {
        Copy-Item -LiteralPath $sourceDll -Destination $targetDll -Force
        $copiedFiles++
    }

    Get-ChildItem -LiteralPath $Project.ArtifactDir -Recurse -File | ForEach-Object {
        if ($_.FullName -eq $sourceDll) {
            return
        }

        if ([string]::Equals($_.Extension, ".pdb", [System.StringComparison]::OrdinalIgnoreCase)) {
            return
        }

        $relativePath = Get-RelativePath -BasePath $Project.ArtifactDir -ChildPath $_.FullName
        $targetPath = Join-Path $targetDir $relativePath
        $targetPathParent = Split-Path -Parent $targetPath

        if (-not (Test-Path -LiteralPath $targetPathParent -PathType Container)) {
            if ($PSCmdlet.ShouldProcess($targetPathParent, "Create directory for $relativePath")) {
                New-Item -ItemType Directory -Path $targetPathParent -Force | Out-Null
            }
        }

        if ($PSCmdlet.ShouldProcess($targetPath, "Update from $($_.FullName)")) {
            Copy-Item -LiteralPath $_.FullName -Destination $targetPath -Force
            $copiedFiles++
        }
    }

    $statusLabel = if ($WhatIfPreference) { "Prepared deploy" } else { "Deployed" }
    Write-Host ("{0} {1} -> {2} ({3} copied)" -f $statusLabel, $Project.ModName, $targetDir, $copiedFiles)
}

$modsRoot = Join-Path $RepoRoot "mods"
if (-not (Test-Path -LiteralPath $modsRoot -PathType Container)) {
    throw "Mods directory not found: $modsRoot"
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet was not found in PATH."
}

$projects = Get-ChildItem -LiteralPath $modsRoot -Recurse -Filter *.csproj |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj|templates)[\\/]' } |
    Sort-Object FullName |
    ForEach-Object { Get-ProjectMetadata -ProjectPath $_.FullName } |
    Where-Object { $null -ne $_ }

if (-not $projects) {
    throw "No deployable mod projects were found under $modsRoot"
}

$installedModMap = Build-InstalledModMap -InstalledModsRoot $DeployRoot

if (-not $SkipBuild) {
    foreach ($project in $projects) {
        Build-Project -Project $project
    }
}

foreach ($project in $projects) {
    Deploy-Project -Project $project -InstalledModMap $installedModMap
}
