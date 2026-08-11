param(
    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
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

function Assert-False {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if ($Condition) {
        throw $Message
    }
}

function Assert-PathEqual {
    param(
        [string]$Expected,
        [string]$Actual,
        [string]$Message
    )

    $normalizedExpected = [System.IO.Path]::GetFullPath($Expected)
    $normalizedActual = [System.IO.Path]::GetFullPath($Actual)
    if (-not [string]::Equals(
            $normalizedExpected,
            $normalizedActual,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw ($Message + "`nExpected: $normalizedExpected`nActual:   $normalizedActual")
    }
}

function Get-MethodByArity {
    param(
        [Type]$Type,
        [string]$Name,
        [int]$ParameterCount,
        [System.Reflection.BindingFlags]$Flags
    )

    $matches = @(
        $Type.GetMethods($Flags) |
            Where-Object {
                $_.Name -eq $Name -and
                $_.GetParameters().Count -eq $ParameterCount
            }
    )
    if ($matches.Count -ne 1) {
        throw "Expected one $Name/$ParameterCount method on $($Type.FullName); found $($matches.Count)."
    }

    return $matches[0]
}

function Invoke-CoreMethod {
    param(
        [Type]$CorePathsType,
        [string]$Name,
        [object[]]$Arguments
    )

    $flags = [System.Reflection.BindingFlags]'Static,NonPublic'
    $method = Get-MethodByArity `
        $CorePathsType `
        $Name `
        $Arguments.Count `
        $flags
    $invokeArguments = [object[]]::new($Arguments.Count)
    for ($index = 0; $index -lt $Arguments.Count; $index++) {
        $candidate = $Arguments[$index]
        if ($candidate -is [System.Management.Automation.PSObject]) {
            $candidate = $candidate.PSObject.BaseObject
        }

        $invokeArguments[$index] = $candidate
    }

    $returnValue = $method.Invoke($null, $invokeArguments)
    return [pscustomobject]@{
        ReturnValue = $returnValue
        Arguments = $invokeArguments
    }
}

function Get-ScopeField {
    param(
        [object]$Scope,
        [string]$Name
    )

    Assert-True ($null -ne $Scope) "Cannot read $Name from a null save scope."
    $flags = [System.Reflection.BindingFlags]'Instance,Public,NonPublic'
    $field = $Scope.GetType().GetField($Name, $flags)
    Assert-True ($null -ne $field) "Save-scope field $Name is missing."
    return $field.GetValue($Scope)
}

function Invoke-RelativeScopeResolution {
    param(
        [Type]$CorePathsType,
        [string]$PersistentRoot,
        [string]$RelativePath
    )

    $arguments = [object[]]@($PersistentRoot, $RelativePath, $null, $null)
    return Invoke-CoreMethod `
        $CorePathsType `
        'TryResolveVanillaSaveRelativePath' `
        $arguments
}

function Invoke-PhysicalScopeResolution {
    param(
        [Type]$CorePathsType,
        [string]$PersistentRoot,
        [string]$SavePath
    )

    $arguments = [object[]]@($PersistentRoot, $SavePath, $null)
    return Invoke-CoreMethod `
        $CorePathsType `
        'TryCreateSaveScope' `
        $arguments
}

$projectDirectory = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot '..'))
$projectPath = Join-Path $projectDirectory 'IM Data Core.csproj'
$assemblyPath = Join-Path `
    $projectDirectory `
    'bin\Debug\net46\com.cosmo.imdatacore.dll'
$dependencyRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot '..\..\..\..\dll'))

if (-not $SkipBuild) {
    & dotnet build $projectPath --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw 'IM Data Core did not build; CorePaths regression tests were not run.'
    }
}

Assert-True `
    (Test-Path -LiteralPath $assemblyPath -PathType Leaf) `
    'The compiled IM Data Core assembly is missing.'
Assert-True `
    (Test-Path -LiteralPath $dependencyRoot -PathType Container) `
    'The game dependency directory is missing.'

$dependencyResolver = [System.ResolveEventHandler]{
    param($sender, $eventArgs)

    $simpleName = (New-Object System.Reflection.AssemblyName($eventArgs.Name)).Name
    $candidatePath = Join-Path $dependencyRoot ($simpleName + '.dll')
    if (Test-Path -LiteralPath $candidatePath -PathType Leaf) {
        return [System.Reflection.Assembly]::LoadFrom($candidatePath)
    }

    return $null
}.GetNewClosure()

[System.AppDomain]::CurrentDomain.add_AssemblyResolve($dependencyResolver)
$testRoot = Join-Path `
    ([System.IO.Path]::GetTempPath()) `
    ('imdatacore-corepaths-' + [Guid]::NewGuid().ToString('N'))

try {
    [System.IO.Directory]::CreateDirectory($testRoot) | Out-Null
    $dataRoot = Join-Path $testRoot 'data'
    $expectedImdcRoot = Join-Path $testRoot 'IMDataCore'
    [System.IO.Directory]::CreateDirectory($dataRoot) | Out-Null

    # These sentinels prove that CorePaths mutation helpers cannot touch vanilla.
    $globalDataPath = Join-Path $dataRoot 'global_data.json'
    $vanillaAutoSavePath = Join-Path $dataRoot 'auto_save.json'
    $globalSentinel = '{"owner":"vanilla-global"}'
    $autoSaveSentinel = '{"owner":"vanilla-save"}'
    [System.IO.File]::WriteAllText($globalDataPath, $globalSentinel)
    [System.IO.File]::WriteAllText($vanillaAutoSavePath, $autoSaveSentinel)

    $assembly = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
    $corePathsType = $assembly.GetType('IMDataCore.CorePaths', $true)

    $rootCall = Invoke-CoreMethod `
        $corePathsType `
        'GetRootDirectory' `
        ([object[]]@($testRoot))
    Assert-PathEqual `
        $expectedImdcRoot `
        ([string]$rootCall.ReturnValue) `
        'CorePaths did not resolve the IMDC root as the sibling of data.'

    $acceptedMappings = @(
        [pscustomobject]@{
            Label = 'freeplay direct autosave'
            RelativePath = 'auto_save.json'
        },
        [pscustomobject]@{
            Label = 'freeplay direct manual save'
            RelativePath = 'manual_save.json'
        },
        [pscustomobject]@{
            Label = 'freeplay opaque manual 1'
            RelativePath = 'manual_saves\1c5ec635\save.json'
        },
        [pscustomobject]@{
            Label = 'freeplay opaque manual 2'
            RelativePath = 'manual_saves\4a239d8b\save.json'
        },
        [pscustomobject]@{
            Label = 'freeplay opaque manual 3'
            RelativePath = 'manual_saves\bb9be439\save.json'
        },
        [pscustomobject]@{
            Label = 'freeplay non-semantic opaque ID'
            RelativePath = 'manual_saves\opaque.slot-A_17\save.json'
        },
        [pscustomobject]@{
            Label = 'story autosave'
            RelativePath = 'story_mode\playthrough.A-01\auto_save.json'
        },
        [pscustomobject]@{
            Label = 'story direct manual save'
            RelativePath = 'story_mode\playthrough.A-01\manual_save.json'
        },
        [pscustomobject]@{
            Label = 'story opaque manual save'
            RelativePath = 'story_mode\playthrough.A-01\manual_saves\story-slot_B.42\save.json'
        },
        [pscustomobject]@{
            Label = 'story first chapter save'
            RelativePath = 'story_mode\playthrough.A-01\chapter_0\save.json'
        },
        [pscustomobject]@{
            Label = 'story middle chapter save'
            RelativePath = 'story_mode\playthrough.A-01\chapter_3\save.json'
        },
        [pscustomobject]@{
            Label = 'story last chapter save'
            RelativePath = 'story_mode\second-playthrough\chapter_6\save.json'
        }
    )

    $resolvedScopes = @{}
    foreach ($mapping in $acceptedMappings) {
        $call = Invoke-RelativeScopeResolution `
            $corePathsType `
            $testRoot `
            $mapping.RelativePath
        Assert-True `
            ([bool]$call.ReturnValue) `
            ("Rejected supported mapping: " + $mapping.Label)

        $savePath = [string]$call.Arguments[2]
        $scope = $call.Arguments[3]
        $scopeRelativePath = [string](Get-ScopeField $scope 'RelativeSavePath')
        $scopeSavePath = [string](Get-ScopeField $scope 'SaveFilePath')
        $sidecarPath = [string](Get-ScopeField $scope 'SidecarFilePath')
        $isTransient = [bool](Get-ScopeField $scope 'IsTransient')
        $expectedSavePath = Join-Path $dataRoot $mapping.RelativePath
        $expectedSidecarPath = Join-Path $expectedImdcRoot $mapping.RelativePath

        Assert-False $isTransient ("Physical mapping became transient: " + $mapping.Label)
        Assert-True `
            ($scopeRelativePath -ceq $mapping.RelativePath) `
            ("Relative path segments were not preserved exactly: " + $mapping.Label)
        Assert-PathEqual `
            $expectedSavePath `
            $savePath `
            ("Resolved vanilla path mismatch: " + $mapping.Label)
        Assert-PathEqual `
            $expectedSavePath `
            $scopeSavePath `
            ("Scope vanilla path mismatch: " + $mapping.Label)
        Assert-PathEqual `
            $expectedSidecarPath `
            $sidecarPath `
            ("Mirrored sidecar mismatch: " + $mapping.Label)

        $forbiddenSavesPrefix = Join-Path $expectedImdcRoot 'saves'
        $forbiddenSavesPrefix += [System.IO.Path]::DirectorySeparatorChar
        Assert-False `
            ($sidecarPath.StartsWith(
                $forbiddenSavesPrefix,
                [System.StringComparison]::OrdinalIgnoreCase)) `
            ("A legacy IMDataCore/saves layer was inserted: " + $mapping.Label)

        $resolvedScopes[$mapping.Label] = $scope
    }

    # Exercise DataSaver's filename/extension reconstruction overload too.
    $dataSaverCases = @(
        [pscustomobject]@{
            Label = 'extensionless freeplay autosave'
            FileName = 'auto_save'
            IsJson = $true
            FullPath = $false
            Expected = (Join-Path $dataRoot 'auto_save.json')
        },
        [pscustomobject]@{
            Label = 'direct freeplay manual save'
            FileName = 'manual_save'
            IsJson = $true
            FullPath = $false
            Expected = (Join-Path $dataRoot 'manual_save.json')
        },
        [pscustomobject]@{
            Label = 'opaque freeplay manual save'
            FileName = 'manual_saves\4a239d8b\save'
            IsJson = $true
            FullPath = $false
            Expected = (Join-Path $dataRoot 'manual_saves\4a239d8b\save.json')
        },
        [pscustomobject]@{
            Label = 'full-path story autosave'
            FileName = (Join-Path $dataRoot 'story_mode\playthrough.A-01\auto_save.json')
            IsJson = $true
            FullPath = $true
            Expected = (Join-Path $dataRoot 'story_mode\playthrough.A-01\auto_save.json')
        },
        [pscustomobject]@{
            Label = 'rooted chapter save with fullPath false'
            FileName = (Join-Path $dataRoot 'story_mode\playthrough.A-01\chapter_3\save')
            IsJson = $true
            FullPath = $false
            Expected = (Join-Path $dataRoot 'story_mode\playthrough.A-01\chapter_3\save.json')
        }
    )

    foreach ($dataSaverCase in $dataSaverCases) {
        $arguments = [object[]]@(
            $testRoot,
            $dataSaverCase.FileName,
            $dataSaverCase.IsJson,
            $dataSaverCase.FullPath,
            $null)
        $call = Invoke-CoreMethod `
            $corePathsType `
            'TryResolveDataSaverPath' `
            $arguments
        Assert-True `
            ([bool]$call.ReturnValue) `
            ("DataSaver resolution failed: " + $dataSaverCase.Label)
        Assert-PathEqual `
            $dataSaverCase.Expected `
            ([string]$call.Arguments[4]) `
            ("DataSaver resolved the wrong path: " + $dataSaverCase.Label)
    }

    # DataSaver.loadData always appends .json, then collapses the literal
    # .json.json token. Cover relative menu/manual paths and rooted autosave paths
    # because Path.Combine intentionally preserves a rooted second argument.
    $dataSaverLoadCases = @(
        [pscustomobject]@{
            Label = 'relative direct manual without extension'
            FileName = 'manual_save'
            Expected = (Join-Path $dataRoot 'manual_save.json')
        },
        [pscustomobject]@{
            Label = 'relative direct manual with extension'
            FileName = 'manual_save.json'
            Expected = (Join-Path $dataRoot 'manual_save.json')
        },
        [pscustomobject]@{
            Label = 'relative story manual without extension'
            FileName = 'story_mode\playthrough.A-01\manual_saves\story-slot_B.42\save'
            Expected = (Join-Path $dataRoot 'story_mode\playthrough.A-01\manual_saves\story-slot_B.42\save.json')
        },
        [pscustomobject]@{
            Label = 'relative story manual with extension'
            FileName = 'story_mode\playthrough.A-01\manual_saves\story-slot_B.42\save.json'
            Expected = (Join-Path $dataRoot 'story_mode\playthrough.A-01\manual_saves\story-slot_B.42\save.json')
        },
        [pscustomobject]@{
            Label = 'absolute freeplay autosave without extension'
            FileName = (Join-Path $dataRoot 'auto_save')
            Expected = (Join-Path $dataRoot 'auto_save.json')
        },
        [pscustomobject]@{
            Label = 'absolute freeplay autosave with extension'
            FileName = (Join-Path $dataRoot 'auto_save.json')
            Expected = (Join-Path $dataRoot 'auto_save.json')
        },
        [pscustomobject]@{
            Label = 'absolute story autosave with extension'
            FileName = (Join-Path $dataRoot 'story_mode\playthrough.A-01\auto_save.json')
            Expected = (Join-Path $dataRoot 'story_mode\playthrough.A-01\auto_save.json')
        }
    )

    foreach ($dataSaverLoadCase in $dataSaverLoadCases) {
        $arguments = [object[]]@(
            $testRoot,
            $dataSaverLoadCase.FileName,
            $null)
        $call = Invoke-CoreMethod `
            $corePathsType `
            'TryResolveDataSaverLoadPath' `
            $arguments
        Assert-True `
            ([bool]$call.ReturnValue) `
            ("DataSaver load resolution failed: " + $dataSaverLoadCase.Label)
        Assert-PathEqual `
            $dataSaverLoadCase.Expected `
            ([string]$call.Arguments[2]) `
            ("DataSaver load resolved the wrong path: " + $dataSaverLoadCase.Label)
    }

    $rejectedRelativePaths = @(
        'global_data.json',
        'GLOBAL_DATA.JSON',
        'save.json',
        'manual_saves\1c5ec635\global_data.json',
        'story_mode\playthrough.A-01\global_data.json',
        'story_mode\playthrough.A-01\chapter_7\save.json',
        'story_mode\playthrough.A-01\chapter_-1\save.json',
        'manual_saves\..\auto_save.json',
        'story_mode\playthrough.A-01\chapter_3\..\auto_save.json',
        '..\data\auto_save.json',
        '..\IMDataCore\auto_save.json',
        (Join-Path $testRoot 'outside\auto_save.json')
    )

    foreach ($relativePath in $rejectedRelativePaths) {
        $call = Invoke-RelativeScopeResolution `
            $corePathsType `
            $testRoot `
            $relativePath
        Assert-False `
            ([bool]$call.ReturnValue) `
            ("Unsafe or unsupported relative path was accepted: " + $relativePath)
        Assert-True `
            ([string]::IsNullOrEmpty([string]$call.Arguments[2])) `
            ("Rejected relative path returned a vanilla path: " + $relativePath)
        Assert-True `
            ($null -eq $call.Arguments[3]) `
            ("Rejected relative path returned a scope: " + $relativePath)
    }

    $rejectedPhysicalPaths = @(
        $globalDataPath,
        (Join-Path $testRoot 'outside\auto_save.json'),
        (Join-Path $testRoot 'data-shadow\auto_save.json'),
        (Join-Path $expectedImdcRoot 'auto_save.json'),
        (Join-Path $dataRoot 'manual_saves\1c5ec635\..\..\global_data.json'),
        (Join-Path $dataRoot 'manual_saves\1c5ec635\..\..\..\outside\auto_save.json'),
        (Join-Path $dataRoot 'arbitrary\save.json')
    )

    foreach ($physicalPath in $rejectedPhysicalPaths) {
        $call = Invoke-PhysicalScopeResolution `
            $corePathsType `
            $testRoot `
            $physicalPath
        Assert-False `
            ([bool]$call.ReturnValue) `
            ("Unsafe or unsupported physical path was accepted: " + $physicalPath)
        Assert-True `
            ($null -eq $call.Arguments[2]) `
            ("Rejected physical path returned a scope: " + $physicalPath)
    }

    foreach ($dataSaverGlobalCase in @('global_data', 'global_data.json')) {
        $arguments = [object[]]@(
            $testRoot,
            $dataSaverGlobalCase,
            $true,
            $false,
            $null)
        $call = Invoke-CoreMethod `
            $corePathsType `
            'TryResolveDataSaverPath' `
            $arguments
        Assert-False `
            ([bool]$call.ReturnValue) `
            ("DataSaver resolution accepted protected $dataSaverGlobalCase.")
    }

    foreach ($dataSaverLoadRejectedCase in @(
            'global_data',
            'global_data.json',
            $globalDataPath,
            (Join-Path $testRoot 'outside\auto_save.json'))) {
        $arguments = [object[]]@(
            $testRoot,
            $dataSaverLoadRejectedCase,
            $null)
        $call = Invoke-CoreMethod `
            $corePathsType `
            'TryResolveDataSaverLoadPath' `
            $arguments
        Assert-False `
            ([bool]$call.ReturnValue) `
            ("DataSaver load resolution accepted a protected or outside path: " +
                $dataSaverLoadRejectedCase)
    }

    # Validate non-mutating containment checks before exercising the mutators.
    $allowedMutationPath = Join-Path `
        $expectedImdcRoot `
        'story_mode\playthrough.A-01\auto_save.json'
    $rejectedMutationPaths = @(
        $expectedImdcRoot,
        $globalDataPath,
        $vanillaAutoSavePath,
        (Join-Path $testRoot 'outside\owned.json'),
        (Join-Path $testRoot 'IMDataCore-shadow\owned.json'),
        (Join-Path $expectedImdcRoot '..\data\global_data.json')
    )

    $arguments = [object[]]@(
        $testRoot,
        $allowedMutationPath,
        $false,
        $null,
        $null)
    $call = Invoke-CoreMethod `
        $corePathsType `
        'TryValidateContainedMutationPath' `
        $arguments
    Assert-True `
        ([bool]$call.ReturnValue) `
        'A valid nested IMDC mutation path was rejected.'
    Assert-PathEqual `
        $allowedMutationPath `
        ([string]$call.Arguments[3]) `
        'The validated mutation path changed unexpectedly.'

    foreach ($mutationPath in $rejectedMutationPaths) {
        $arguments = [object[]]@(
            $testRoot,
            $mutationPath,
            $false,
            $null,
            $null)
        $call = Invoke-CoreMethod `
            $corePathsType `
            'TryValidateContainedMutationPath' `
            $arguments
        Assert-False `
            ([bool]$call.ReturnValue) `
            ("An unsafe mutation path was accepted: " + $mutationPath)
        Assert-True `
            ([string]::IsNullOrEmpty([string]$call.Arguments[3])) `
            ("An unsafe mutation returned a normalized target: " + $mutationPath)
    }

    $ensureRootArguments = [object[]]@($testRoot, $null, $null)
    $ensureRootCall = Invoke-CoreMethod `
        $corePathsType `
        'TryEnsureRootDirectory' `
        $ensureRootArguments
    Assert-True `
        ([bool]$ensureRootCall.ReturnValue) `
        ('Could not create the IMDC root: ' + [string]$ensureRootCall.Arguments[2])
    Assert-PathEqual `
        $expectedImdcRoot `
        ([string]$ensureRootCall.Arguments[1]) `
        'The root-creation helper created the wrong directory.'
    Assert-True `
        (Test-Path -LiteralPath $expectedImdcRoot -PathType Container) `
        'The IMDC root was not created.'

    $storyScope = $resolvedScopes['story opaque manual save']
    $ensureParentArguments = [object[]]@($testRoot, $storyScope, $null)
    $ensureParentCall = Invoke-CoreMethod `
        $corePathsType `
        'TryEnsureSidecarParentDirectory' `
        $ensureParentArguments
    Assert-True `
        ([bool]$ensureParentCall.ReturnValue) `
        ('Could not create a mirrored sidecar parent: ' +
            [string]$ensureParentCall.Arguments[2])
    $storySidecar = [string](Get-ScopeField $storyScope 'SidecarFilePath')
    Assert-True `
        (Test-Path -LiteralPath ([System.IO.Path]::GetDirectoryName($storySidecar)) -PathType Container) `
        'The exact mirrored sidecar parent was not created.'
    Assert-False `
        (Test-Path -LiteralPath $storySidecar) `
        'Creating a sidecar parent unexpectedly created the sidecar file.'
    Assert-False `
        (Test-Path -LiteralPath (Join-Path $expectedImdcRoot 'saves')) `
        'A legacy IMDataCore/saves directory was created.'

    $ownedDirectory = Join-Path $expectedImdcRoot 'mutation-test\nested'
    $createDirectoryArguments = [object[]]@(
        $testRoot,
        $ownedDirectory,
        $null,
        $null)
    $createDirectoryCall = Invoke-CoreMethod `
        $corePathsType `
        'TryCreateContainedDirectory' `
        $createDirectoryArguments
    Assert-True `
        ([bool]$createDirectoryCall.ReturnValue) `
        ('Could not create an IMDC-owned directory: ' +
            [string]$createDirectoryCall.Arguments[3])
    Assert-PathEqual `
        $ownedDirectory `
        ([string]$createDirectoryCall.Arguments[2]) `
        'The contained-directory helper created the wrong path.'

    $outsideCreatePath = Join-Path $testRoot 'outside-created-by-imdc'
    foreach ($unsafeDirectory in @($dataRoot, $outsideCreatePath)) {
        $arguments = [object[]]@(
            $testRoot,
            $unsafeDirectory,
            $null,
            $null)
        $call = Invoke-CoreMethod `
            $corePathsType `
            'TryCreateContainedDirectory' `
            $arguments
        Assert-False `
            ([bool]$call.ReturnValue) `
            ("Directory creation accepted an unsafe target: " + $unsafeDirectory)
    }
    Assert-False `
        (Test-Path -LiteralPath $outsideCreatePath) `
        'Rejected directory creation still created an outside directory.'

    $ownedFile = Join-Path $ownedDirectory 'owned.tmp'
    [System.IO.File]::WriteAllText($ownedFile, 'IMDC-owned')
    $deleteFileArguments = [object[]]@($testRoot, $ownedFile, $null)
    $deleteFileCall = Invoke-CoreMethod `
        $corePathsType `
        'TryDeleteContainedFile' `
        $deleteFileArguments
    Assert-True `
        ([bool]$deleteFileCall.ReturnValue) `
        ('Could not delete an IMDC-owned file: ' +
            [string]$deleteFileCall.Arguments[2])
    Assert-False `
        (Test-Path -LiteralPath $ownedFile) `
        'The IMDC-owned file was not deleted.'

    foreach ($vanillaFile in @($globalDataPath, $vanillaAutoSavePath)) {
        $arguments = [object[]]@($testRoot, $vanillaFile, $null)
        $call = Invoke-CoreMethod `
            $corePathsType `
            'TryDeleteContainedFile' `
            $arguments
        Assert-False `
            ([bool]$call.ReturnValue) `
            ("The contained-file API accepted vanilla file: " + $vanillaFile)
        Assert-True `
            (Test-Path -LiteralPath $vanillaFile -PathType Leaf) `
            ("The contained-file API deleted vanilla file: " + $vanillaFile)
    }

    $deleteDirectoryArguments = [object[]]@(
        $testRoot,
        (Join-Path $expectedImdcRoot 'mutation-test'),
        $true,
        $null)
    $deleteDirectoryCall = Invoke-CoreMethod `
        $corePathsType `
        'TryDeleteContainedDirectory' `
        $deleteDirectoryArguments
    Assert-True `
        ([bool]$deleteDirectoryCall.ReturnValue) `
        ('Could not delete an IMDC-owned directory: ' +
            [string]$deleteDirectoryCall.Arguments[3])
    Assert-False `
        (Test-Path -LiteralPath (Join-Path $expectedImdcRoot 'mutation-test')) `
        'The IMDC-owned directory was not deleted.'

    foreach ($unsafeDirectory in @($expectedImdcRoot, $dataRoot)) {
        $arguments = [object[]]@($testRoot, $unsafeDirectory, $true, $null)
        $call = Invoke-CoreMethod `
            $corePathsType `
            'TryDeleteContainedDirectory' `
            $arguments
        Assert-False `
            ([bool]$call.ReturnValue) `
            ("Directory deletion accepted protected root: " + $unsafeDirectory)
    }

    Assert-True `
        ([System.IO.File]::ReadAllText($globalDataPath) -ceq $globalSentinel) `
        'global_data.json changed during CorePaths mutation tests.'
    Assert-True `
        ([System.IO.File]::ReadAllText($vanillaAutoSavePath) -ceq $autoSaveSentinel) `
        'The vanilla save sentinel changed during CorePaths mutation tests.'
    Assert-True `
        (@([System.IO.Directory]::GetDirectories(
            $dataRoot,
            '*',
            [System.IO.SearchOption]::AllDirectories)).Count -eq 0) `
        'CorePaths unexpectedly created a directory beneath vanilla data.'
    Assert-True `
        (@([System.IO.Directory]::GetFiles(
            $dataRoot,
            '*',
            [System.IO.SearchOption]::AllDirectories)).Count -eq 2) `
        'CorePaths unexpectedly created or removed a file beneath vanilla data.'

    Write-Host (
        'CorePaths regression tests passed: ' +
        $acceptedMappings.Count +
        ' exact mappings, DataSaver save/load reconstruction, ' +
        'traversal/global rejection, and contained mutations.')
}
finally {
    [System.AppDomain]::CurrentDomain.remove_AssemblyResolve($dependencyResolver)

    if (Test-Path -LiteralPath $testRoot) {
        $normalizedTestRoot = [System.IO.Path]::GetFullPath($testRoot)
        $normalizedTempRoot = [System.IO.Path]::GetFullPath(
            [System.IO.Path]::GetTempPath()).TrimEnd(
                [System.IO.Path]::DirectorySeparatorChar,
                [System.IO.Path]::AltDirectorySeparatorChar)
        $tempPrefix = $normalizedTempRoot + [System.IO.Path]::DirectorySeparatorChar
        $testLeaf = [System.IO.Path]::GetFileName($normalizedTestRoot)
        Assert-True `
            ($normalizedTestRoot.StartsWith(
                $tempPrefix,
                [System.StringComparison]::OrdinalIgnoreCase) -and
                $testLeaf.StartsWith(
                    'imdatacore-corepaths-',
                    [System.StringComparison]::Ordinal)) `
            'Refused to clean a CorePaths test directory outside the expected temp scope.'
        Remove-Item -LiteralPath $normalizedTestRoot -Recurse -Force
    }
}
