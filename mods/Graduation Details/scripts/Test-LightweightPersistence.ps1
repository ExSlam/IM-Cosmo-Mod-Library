[CmdletBinding()]
param(
    [switch]$SkipBuild
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

function Assert-False {
    param(
        [bool]$Condition,
        [string]$Message
    )
    Assert-True (-not $Condition) $Message
}

function Assert-Equal {
    param(
        $Expected,
        $Actual,
        [string]$Message
    )
    if ($Expected -cne $Actual) {
        throw ("{0} Expected: [{1}] Actual: [{2}]" -f `
            $Message, $Expected, $Actual)
    }
}

function Assert-PathEqual {
    param(
        [string]$Expected,
        [string]$Actual,
        [string]$Message
    )
    $expectedPath = [System.IO.Path]::GetFullPath($Expected)
    $actualPath = [System.IO.Path]::GetFullPath($Actual)
    if (-not [string]::Equals(
            $expectedPath,
            $actualPath,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw ("{0} Expected: [{1}] Actual: [{2}]" -f `
            $Message, $expectedPath, $actualPath)
    }
}

function Assert-SequenceEqual {
    param(
        [object[]]$Expected,
        [object[]]$Actual,
        [string]$Message
    )
    Assert-Equal $Expected.Count $Actual.Count `
        ($Message + ' Sequence lengths differ.')
    for ($index = 0; $index -lt $Expected.Count; $index++) {
        Assert-Equal $Expected[$index] $Actual[$index] `
            ($Message + " Difference at index $index.")
    }
}

function Get-ReflectionFlags {
    return [System.Reflection.BindingFlags]::Public -bor `
        [System.Reflection.BindingFlags]::NonPublic -bor `
        [System.Reflection.BindingFlags]::Instance -bor `
        [System.Reflection.BindingFlags]::Static
}

function Get-ReflectedMethod {
    param(
        [Type]$Type,
        [string]$Name,
        [int]$ParameterCount,
        [bool]$Static
    )
    $methods = @(
        $Type.GetMethods((Get-ReflectionFlags)) |
            Where-Object {
                $_.Name -ceq $Name -and
                $_.GetParameters().Count -eq $ParameterCount -and
                $_.IsStatic -eq $Static
            })
    Assert-Equal 1 $methods.Count `
        ("Expected one {0}-parameter method {1}.{2}." -f `
            $ParameterCount, $Type.FullName, $Name)
    return $methods[0]
}

function Invoke-ReflectedMethod {
    param(
        [object]$Instance,
        [Type]$Type,
        [string]$Name,
        [object[]]$Arguments,
        [bool]$Static = $false
    )
    if ($null -eq $Arguments) {
        $Arguments = [object[]]@()
    }
    $method = Get-ReflectedMethod $Type $Name $Arguments.Count $Static
    $invokeArguments = [object[]]::new($Arguments.Count)
    for ($index = 0; $index -lt $Arguments.Count; $index++) {
        $candidate = $Arguments[$index]
        if ($candidate -is [System.Management.Automation.PSObject]) {
            $candidate = $candidate.PSObject.BaseObject
        }
        $invokeArguments[$index] = $candidate
    }
    try {
        $returnValue = $method.Invoke(
            $(if ($Static) { $null } else { $Instance }),
            $invokeArguments)
    }
    catch [System.Reflection.TargetInvocationException] {
        throw $_.Exception.InnerException
    }
    return [pscustomobject]@{
        ReturnValue = $returnValue
        Arguments = $invokeArguments
    }
}

function Get-MemberValue {
    param(
        [object]$Instance,
        [string]$Name
    )
    $type = $Instance.GetType()
    $field = $type.GetField($Name, (Get-ReflectionFlags))
    if ($null -ne $field) {
        return $field.GetValue($Instance)
    }
    $property = $type.GetProperty($Name, (Get-ReflectionFlags))
    Assert-True ($null -ne $property) `
        "Member $($type.FullName).$Name is missing."
    return $property.GetValue($Instance, $null)
}

function Set-FieldValue {
    param(
        [object]$Instance,
        [string]$Name,
        $Value
    )
    $field = $Instance.GetType().GetField($Name, (Get-ReflectionFlags))
    Assert-True ($null -ne $field) `
        "Field $($Instance.GetType().FullName).$Name is missing."
    $field.SetValue($Instance, $Value)
}

function Get-StaticFieldValue {
    param(
        [Type]$Type,
        [string]$Name
    )
    $field = $Type.GetField($Name, (Get-ReflectionFlags))
    Assert-True ($null -ne $field -and $field.IsStatic) `
        "Static field $($Type.FullName).$Name is missing."
    return $field.GetValue($null)
}

function Set-StaticFieldValue {
    param(
        [Type]$Type,
        [string]$Name,
        $Value
    )
    $field = $Type.GetField($Name, (Get-ReflectionFlags))
    Assert-True ($null -ne $field -and $field.IsStatic) `
        "Static field $($Type.FullName).$Name is missing."
    if ($Value -is [System.Management.Automation.PSObject]) {
        $Value = $Value.PSObject.BaseObject
    }
    $field.SetValue($null, $Value)
}

function New-Record {
    param(
        [Type]$Type,
        [hashtable]$Fields
    )
    $record = [System.Activator]::CreateInstance($Type, $true)
    foreach ($entry in $Fields.GetEnumerator()) {
        Set-FieldValue $record ([string]$entry.Key) $entry.Value
    }
    return $record
}

function New-SaveStamp {
    param(
        [Type]$StampType,
        [string]$RelativeSavePath,
        [string]$LastSave,
        [long]$PlaytimeSeconds,
        [string]$GameDateTime
    )
    return New-Record $StampType @{
        RelativeSavePath = $RelativeSavePath.Replace('\', '/')
        LastSave = $LastSave
        PlaytimeSeconds = $PlaytimeSeconds
        GameDateTime = $GameDateTime
    }
}

function Resolve-SaveScope {
    param(
        [Type]$PathsType,
        [string]$PersistentRoot,
        [string]$SavePath
    )
    $arguments = [object[]]@($PersistentRoot, $SavePath, $null)
    $call = Invoke-ReflectedMethod `
        $null $PathsType 'TryResolveSaveScope' $arguments $true
    Assert-True ([bool]$call.ReturnValue) `
        "Could not resolve save scope for $SavePath."
    Assert-True ($null -ne $call.Arguments[2]) `
        "Save-scope resolution returned null for $SavePath."
    return $call.Arguments[2]
}

function New-Engine {
    param(
        [Type]$EngineType,
        [object]$Scope,
        [bool]$Transient = $false
    )
    $engine = [System.Activator]::CreateInstance($EngineType, $true)
    if ($Transient) {
        Invoke-ReflectedMethod `
            $engine $EngineType 'InitializeTransient' ([object[]]@()) $false |
            Out-Null
        return $engine
    }
    $arguments = [object[]]@($Scope, $null)
    $call = Invoke-ReflectedMethod `
        $engine $EngineType 'Initialize' $arguments $false
    Assert-True ([bool]$call.ReturnValue) `
        ('Engine initialization failed: ' + [string]$call.Arguments[1])
    return $engine
}

function Invoke-Upsert {
    param(
        [object]$Engine,
        [string]$MethodName,
        [object]$Record,
        [bool]$ExpectedChanged = $true
    )
    $arguments = [object[]]@($Record, $null, $null)
    $call = Invoke-ReflectedMethod `
        $Engine $Engine.GetType() $MethodName $arguments $false
    Assert-True ([bool]$call.ReturnValue) `
        ("$MethodName failed: " + [string]$call.Arguments[2])
    Assert-Equal $ExpectedChanged ([bool]$call.Arguments[1]) `
        "$MethodName reported the wrong changed flag."
}

function Invoke-RemoveStaff {
    param(
        [object]$Engine,
        [int]$StaffId,
        [bool]$ExpectedChanged = $true
    )
    $arguments = [object[]]@($StaffId, $null, $null)
    $call = Invoke-ReflectedMethod `
        $Engine $Engine.GetType() 'TryRemoveStaffRecord' $arguments $false
    Assert-True ([bool]$call.ReturnValue) `
        ('TryRemoveStaffRecord failed: ' + [string]$call.Arguments[2])
    Assert-Equal $ExpectedChanged ([bool]$call.Arguments[1]) `
        'TryRemoveStaffRecord reported the wrong changed flag.'
}

function Read-Record {
    param(
        [object]$Engine,
        [string]$MethodName,
        [int]$Identifier
    )
    $arguments = [object[]]@($Identifier, $null)
    $call = Invoke-ReflectedMethod `
        $Engine $Engine.GetType() $MethodName $arguments $false
    return [pscustomobject]@{
        Found = [bool]$call.ReturnValue
        Record = $call.Arguments[1]
    }
}

function Add-Checkpoint {
    param(
        [object]$Engine,
        [object]$Stamp,
        [long]$Sequence
    )
    $arguments = [object[]]@($Stamp, $Sequence, $null)
    $call = Invoke-ReflectedMethod `
        $Engine $Engine.GetType() 'AddOrReplaceCheckpoint' $arguments $false
    Assert-True ([bool]$call.ReturnValue) `
        ('Adding checkpoint failed: ' + [string]$call.Arguments[2])
}

function Persist-ForScope {
    param(
        [object]$Engine,
        [object]$Scope
    )
    $arguments = [object[]]@($Scope, $null)
    $call = Invoke-ReflectedMethod `
        $Engine $Engine.GetType() 'TryPersistForScope' $arguments $false
    return [pscustomobject]@{
        Succeeded = [bool]$call.ReturnValue
        ErrorMessage = [string]$call.Arguments[1]
    }
}

function Activate-Checkpoint {
    param(
        [object]$Engine,
        [object]$Stamp
    )
    $arguments = [object[]]@($Stamp, $null, $null, $null)
    $call = Invoke-ReflectedMethod `
        $Engine $Engine.GetType() 'TryActivateCheckpoint' $arguments $false
    Assert-True ([bool]$call.ReturnValue) `
        ('Checkpoint activation failed: ' + [string]$call.Arguments[3])
    return [pscustomobject]@{
        Found = [bool]$call.Arguments[1]
        Sequence = [long]$call.Arguments[2]
    }
}

function Get-Sequences {
    param(
        [object]$Document,
        [string]$CollectionName
    )
    $sequences = @()
    foreach ($record in (Get-MemberValue $Document $CollectionName)) {
        $sequences += [long](Get-MemberValue $record 'Sequence')
    }
    return $sequences
}

function Assert-MarriageMarker {
    param(
        [object]$Engine,
        [int]$GirlId,
        [string]$ExpectedPlayerName
    )
    $read = Read-Record $Engine 'TryGetMarriageRecord' $GirlId
    Assert-True $read.Found "Marriage record $GirlId was not materialized."
    Assert-Equal $ExpectedPlayerName `
        ([string](Get-MemberValue $read.Record 'PlayerName')) `
        "Marriage record $GirlId came from the wrong branch."
}

function Assert-StaffMarker {
    param(
        [object]$Engine,
        [int]$StaffId,
        [string]$ExpectedNickname
    )
    $read = Read-Record $Engine 'TryGetStaffRecord' $StaffId
    Assert-True $read.Found "Staff record $StaffId was not materialized."
    Assert-Equal $ExpectedNickname `
        ([string](Get-MemberValue $read.Record 'Nickname')) `
        "Staff record $StaffId came from the wrong branch."
}

function Assert-SnapshotMarker {
    param(
        [object]$Engine,
        [int]$GirlId,
        [string]$ExpectedNickname
    )
    $read = Read-Record $Engine 'TryGetSnapshot' $GirlId
    Assert-True $read.Found "Graduation snapshot $GirlId was not materialized."
    Assert-Equal $ExpectedNickname `
        ([string](Get-MemberValue $read.Record 'Nickname')) `
        "Graduation snapshot $GirlId came from the wrong branch."
}

$projectDirectory = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot '..'))
$projectPath = Join-Path $projectDirectory 'Graduation Details.csproj'
$assemblyPath = Join-Path `
    $projectDirectory `
    'bin\Debug\net46\com.cosmo.graduationdetails.dll'
$dependencyRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot '..\..\..\..\dll'))

if (-not $SkipBuild) {
    & dotnet build $projectPath --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw 'Graduation Details did not build; persistence tests were not run.'
    }
}

Assert-True `
    (Test-Path -LiteralPath $assemblyPath -PathType Leaf) `
    'The compiled Graduation Details assembly is missing.'
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
    ('graduationdetails-lightweight-' + [Guid]::NewGuid().ToString('N'))

try {
    [System.IO.Directory]::CreateDirectory($testRoot) | Out-Null
    Add-Type -AssemblyName System.Web.Extensions

    # Unity's JsonUtility is a native InternalCall. Supply only its managed surface
    # so the compiled engine can be exercised in an ordinary PowerShell process.
    $jsonShimPath = Join-Path $testRoot 'UnityEngine.JSONSerializeModule.dll'
    $jsonShimSource = @'
using System;
using System.Reflection;
using System.Web.Script.Serialization;

[assembly: AssemblyVersion("0.0.0.0")]

namespace UnityEngine
{
    public static class JsonUtility
    {
        private static readonly JavaScriptSerializer Serializer =
            new JavaScriptSerializer
            {
                MaxJsonLength = Int32.MaxValue,
                RecursionLimit = 100
            };

        public static string ToJson(object value)
        {
            return ToJson(value, false);
        }

        public static string ToJson(object value, bool prettyPrint)
        {
            return Serializer.Serialize(value);
        }

        public static T FromJson<T>(string json)
        {
            return (T)Serializer.Deserialize(json, typeof(T));
        }

        public static object FromJson(string json, Type type)
        {
            return Serializer.Deserialize(json, type);
        }
    }
}
'@
    $codeProvider = New-Object Microsoft.CSharp.CSharpCodeProvider
    $compilerParameters = New-Object System.CodeDom.Compiler.CompilerParameters
    $compilerParameters.GenerateExecutable = $false
    $compilerParameters.GenerateInMemory = $true
    $compilerParameters.OutputAssembly = $jsonShimPath
    $compilerParameters.CompilerOptions = '/optimize'
    $compilerParameters.ReferencedAssemblies.Add('System.dll') | Out-Null
    $compilerParameters.ReferencedAssemblies.Add(
        [System.Web.Script.Serialization.JavaScriptSerializer].Assembly.Location) |
        Out-Null
    $compileResult = $codeProvider.CompileAssemblyFromSource(
        $compilerParameters,
        $jsonShimSource)
    if ($compileResult.Errors.HasErrors) {
        $compileErrors = @(
            $compileResult.Errors |
                ForEach-Object { $_.ToString() }) -join [Environment]::NewLine
        throw ('Could not compile the managed JsonUtility test shim: ' +
            $compileErrors)
    }
    $jsonShimAssembly = $compileResult.CompiledAssembly
    Assert-Equal `
        'UnityEngine.JSONSerializeModule' `
        $jsonShimAssembly.GetName().Name `
        'The managed JsonUtility shim has the wrong assembly identity.'
    if (Test-Path -LiteralPath $jsonShimPath) {
        [System.IO.File]::Delete($jsonShimPath)
    }

    $serializer = New-Object `
        System.Web.Script.Serialization.JavaScriptSerializer
    $serializer.MaxJsonLength = [int]::MaxValue
    $serializer.RecursionLimit = 100

    $assembly = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
    $pathsType = $assembly.GetType(
        'GraduationDetails.GraduationDetailsPaths', $true)
    $scopeType = $assembly.GetType(
        'GraduationDetails.GraduationDetailsSaveScope', $true)
    $stampType = $assembly.GetType(
        'GraduationDetails.GraduationDetailsSaveStamp', $true)
    $documentType = $assembly.GetType(
        'GraduationDetails.GraduationDetailsSidecarDocument', $true)
    $engineType = $assembly.GetType(
        'GraduationDetails.GraduationDetailsStorageEngine', $true)
    $controllerType = $assembly.GetType(
        'GraduationDetails.GraduationDetailsPersistenceController', $true)
    $marriageType = $assembly.GetType(
        'GraduationDetails.MarriageRecord', $true)
    $staffType = $assembly.GetType(
        'GraduationDetails.StaffIdolRecord', $true)
    $snapshotType = $assembly.GetType(
        'GraduationDetails.GraduationSnapshot', $true)
    $custodyType = $assembly.GetType(
        'GraduationDetails.CustodyOwner', $true)

    $formatNameField = $engineType.GetField(
        'SidecarFormatName', (Get-ReflectionFlags))
    $formatVersionField = $engineType.GetField(
        'SidecarFormatVersion', (Get-ReflectionFlags))
    Assert-True `
        ($null -eq $controllerType.GetProperty(
            'Generation', (Get-ReflectionFlags))) `
        'The removed controller Generation property was reintroduced.'
    Assert-Equal `
        0 `
        @($controllerType.GetMethods((Get-ReflectionFlags)) |
            Where-Object { $_.Name -ceq 'OnVanillaLoadCompleted' }).Count `
        'The removed OnVanillaLoadCompleted hook was reintroduced.'
    Assert-True `
        ($null -eq $engineType.GetField(
            'currentScope', (Get-ReflectionFlags))) `
        'The dead storage-engine currentScope field was reintroduced.'
    Assert-Equal `
        'GraduationDetails.LightweightSidecar' `
        ([string]$formatNameField.GetRawConstantValue()) `
        'The lightweight format name changed unexpectedly.'
    Assert-Equal `
        1 `
        ([int]$formatVersionField.GetRawConstantValue()) `
        'The lightweight format version changed unexpectedly.'

    $dataRoot = Join-Path $testRoot 'data'
    $detailsRoot = Join-Path $testRoot 'GraduationDetails'
    [System.IO.Directory]::CreateDirectory($dataRoot) | Out-Null

    $rootCall = Invoke-ReflectedMethod `
        $null $pathsType 'GetRootDirectory' `
        ([object[]]@($testRoot)) $true
    Assert-PathEqual $detailsRoot ([string]$rootCall.ReturnValue) `
        'Graduation Details was not rooted beside vanilla data.'
    Assert-False (Test-Path -LiteralPath $detailsRoot) `
        'Resolving the Graduation Details root unexpectedly created it.'

    $acceptedMappings = @(
        'auto_save.json',
        'manual_save.json',
        'manual_saves\1c5ec635\save.json',
        'manual_saves\opaque.slot-A_17\save.json',
        'story_mode\playthrough.A-01\auto_save.json',
        'story_mode\playthrough.A-01\manual_save.json',
        'story_mode\playthrough.A-01\manual_saves\story-slot_B.42\save.json',
        'story_mode\playthrough.A-01\chapter_0\save.json',
        'story_mode\playthrough.A-01\chapter_3\save.json',
        'story_mode\second-playthrough\chapter_6\save.json'
    )
    $resolvedScopes = @{}
    foreach ($relativePath in $acceptedMappings) {
        $savePath = Join-Path $dataRoot $relativePath
        $scope = Resolve-SaveScope $pathsType $testRoot $savePath
        $sidecarPath = [string](Get-MemberValue $scope 'SidecarFilePath')
        $portraitPath = [string](Get-MemberValue $scope 'PortraitDirectoryPath')
        $expectedSidecarPath = Join-Path $detailsRoot $relativePath
        $expectedPortraitPath = [System.IO.Path]::ChangeExtension(
            $expectedSidecarPath, '.portraits')

        Assert-PathEqual $savePath `
            ([string](Get-MemberValue $scope 'SaveFilePath')) `
            "Vanilla path mismatch for $relativePath."
        Assert-PathEqual $expectedSidecarPath $sidecarPath `
            "Mirrored sidecar mismatch for $relativePath."
        Assert-PathEqual $expectedPortraitPath $portraitPath `
            "Mirrored portrait path mismatch for $relativePath."
        Assert-False ([bool](Get-MemberValue $scope 'IsTransient')) `
            "A physical scope became transient for $relativePath."
        Assert-False `
            ($sidecarPath -match '[\\/](Mods|saves)[\\/]') `
            "A legacy Mods/saves layer was inserted for $relativePath."
        $resolvedScopes[$relativePath] = $scope
    }

    $dataSaverCases = @(
        [pscustomobject]@{
            Name = 'auto_save'; IsJson = $true; FullPath = $false
            Expected = (Join-Path $dataRoot 'auto_save.json')
        },
        [pscustomobject]@{
            Name = 'manual_saves\1c5ec635\save'; IsJson = $false
            FullPath = $false
            Expected = (Join-Path $dataRoot 'manual_saves\1c5ec635\save.json')
        },
        [pscustomobject]@{
            Name = (Join-Path $dataRoot `
                'story_mode\playthrough.A-01\chapter_3\save.json')
            IsJson = $true; FullPath = $true
            Expected = (Join-Path $dataRoot `
                'story_mode\playthrough.A-01\chapter_3\save.json')
        }
    )
    foreach ($case in $dataSaverCases) {
        $arguments = [object[]]@(
            $testRoot, $case.Name, $case.IsJson, $case.FullPath, $null)
        $call = Invoke-ReflectedMethod `
            $null $pathsType 'TryResolveDataSaverPath' $arguments $true
        Assert-True ([bool]$call.ReturnValue) `
            "DataSaver path resolution failed for $($case.Name)."
        Assert-PathEqual $case.Expected ([string]$call.Arguments[4]) `
            "DataSaver path resolution mismatch for $($case.Name)."
    }

    foreach ($loadName in @(
            'manual_save',
            'manual_save.json',
            'story_mode\playthrough.A-01\manual_saves\story-slot_B.42\save',
            (Join-Path $dataRoot 'auto_save.json'))) {
        $arguments = [object[]]@($testRoot, $loadName, $null)
        $call = Invoke-ReflectedMethod `
            $null $pathsType 'TryResolveDataSaverLoadPath' $arguments $true
        Assert-True ([bool]$call.ReturnValue) `
            "DataSaver load resolution failed for $loadName."
        Assert-True `
            ([string]$call.Arguments[2] -like '*.json') `
            "DataSaver load resolution omitted .json for $loadName."
    }

    $rejectedPaths = @(
        'global_data.json',
        'save.json',
        'arbitrary\save.json',
        'manual_saves\..\auto_save.json',
        'story_mode\playthrough.A-01\chapter_7\save.json',
        'story_mode\playthrough.A-01\chapter_-1\save.json',
        '..\data\auto_save.json',
        '..\GraduationDetails\auto_save.json',
        (Join-Path $testRoot 'outside\auto_save.json'),
        (Join-Path $testRoot 'data-shadow\auto_save.json')
    )
    foreach ($rejectedPath in $rejectedPaths) {
        $arguments = [object[]]@($testRoot, $rejectedPath, $null)
        $call = Invoke-ReflectedMethod `
            $null $pathsType 'TryResolveSaveScope' $arguments $true
        Assert-False ([bool]$call.ReturnValue) `
            "Unsafe or unsupported save path was accepted: $rejectedPath"
        Assert-True ($null -eq $call.Arguments[2]) `
            "Rejected save path returned a scope: $rejectedPath"
    }

    $relativePath = 'manual_saves\1c5ec635\save.json'
    $scope = $resolvedScopes[$relativePath]
    $vanillaSavePath = [string](Get-MemberValue $scope 'SaveFilePath')
    $sidecarPath = [string](Get-MemberValue $scope 'SidecarFilePath')
    [System.IO.Directory]::CreateDirectory(
        [System.IO.Path]::GetDirectoryName($vanillaSavePath)) | Out-Null
    $vanillaSentinel = '{"owner":"vanilla-save"}'
    [System.IO.File]::WriteAllText($vanillaSavePath, $vanillaSentinel)

    $custodyPlayer = [System.Enum]::Parse($custodyType, 'Player')
    $earlyMarriage = New-Record $marriageType @{
        GirlId = 101; MarriedToPlayer = $true; PlayerName = 'Early Player'
        KidsCount = 1; Custody = $custodyPlayer
    }
    $lateMarriage = New-Record $marriageType @{
        GirlId = 101; MarriedToPlayer = $true; PlayerName = 'Late Player'
        KidsCount = 2; Custody = $custodyPlayer
    }
    $branchMarriage = New-Record $marriageType @{
        GirlId = 101; MarriedToPlayer = $true; PlayerName = 'Branch Player'
        KidsCount = 3; Custody = $custodyPlayer
    }
    $staffRecord = New-Record $staffType @{
        StaffId = 501; GirlId = 101; CapturedAtHire = $true
        FirstName = 'Early'; LastName = 'Idol'; Nickname = 'Early Staff'
        TextureSignature = 'early-texture'
    }
    $earlySnapshot = New-Record $snapshotType @{
        GirlId = 101; Birthdate = '2001-01-01'; AgeAtGraduation = 20
        PortraitFile = 'girl_101_early.png'; FirstName = 'Early'
        LastName = 'Idol'; Nickname = 'Early Snapshot'
        TextureSignature = 'early-texture'
    }
    $lateSnapshot = New-Record $snapshotType @{
        GirlId = 101; Birthdate = '2001-01-01'; AgeAtGraduation = 21
        PortraitFile = 'girl_101_late.png'; FirstName = 'Late'
        LastName = 'Idol'; Nickname = 'Late Snapshot'
        TextureSignature = 'late-texture'
    }
    $branchSnapshot = New-Record $snapshotType @{
        GirlId = 101; Birthdate = '2001-01-01'; AgeAtGraduation = 22
        PortraitFile = 'girl_101_branch.png'; FirstName = 'Branch'
        LastName = 'Idol'; Nickname = 'Branch Snapshot'
        TextureSignature = 'branch-texture'
    }

    $engine = New-Engine $engineType $null $true
    Assert-Equal ([long]0) `
        ([long](Get-MemberValue $engine 'LastIssuedSequence')) `
        'A transient engine did not begin at sequence zero.'
    Invoke-Upsert $engine 'TryUpsertMarriageRecord' $earlyMarriage
    Invoke-Upsert $engine 'TryUpsertStaffRecord' $staffRecord
    Invoke-Upsert $engine 'TryUpsertSnapshot' $earlySnapshot
    $beforeNoOp = [long](Get-MemberValue $engine 'LastIssuedSequence')
    Invoke-Upsert $engine 'TryUpsertSnapshot' $earlySnapshot $false
    Assert-Equal $beforeNoOp `
        ([long](Get-MemberValue $engine 'LastIssuedSequence')) `
        'An identical snapshot upsert consumed a sequence.'

    $normalizedRelativePath = $relativePath.Replace('\', '/')
    $olderStamp = New-SaveStamp `
        $stampType $normalizedRelativePath '2026-08-11 10:00:00' 100 `
        '2026-01-02 08:00:00'
    Add-Checkpoint $engine $olderStamp 3
    Invoke-Upsert $engine 'TryUpsertMarriageRecord' $lateMarriage
    Invoke-RemoveStaff $engine 501
    Invoke-Upsert $engine 'TryUpsertSnapshot' $lateSnapshot
    $newerStamp = New-SaveStamp `
        $stampType $normalizedRelativePath '2026-08-11 10:05:00' 200 `
        '2026-01-04 08:00:00'
    Add-Checkpoint $engine $newerStamp 6

    $initialPersist = Persist-ForScope $engine $scope
    Assert-True $initialPersist.Succeeded `
        ('Initial persistence failed: ' + $initialPersist.ErrorMessage)
    Assert-True (Test-Path -LiteralPath $sidecarPath -PathType Leaf) `
        'The mirrored Graduation Details sidecar was not created.'
    Assert-Equal $vanillaSentinel `
        ([System.IO.File]::ReadAllText($vanillaSavePath)) `
        'Graduation Details modified the vanilla save.'

    $initialJson = [System.IO.File]::ReadAllText($sidecarPath)
    $initialBytes = [Convert]::ToBase64String(
        [System.IO.File]::ReadAllBytes($sidecarPath))
    $document = $serializer.Deserialize($initialJson, $documentType)
    $documentFieldNames = @(
        $documentType.GetFields(
            [System.Reflection.BindingFlags]::Public -bor `
            [System.Reflection.BindingFlags]::Instance) |
            ForEach-Object { $_.Name } |
            Sort-Object)
    Assert-SequenceEqual `
        @(
            'Checkpoints', 'FormatName', 'FormatVersion', 'LastIssuedSequence',
            'MarriageMutations', 'RelativeSavePath', 'SnapshotMutations',
            'StaffMutations') `
        $documentFieldNames `
        'The lightweight root document grew an unexpected field.'
    Assert-Equal 'GraduationDetails.LightweightSidecar' `
        ([string](Get-MemberValue $document 'FormatName')) `
        'The persisted sidecar has the wrong format name.'
    Assert-Equal 1 ([int](Get-MemberValue $document 'FormatVersion')) `
        'The persisted sidecar has the wrong format version.'
    Assert-False `
        ($initialJson -match `
            'SavedData|staticVars__|PlayerData|CheckpointSnapshot|SnapshotJson|active_snapshot|\.transactions|\.snapshots') `
        'The lightweight sidecar embeds vanilla state or legacy snapshot storage.'
    Assert-SequenceEqual @([long]1, [long]4) `
        @(Get-Sequences $document 'MarriageMutations') `
        'Initial marriage mutation history is wrong.'
    Assert-SequenceEqual @([long]2, [long]5) `
        @(Get-Sequences $document 'StaffMutations') `
        'Initial staff mutation history is wrong.'
    Assert-SequenceEqual @([long]3, [long]6) `
        @(Get-Sequences $document 'SnapshotMutations') `
        'Initial snapshot mutation history is wrong.'
    Assert-SequenceEqual @([long]3, [long]6) `
        @(Get-Sequences $document 'Checkpoints') `
        'Initial exact checkpoints are wrong.'

    $loaded = New-Engine $engineType $scope
    $newerActivation = Activate-Checkpoint $loaded $newerStamp
    Assert-True $newerActivation.Found `
        'The newest exact checkpoint was not found.'
    Assert-Equal ([long]6) $newerActivation.Sequence `
        'The newest exact checkpoint activated the wrong sequence.'
    Assert-MarriageMarker $loaded 101 'Late Player'
    Assert-SnapshotMarker $loaded 101 'Late Snapshot'
    Assert-False (Read-Record $loaded 'TryGetStaffRecord' 501).Found `
        'A removed staff record survived the newer checkpoint.'

    $olderActivation = Activate-Checkpoint $loaded $olderStamp
    Assert-True $olderActivation.Found `
        'The older exact checkpoint was not found.'
    Assert-Equal ([long]3) $olderActivation.Sequence `
        'Exact rollback activated the wrong sequence.'
    Assert-MarriageMarker $loaded 101 'Early Player'
    Assert-StaffMarker $loaded 501 'Early Staff'
    Assert-SnapshotMarker $loaded 101 'Early Snapshot'
    Assert-Equal $initialBytes `
        ([Convert]::ToBase64String(
            [System.IO.File]::ReadAllBytes($sidecarPath))) `
        'In-memory rollback changed durable sidecar bytes.'

    $missingStamp = New-SaveStamp `
        $stampType $normalizedRelativePath '2026-08-11 10:02:30' 150 `
        '2026-01-03 08:00:00'
    $missingActivation = Activate-Checkpoint $loaded $missingStamp
    Assert-False $missingActivation.Found `
        'A nonexistent four-field save identity matched a checkpoint.'
    Assert-False (Read-Record $loaded 'TryGetMarriageRecord' 101).Found `
        'A missing exact checkpoint inherited marriage state.'
    Assert-False (Read-Record $loaded 'TryGetStaffRecord' 501).Found `
        'A missing exact checkpoint inherited staff state.'
    Assert-False (Read-Record $loaded 'TryGetSnapshot' 101).Found `
        'A missing exact checkpoint inherited snapshot state.'

    # Re-activate the old save and create a divergent branch. The high-water mark
    # stays monotonic, while abandoned future mutations disappear on persistence.
    $olderActivation = Activate-Checkpoint $loaded $olderStamp
    Assert-True $olderActivation.Found `
        'Could not reactivate the old checkpoint for branching.'
    Invoke-Upsert $loaded 'TryUpsertMarriageRecord' $branchMarriage
    Invoke-RemoveStaff $loaded 501
    Invoke-Upsert $loaded 'TryUpsertSnapshot' $branchSnapshot
    Assert-Equal ([long]9) `
        ([long](Get-MemberValue $loaded 'LastIssuedSequence')) `
        'The divergent branch did not retain a monotonic high-water mark.'
    $branchStamp = New-SaveStamp `
        $stampType $normalizedRelativePath '2026-08-11 10:10:00' 175 `
        '2026-01-03 21:00:00'
    Add-Checkpoint $loaded $branchStamp 9
    $branchPersist = Persist-ForScope $loaded $scope
    Assert-True $branchPersist.Succeeded `
        ('Divergent persistence failed: ' + $branchPersist.ErrorMessage)
    $branchJson = [System.IO.File]::ReadAllText($sidecarPath)
    $branchDocument = $serializer.Deserialize($branchJson, $documentType)
    Assert-Equal ([long]9) `
        ([long](Get-MemberValue $branchDocument 'LastIssuedSequence')) `
        'The divergent document has the wrong sequence high-water mark.'
    Assert-SequenceEqual @([long]1, [long]7) `
        @(Get-Sequences $branchDocument 'MarriageMutations') `
        'The divergent marriage branch retained abandoned future state.'
    Assert-SequenceEqual @([long]2, [long]8) `
        @(Get-Sequences $branchDocument 'StaffMutations') `
        'The divergent staff branch retained abandoned future state.'
    Assert-SequenceEqual @([long]3, [long]9) `
        @(Get-Sequences $branchDocument 'SnapshotMutations') `
        'The divergent snapshot branch retained abandoned future state.'
    Assert-SequenceEqual @([long]3, [long]9) `
        @(Get-Sequences $branchDocument 'Checkpoints') `
        'The divergent branch retained an abandoned future checkpoint.'

    $branchReload = New-Engine $engineType $scope
    $branchActivation = Activate-Checkpoint $branchReload $branchStamp
    Assert-True $branchActivation.Found `
        'The divergent checkpoint was not durable.'
    Assert-MarriageMarker $branchReload 101 'Branch Player'
    Assert-SnapshotMarker $branchReload 101 'Branch Snapshot'
    Assert-False (Read-Record $branchReload 'TryGetStaffRecord' 501).Found `
        'The divergent staff removal was not durable.'

    # A new game is transient: mutations must not create a physical tree until a
    # concrete vanilla save scope is persisted.
    $newGameRoot = Join-Path $testRoot 'new-game'
    $newGameDataRoot = Join-Path $newGameRoot 'data'
    $newGameDetailsRoot = Join-Path $newGameRoot 'GraduationDetails'
    [System.IO.Directory]::CreateDirectory($newGameDataRoot) | Out-Null
    $newGameRelative = 'story_mode\new-game\manual_save.json'
    $newGameScope = Resolve-SaveScope `
        $pathsType $newGameRoot (Join-Path $newGameDataRoot $newGameRelative)
    $transient = New-Engine $engineType $null $true
    $transientSnapshot = New-Record $snapshotType @{
        GirlId = 303; Birthdate = '2003-03-03'; AgeAtGraduation = 19
        PortraitFile = 'girl_303_transient.png'; FirstName = 'Transient'
        LastName = 'Idol'; Nickname = 'Transient Snapshot'
        TextureSignature = 'transient-texture'
    }
    Invoke-Upsert $transient 'TryUpsertSnapshot' $transientSnapshot
    Assert-False (Test-Path -LiteralPath $newGameDetailsRoot) `
        'A transient new-game mutation wrote to disk.'
    $newGameStamp = New-SaveStamp `
        $stampType ($newGameRelative.Replace('\', '/')) `
        '2026-08-11 11:00:00' 10 '2026-02-01 09:00:00'
    Add-Checkpoint $transient $newGameStamp 1
    $newGamePersist = Persist-ForScope $transient $newGameScope
    Assert-True $newGamePersist.Succeeded `
        ('The first physical new-game save failed: ' +
            $newGamePersist.ErrorMessage)
    $newGameSidecar = [string](Get-MemberValue `
        $newGameScope 'SidecarFilePath')
    Assert-True (Test-Path -LiteralPath $newGameSidecar -PathType Leaf) `
        'The first physical new-game save did not create its sidecar.'
    $newGameReload = New-Engine $engineType $newGameScope
    $newGameActivation = Activate-Checkpoint $newGameReload $newGameStamp
    Assert-True $newGameActivation.Found `
        'The first new-game checkpoint was not durable.'
    Assert-SnapshotMarker $newGameReload 303 'Transient Snapshot'

    # Lock the primary against replacement. Production persistence must report a
    # failure, keep the old primary byte-for-byte, and remove its temporary file.
    $beforeFailureBytes = [Convert]::ToBase64String(
        [System.IO.File]::ReadAllBytes($sidecarPath))
    $failedMarriage = New-Record $marriageType @{
        GirlId = 101; MarriedToPlayer = $true; PlayerName = 'Must Not Persist'
        KidsCount = 4; Custody = $custodyPlayer
    }
    Invoke-Upsert $branchReload 'TryUpsertMarriageRecord' $failedMarriage
    $failureStamp = New-SaveStamp `
        $stampType $normalizedRelativePath '2026-08-11 10:15:00' 190 `
        '2026-01-03 22:00:00'
    Add-Checkpoint $branchReload $failureStamp 10
    $lockedStream = [System.IO.File]::Open(
        $sidecarPath,
        [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read,
        [System.IO.FileShare]::None)
    try {
        $failedPersist = Persist-ForScope $branchReload $scope
    }
    finally {
        $lockedStream.Dispose()
    }
    Assert-False $failedPersist.Succeeded `
        'Atomic persistence unexpectedly succeeded while primary was locked.'
    Assert-True (-not [string]::IsNullOrWhiteSpace(
            $failedPersist.ErrorMessage)) `
        'Atomic persistence failure returned no diagnostic.'
    Assert-Equal $beforeFailureBytes `
        ([Convert]::ToBase64String(
            [System.IO.File]::ReadAllBytes($sidecarPath))) `
        'A failed atomic replacement changed the durable primary.'
    $sidecarParent = [System.IO.Path]::GetDirectoryName($sidecarPath)
    $temporaryArtifacts = @(
        [System.IO.Directory]::GetFiles(
            $sidecarParent,
            ([System.IO.Path]::GetFileName($sidecarPath) +
                '.graduationdetails.tmp.*')))
    Assert-Equal 0 $temporaryArtifacts.Count `
        'A failed atomic replacement left a temporary sidecar behind.'
    Assert-False `
        (Test-Path -LiteralPath `
            ($sidecarPath + '.graduationdetails.bak')) `
        'A failed atomic replacement left a backup sidecar behind.'

    $postFailureReload = New-Engine $engineType $scope
    $postFailureActivation = Activate-Checkpoint `
        $postFailureReload $branchStamp
    Assert-True $postFailureActivation.Found `
        'The last successful checkpoint was lost after atomic failure.'
    Assert-MarriageMarker $postFailureReload 101 'Branch Player'
    $failedActivation = Activate-Checkpoint $postFailureReload $failureStamp
    Assert-False $failedActivation.Found `
        'A failed atomic replacement made its checkpoint durable.'
    Assert-Equal $vanillaSentinel `
        ([System.IO.File]::ReadAllText($vanillaSavePath)) `
        'The persistence harness or engine changed the vanilla save sentinel.'

    # Captured portraits now enter the controller through TryStagePortrait. Force
    # File.Replace to fail while a stale destination is locked: the old complete
    # file must remain intact and the durable temporary file must be removed. Once
    # unlocked, the same API must atomically promote the fresh bytes over the stale
    # same-named destination.
    $stageEngine = New-Engine $engineType $null $true
    $stageWorkingDirectory = Join-Path `
        $testRoot 'portrait-controller-working\capture-stage'
    Set-StaticFieldValue $controllerType 'storageEngine' $stageEngine
    Set-StaticFieldValue `
        $controllerType 'workingPortraitDirectory' $stageWorkingDirectory
    $stageFileName = 'girl_808_capture.png'
    $stageSourceDirectory = Join-Path $testRoot 'portrait-capture-source'
    [System.IO.Directory]::CreateDirectory($stageSourceDirectory) | Out-Null
    [System.IO.Directory]::CreateDirectory($stageWorkingDirectory) | Out-Null
    $stageSourcePath = Join-Path $stageSourceDirectory $stageFileName
    $stageDestinationPath = Join-Path $stageWorkingDirectory $stageFileName
    $capturedPortraitBytes = [byte[]](8, 0, 8, 1, 9, 9)
    $staleStageBytes = [byte[]](4, 0, 4)
    [System.IO.File]::WriteAllBytes(
        $stageSourcePath, $capturedPortraitBytes)
    [System.IO.File]::WriteAllBytes(
        $stageDestinationPath, $staleStageBytes)

    $lockedStage = [System.IO.File]::Open(
        $stageDestinationPath,
        [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read,
        [System.IO.FileShare]::None)
    try {
        $failedStageCall = Invoke-ReflectedMethod `
            $null $controllerType 'TryStagePortrait' `
            ([object[]]@($stageSourcePath, $stageDestinationPath)) $true
    }
    finally {
        $lockedStage.Dispose()
    }
    Assert-False ([bool]$failedStageCall.ReturnValue) `
        'Locked portrait staging unexpectedly reported success.'
    Assert-Equal `
        ([Convert]::ToBase64String($staleStageBytes)) `
        ([Convert]::ToBase64String(
            [System.IO.File]::ReadAllBytes($stageDestinationPath))) `
        'Failed portrait staging changed the prior complete destination.'
    Assert-Equal 0 `
        @([System.IO.Directory]::GetFiles(
            $stageWorkingDirectory,
            ($stageFileName + '.tmp.*'))).Count `
        'Failed portrait staging left a partial temporary file.'

    $successfulStageCall = Invoke-ReflectedMethod `
        $null $controllerType 'TryStagePortrait' `
        ([object[]]@($stageSourcePath, $stageDestinationPath)) $true
    Assert-True ([bool]$successfulStageCall.ReturnValue) `
        'Portrait staging could not replace a stale same-named destination.'
    Assert-Equal `
        ([Convert]::ToBase64String($capturedPortraitBytes)) `
        ([Convert]::ToBase64String(
            [System.IO.File]::ReadAllBytes($stageDestinationPath))) `
        'Successful portrait staging retained stale destination bytes.'
    Assert-Equal 0 `
        @([System.IO.Directory]::GetFiles(
            $stageWorkingDirectory,
            ($stageFileName + '.tmp.*'))).Count `
        'Successful portrait staging left a temporary file.'

    # Coroutine completions can arrive after a load/new-game rotation. The
    # destination issued by the old working scope must no longer authorize a copy
    # into the new cache merely because its leaf filename is still safe.
    $oldStageBytes = [Convert]::ToBase64String(
        [System.IO.File]::ReadAllBytes($stageDestinationPath))
    $rotatedStageWorking = Join-Path `
        $testRoot 'portrait-controller-working\capture-stage-rotated'
    $rotatedStageDestination = Join-Path `
        $rotatedStageWorking $stageFileName
    Set-StaticFieldValue `
        $controllerType 'workingPortraitDirectory' $rotatedStageWorking
    $staleDestinationCall = Invoke-ReflectedMethod `
        $null $controllerType 'TryStagePortrait' `
        ([object[]]@($stageSourcePath, $stageDestinationPath)) $true
    Assert-False ([bool]$staleDestinationCall.ReturnValue) `
        'An expected destination from the prior working scope was accepted.'
    Assert-Equal $oldStageBytes `
        ([Convert]::ToBase64String(
            [System.IO.File]::ReadAllBytes($stageDestinationPath))) `
        'Rejecting an old expected destination changed the prior cache file.'
    Assert-False `
        (Test-Path -LiteralPath $rotatedStageDestination) `
        'A stale expected destination contaminated the rotated working cache.'
    if (Test-Path -LiteralPath $rotatedStageWorking -PathType Container) {
        Assert-Equal 0 `
            @([System.IO.Directory]::GetFiles(
                $rotatedStageWorking,
                ($stageFileName + '.tmp.*'))).Count `
            'Stale expected-destination rejection left a rotated-cache temp file.'
    }

    # Startup cleanup removes only GUID-N sessions directly owned by its cache
    # root. Rotation removes the immediately prior owned GUID session. A sentinel
    # directory with a non-GUID name must survive both operations byte-for-byte.
    $cleanupRoot = Join-Path $testRoot 'portrait-session-cleanup'
    $startupSession = Join-Path `
        $cleanupRoot ([Guid]::NewGuid().ToString('N'))
    $sentinelSession = Join-Path $cleanupRoot 'do-not-delete-sentinel'
    $startupPortraits = Join-Path $startupSession 'Portraits'
    $sentinelPortraits = Join-Path $sentinelSession 'Portraits'
    [System.IO.Directory]::CreateDirectory($startupPortraits) | Out-Null
    [System.IO.Directory]::CreateDirectory($sentinelPortraits) | Out-Null
    $sentinelPath = Join-Path $sentinelSession 'keep.txt'
    $sentinelText = 'not owned by a GUID session'
    [System.IO.File]::WriteAllText($sentinelPath, $sentinelText)
    [System.IO.File]::WriteAllBytes(
        (Join-Path $startupPortraits 'owned.png'),
        [byte[]](1, 2, 3))

    Invoke-ReflectedMethod `
        $null $controllerType 'TryDeleteOwnedWorkingSessions' `
        ([object[]]@($cleanupRoot)) $true | Out-Null
    Assert-False (Test-Path -LiteralPath $startupSession) `
        'Startup cleanup retained an owned GUID working session.'
    Assert-True (Test-Path -LiteralPath $sentinelSession -PathType Container) `
        'Startup cleanup deleted a non-GUID sentinel directory.'
    Assert-Equal $sentinelText `
        ([System.IO.File]::ReadAllText($sentinelPath)) `
        'Startup cleanup changed the non-GUID sentinel.'

    $rotationSession = Join-Path `
        $cleanupRoot ([Guid]::NewGuid().ToString('N'))
    $rotationPortraits = Join-Path $rotationSession 'Portraits'
    [System.IO.Directory]::CreateDirectory($rotationPortraits) | Out-Null
    [System.IO.File]::WriteAllBytes(
        (Join-Path $rotationPortraits 'owned.png'),
        [byte[]](3, 2, 1))
    Invoke-ReflectedMethod `
        $null $controllerType 'TryDeletePriorWorkingSession' `
        ([object[]]@($cleanupRoot, $rotationPortraits)) $true | Out-Null
    Assert-False (Test-Path -LiteralPath $rotationSession) `
        'Scope rotation retained its prior owned GUID session.'
    Invoke-ReflectedMethod `
        $null $controllerType 'TryDeletePriorWorkingSession' `
        ([object[]]@($cleanupRoot, $sentinelPortraits)) $true | Out-Null
    Assert-True (Test-Path -LiteralPath $sentinelSession -PathType Container) `
        'Scope rotation deleted a non-GUID sentinel directory.'
    Assert-Equal $sentinelText `
        ([System.IO.File]::ReadAllText($sentinelPath)) `
        'Scope rotation changed the non-GUID sentinel.'

    # Exercise the controller's final Save As portrait helper against real files.
    # A destination may already contain an older portrait under the same safe name;
    # the durable temporary-file replacement must overwrite it, not skip the copy.
    $portraitSourceScope = $resolvedScopes[
        'manual_saves\opaque.slot-A_17\save.json']
    $portraitTargetScope = $resolvedScopes[
        'story_mode\playthrough.A-01\manual_save.json']
    $portraitSwitchedScope = $resolvedScopes[
        'story_mode\playthrough.A-01\chapter_0\save.json']
    $portraitFileName = 'girl_909_save_as.png'
    $portraitEngine = New-Engine $engineType $null $true
    $portraitSnapshot = New-Record $snapshotType @{
        GirlId = 909; Birthdate = '2009-09-09'; AgeAtGraduation = 18
        PortraitFile = $portraitFileName; FirstName = 'Portrait'
        LastName = 'Source'; Nickname = 'Save As Portrait'
        TextureSignature = 'portrait-save-as'
    }
    Invoke-Upsert $portraitEngine 'TryUpsertSnapshot' $portraitSnapshot
    $portraitWorking = Join-Path $testRoot 'portrait-controller-working\save-as'
    Set-StaticFieldValue $controllerType 'storageEngine' $portraitEngine
    Set-StaticFieldValue $controllerType 'activeScope' $portraitSourceScope
    Set-StaticFieldValue `
        $controllerType 'workingPortraitDirectory' $portraitWorking

    $sourcePortraitDirectory = [string](Get-MemberValue `
        $portraitSourceScope 'PortraitDirectoryPath')
    $targetPortraitDirectory = [string](Get-MemberValue `
        $portraitTargetScope 'PortraitDirectoryPath')
    [System.IO.Directory]::CreateDirectory($sourcePortraitDirectory) | Out-Null
    [System.IO.Directory]::CreateDirectory($targetPortraitDirectory) | Out-Null
    $sourcePortrait = Join-Path $sourcePortraitDirectory $portraitFileName
    $targetPortrait = Join-Path $targetPortraitDirectory $portraitFileName
    $freshPortraitBytes = [byte[]](1, 3, 3, 7, 9, 0, 9)
    $stalePortraitBytes = [byte[]](9, 9, 9)
    [System.IO.File]::WriteAllBytes($sourcePortrait, $freshPortraitBytes)
    [System.IO.File]::WriteAllBytes($targetPortrait, $stalePortraitBytes)

    $copyCall = Invoke-ReflectedMethod `
        $null $controllerType 'CopyReferencedPortraitsLocked' `
        ([object[]]@($portraitTargetScope)) $true
    Assert-True ([bool]$copyCall.ReturnValue) `
        'Save As could not replace a stale same-named destination portrait.'
    Assert-Equal `
        ([Convert]::ToBase64String($freshPortraitBytes)) `
        ([Convert]::ToBase64String(
            [System.IO.File]::ReadAllBytes($targetPortrait))) `
        'Save As retained stale portrait bytes at the destination.'
    Assert-Equal 0 `
        @([System.IO.Directory]::GetFiles(
            $targetPortraitDirectory,
            ($portraitFileName + '.tmp.*'))).Count `
        'Successful portrait replacement left a temporary file behind.'

    # When the destination cannot be replaced, the helper first stages the active
    # source in the working scope. That retry source must survive independently of
    # activeScope, so a later scope switch cannot strand the only good portrait.
    $retryFileName = 'girl_910_retry.png'
    $retryEngine = New-Engine $engineType $null $true
    $retrySnapshot = New-Record $snapshotType @{
        GirlId = 910; Birthdate = '2010-10-10'; AgeAtGraduation = 18
        PortraitFile = $retryFileName; FirstName = 'Portrait'
        LastName = 'Retry'; Nickname = 'Retry Portrait'
        TextureSignature = 'portrait-retry'
    }
    Invoke-Upsert $retryEngine 'TryUpsertSnapshot' $retrySnapshot
    $retryWorking = Join-Path $testRoot 'portrait-controller-working\retry'
    Set-StaticFieldValue $controllerType 'storageEngine' $retryEngine
    Set-StaticFieldValue $controllerType 'activeScope' $portraitSourceScope
    Set-StaticFieldValue `
        $controllerType 'workingPortraitDirectory' $retryWorking

    $retrySource = Join-Path $sourcePortraitDirectory $retryFileName
    $retryDestination = Join-Path $targetPortraitDirectory $retryFileName
    $retryBytes = [byte[]](2, 4, 6, 8, 10)
    $retryStaleBytes = [byte[]](10, 8, 6)
    [System.IO.File]::WriteAllBytes($retrySource, $retryBytes)
    [System.IO.File]::WriteAllBytes($retryDestination, $retryStaleBytes)
    $lockedPortrait = [System.IO.File]::Open(
        $retryDestination,
        [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read,
        [System.IO.FileShare]::None)
    try {
        $failedCopyCall = Invoke-ReflectedMethod `
            $null $controllerType 'CopyReferencedPortraitsLocked' `
            ([object[]]@($portraitTargetScope)) $true
    }
    finally {
        $lockedPortrait.Dispose()
    }
    Assert-False ([bool]$failedCopyCall.ReturnValue) `
        'A locked destination portrait unexpectedly reported a successful copy.'
    Assert-Equal `
        ([Convert]::ToBase64String($retryStaleBytes)) `
        ([Convert]::ToBase64String(
            [System.IO.File]::ReadAllBytes($retryDestination))) `
        'A failed portrait replacement changed the stale destination.'
    $stagedRetry = Join-Path $retryWorking $retryFileName
    Assert-True (Test-Path -LiteralPath $stagedRetry -PathType Leaf) `
        'A failed destination copy did not retain a staged retry source.'
    Assert-Equal `
        ([Convert]::ToBase64String($retryBytes)) `
        ([Convert]::ToBase64String(
            [System.IO.File]::ReadAllBytes($stagedRetry))) `
        'The staged portrait retry source has the wrong bytes.'
    Assert-Equal 0 `
        @([System.IO.Directory]::GetFiles(
            $targetPortraitDirectory,
            ($retryFileName + '.tmp.*'))).Count `
        'Failed portrait replacement left a destination temporary file behind.'

    # Remove the original source and switch the active scope to one that has no
    # portrait. A successful retry now proves the working stage is authoritative.
    [System.IO.File]::Delete($retrySource)
    Set-StaticFieldValue `
        $controllerType 'activeScope' $portraitSwitchedScope
    $retryCopyCall = Invoke-ReflectedMethod `
        $null $controllerType 'CopyReferencedPortraitsLocked' `
        ([object[]]@($portraitTargetScope)) $true
    Assert-True ([bool]$retryCopyCall.ReturnValue) `
        'The staged portrait could not be retried after active-scope switching.'
    Assert-Equal `
        ([Convert]::ToBase64String($retryBytes)) `
        ([Convert]::ToBase64String(
            [System.IO.File]::ReadAllBytes($retryDestination))) `
        'Retry after scope switching did not install the retained portrait.'
    Assert-True (Test-Path -LiteralPath $stagedRetry -PathType Leaf) `
        'Retry processing discarded its stable working portrait source.'

    Write-Host (
        'Graduation Details lightweight persistence regression tests passed: ' +
        'mirrored paths, exact rollback, divergent branching, transient first ' +
        'save, compact schema, atomic-failure recovery, and portrait Save As ' +
        'replacement/retry, capture staging, and owned-session cleanup.')
}
finally {
    [System.AppDomain]::CurrentDomain.remove_AssemblyResolve(
        $dependencyResolver)
    if (Test-Path -LiteralPath $testRoot -PathType Container) {
        $normalizedTempRoot = [System.IO.Path]::GetFullPath(
            [System.IO.Path]::GetTempPath()).TrimEnd('\') + '\'
        $normalizedTestRoot = [System.IO.Path]::GetFullPath($testRoot)
        if ($normalizedTestRoot.StartsWith(
                $normalizedTempRoot,
                [System.StringComparison]::OrdinalIgnoreCase) -and
            [System.IO.Path]::GetFileName($normalizedTestRoot).StartsWith(
                'graduationdetails-lightweight-',
                [System.StringComparison]::Ordinal)) {
            [System.IO.Directory]::Delete($normalizedTestRoot, $true)
        }
    }
}
