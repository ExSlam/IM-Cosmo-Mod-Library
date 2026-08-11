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

function Assert-Equal {
    param(
        [object]$Expected,
        [object]$Actual,
        [string]$Message
    )

    if (-not [object]::Equals($Expected, $Actual)) {
        throw ($Message + "`nExpected: $Expected`nActual:   $Actual")
    }
}

function Assert-SequenceEqual {
    param(
        [object[]]$Expected,
        [object[]]$Actual,
        [string]$Message
    )

    $expectedText = @($Expected | ForEach-Object { [string]$_ }) -join ','
    $actualText = @($Actual | ForEach-Object { [string]$_ }) -join ','
    if ($expectedText -cne $actualText) {
        throw ($Message + "`nExpected: $expectedText`nActual:   $actualText")
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

function Invoke-InstanceMethod {
    param(
        [object]$Instance,
        [string]$Name,
        [object[]]$Arguments
    )

    if ($null -eq $Arguments) {
        $Arguments = [object[]]::new(0)
    }

    $flags = [System.Reflection.BindingFlags]'Instance,Public,NonPublic'
    $method = Get-MethodByArity `
        $Instance.GetType() `
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

    $returnValue = $method.Invoke($Instance, $invokeArguments)
    return [pscustomobject]@{
        ReturnValue = $returnValue
        Arguments = $invokeArguments
    }
}

function Invoke-StaticMethod {
    param(
        [Type]$Type,
        [string]$Name,
        [object[]]$Arguments
    )

    $flags = [System.Reflection.BindingFlags]'Static,Public,NonPublic'
    $method = Get-MethodByArity $Type $Name $Arguments.Count $flags
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

function Get-FieldValue {
    param(
        [object]$Instance,
        [string]$Name
    )

    $flags = [System.Reflection.BindingFlags]'Instance,Public,NonPublic'
    $field = $Instance.GetType().GetField($Name, $flags)
    Assert-True ($null -ne $field) "Field $Name is missing on $($Instance.GetType().FullName)."
    return ,$field.GetValue($Instance)
}

function Set-FieldValue {
    param(
        [object]$Instance,
        [string]$Name,
        [object]$Value
    )

    $flags = [System.Reflection.BindingFlags]'Instance,Public,NonPublic'
    $field = $Instance.GetType().GetField($Name, $flags)
    Assert-True ($null -ne $field) "Field $Name is missing on $($Instance.GetType().FullName)."
    $field.SetValue($Instance, $Value)
}

function New-TestDate {
    param([string]$Value)

    return [DateTime]::ParseExact(
        $Value,
        'yyyy-MM-dd HH:mm:ss',
        [System.Globalization.CultureInfo]::InvariantCulture,
        [System.Globalization.DateTimeStyles]::None)
}

function New-PendingEvent {
    param(
        [Type]$PendingEventType,
        [long]$Sequence,
        [DateTime]$GameDate,
        [int]$IdolId,
        [string]$EventType
    )

    $pending = [System.Activator]::CreateInstance($PendingEventType, $true)
    Set-FieldValue $pending 'CaptureSequence' $Sequence
    Set-FieldValue $pending 'GameDateKey' `
        ($GameDate.Year * 10000 + $GameDate.Month * 100 + $GameDate.Day)
    Set-FieldValue $pending 'GameDateTime' `
        ($GameDate.ToString(
            'O',
            [System.Globalization.CultureInfo]::InvariantCulture))
    Set-FieldValue $pending 'IdolId' $IdolId
    Set-FieldValue $pending 'EntityKind' 'idol'
    Set-FieldValue $pending 'EntityId' ([string]$IdolId)
    Set-FieldValue $pending 'EventType' $EventType
    Set-FieldValue $pending 'SourcePatch' 'Test-LightweightPersistence'
    Set-FieldValue $pending 'NamespaceIdentifier' 'tests.persistence'
    Set-FieldValue $pending 'PayloadJson' ('{"sequence":' + $Sequence + '}')
    return $pending
}

function New-VanillaStamp {
    param(
        [Type]$StampType,
        [string]$RelativePath,
        [string]$LastSave,
        [long]$PlaytimeSeconds,
        [DateTime]$GameDate
    )

    $stamp = [System.Activator]::CreateInstance($StampType, $true)
    Set-FieldValue $stamp 'RelativeSavePath' $RelativePath
    Set-FieldValue $stamp 'LastSave' $LastSave
    Set-FieldValue $stamp 'PlaytimeSeconds' $PlaytimeSeconds
    Set-FieldValue $stamp 'GameDateTime' `
        ($GameDate.ToString(
            'yyyy-MM-dd HH:mm:ss',
            [System.Globalization.CultureInfo]::InvariantCulture))
    return $stamp
}

function New-ScopedEngine {
    param(
        [Type]$EngineType,
        [object]$Scope
    )

    $engine = [System.Activator]::CreateInstance($EngineType, $true)
    Invoke-InstanceMethod $engine 'InitializeTransient' ([object[]]@()) | Out-Null
    $relativePath = [string](Get-FieldValue $Scope 'RelativeSavePath')
    Set-FieldValue $engine 'currentSidecarPath' `
        ([string](Get-FieldValue $Scope 'SidecarFilePath'))
    Set-FieldValue $engine 'currentRelativeSavePath' `
        ($relativePath.Replace('\', '/'))
    return $engine
}

function Invoke-TestPersistenceBoundary {
    param(
        [object]$Engine,
        [object]$Scope,
        [System.Web.Script.Serialization.JavaScriptSerializer]$Serializer
    )

    # Application.persistentDataPath is a Unity InternalCall and cannot execute in
    # this standalone PowerShell/.NET process. The JSON codec, however, is managed
    # IMDC code and must be the exact production codec. This regression test must
    # never substitute JavaScriptSerializer for the sidecar serialization boundary.
    $relativePath = [string](Get-FieldValue $Scope 'RelativeSavePath')
    $buildCall = Invoke-InstanceMethod `
        $Engine `
        'BuildDocumentLocked' `
        ([object[]]@($relativePath))
    $document = $buildCall.ReturnValue
    $sidecarJsonType = $Engine.GetType().Assembly.GetType(
        'IMDataCore.LightweightSidecarJson',
        $true)
    $serializeCall = Invoke-StaticMethod `
        $sidecarJsonType `
        'Serialize' `
        ([object[]]@($document))
    $json = [string]$serializeCall.ReturnValue
    $sidecarPath = [string](Get-FieldValue $Scope 'SidecarFilePath')
    $parentDirectory = [System.IO.Path]::GetDirectoryName($sidecarPath)
    [System.IO.Directory]::CreateDirectory($parentDirectory) | Out-Null
    $temporaryPath = $sidecarPath + '.imdc.tmp.' + [Guid]::NewGuid().ToString('N')
    $backupPath = $sidecarPath + '.imdc.bak'
    $succeeded = $false
    $errorMessage = ''
    try {
        $bytes = (New-Object System.Text.UTF8Encoding($false)).GetBytes($json)
        $stream = [System.IO.File]::Open(
            $temporaryPath,
            [System.IO.FileMode]::CreateNew,
            [System.IO.FileAccess]::Write,
            [System.IO.FileShare]::None)
        try {
            $stream.Write($bytes, 0, $bytes.Length)
            $stream.Flush($true)
        }
        finally {
            $stream.Dispose()
        }

        if ([System.IO.File]::Exists($sidecarPath)) {
            if ([System.IO.File]::Exists($backupPath)) {
                [System.IO.File]::Delete($backupPath)
            }

            [System.IO.File]::Replace(
                $temporaryPath,
                $sidecarPath,
                $backupPath,
                $true)
            if ([System.IO.File]::Exists($backupPath)) {
                [System.IO.File]::Delete($backupPath)
            }
        }
        else {
            [System.IO.File]::Move($temporaryPath, $sidecarPath)
        }

        Invoke-InstanceMethod `
            $Engine `
            'CommitActiveAsDurableLocked' `
            ([object[]]@()) | Out-Null
        $succeeded = $true
    }
    catch {
        $errorMessage = $_.Exception.GetBaseException().Message
    }
    finally {
        if ([System.IO.File]::Exists($temporaryPath)) {
            [System.IO.File]::Delete($temporaryPath)
        }
    }

    return [pscustomobject]@{
        Succeeded = $succeeded
        ErrorMessage = $errorMessage
        Document = $document
        Json = $json
        SidecarPath = $sidecarPath
    }
}

function Import-TestSidecar {
    param(
        [Type]$EngineType,
        [Type]$DocumentType,
        [object]$Scope,
        [string]$Json,
        [System.Web.Script.Serialization.JavaScriptSerializer]$Serializer,
        [System.Collections.IList]$DisposableEngines
    )

    $sidecarJsonType = $EngineType.Assembly.GetType(
        'IMDataCore.LightweightSidecarJson',
        $true)
    $deserializeCall = Invoke-StaticMethod `
        $sidecarJsonType `
        'Deserialize' `
        ([object[]]@($Json))
    $document = $deserializeCall.ReturnValue
    $engine = New-ScopedEngine $EngineType $Scope
    $DisposableEngines.Add($engine) | Out-Null
    $validationArguments = [object[]]@($document, $null)
    $validationCall = Invoke-InstanceMethod `
        $engine `
        'TryValidateDocumentLocked' `
        $validationArguments
    Assert-True `
        ([bool]$validationCall.ReturnValue) `
        ('Compiled sidecar validation failed: ' +
            [string]$validationCall.Arguments[1])
    Invoke-InstanceMethod `
        $engine `
        'LoadDocumentLocked' `
        ([object[]]@($document)) | Out-Null
    return $engine
}

function Get-EventIdentifiers {
    param(
        [object]$Engine,
        [int]$IdolId
    )

    $arguments = [object[]]@($IdolId, 100, $null, $null)
    $call = Invoke-InstanceMethod `
        $Engine `
        'TryReadRecentEventsForIdol' `
        $arguments
    Assert-True `
        ([bool]$call.ReturnValue) `
        ('Reading active events failed: ' + [string]$call.Arguments[3])
    $identifiers = @()
    foreach ($eventRecord in $call.Arguments[2]) {
        $identifiers += [long]$eventRecord.EventId
    }

    return $identifiers
}

function Assert-CustomValue {
    param(
        [object]$Engine,
        [string]$NamespaceIdentifier,
        [string]$DataKey,
        [string]$ExpectedValue
    )

    $arguments = [object[]]@(
        $NamespaceIdentifier,
        $DataKey,
        $null,
        $null)
    $call = Invoke-InstanceMethod `
        $Engine `
        'TryGetCustomData' `
        $arguments
    Assert-True `
        ([bool]$call.ReturnValue) `
        ('Expected custom value is missing: ' +
            $NamespaceIdentifier + '/' + $DataKey + '; ' +
            [string]$call.Arguments[3])
    Assert-Equal `
        $ExpectedValue `
        ([string]$call.Arguments[2]) `
        ('Custom value mismatch: ' + $NamespaceIdentifier + '/' + $DataKey)
}

function Assert-CustomMissing {
    param(
        [object]$Engine,
        [string]$NamespaceIdentifier,
        [string]$DataKey
    )

    $arguments = [object[]]@(
        $NamespaceIdentifier,
        $DataKey,
        $null,
        $null)
    $call = Invoke-InstanceMethod `
        $Engine `
        'TryGetCustomData' `
        $arguments
    Assert-False `
        ([bool]$call.ReturnValue) `
        ('Removed custom value remained materialized: ' +
            $NamespaceIdentifier + '/' + $DataKey)
    Assert-True `
        ([string]::IsNullOrEmpty([string]$call.Arguments[3])) `
        ('Missing custom value produced an error: ' +
            [string]$call.Arguments[3])
}

function Invoke-MoneyQuery {
    param(
        [object]$Engine,
        [DateTime]$StartInclusive,
        [DateTime]$EndExclusive,
        [int]$MaxCount
    )

    $arguments = [object[]]@(
        $StartInclusive,
        $EndExclusive,
        $MaxCount,
        $null,
        $null,
        $null)
    $call = Invoke-InstanceMethod `
        $Engine `
        'TryReadMoneyTransactions' `
        $arguments
    Assert-True `
        ([bool]$call.ReturnValue) `
        ('Reading money transactions failed: ' +
            [string]$call.Arguments[5])
    return [pscustomobject]@{
        Transactions = $call.Arguments[3]
        WasTruncated = [bool]$call.Arguments[4]
    }
}

function Get-RecordSequences {
    param(
        [object]$Document,
        [string]$CollectionField
    )

    $records = Get-FieldValue $Document $CollectionField
    $sequences = @()
    foreach ($record in $records) {
        $sequences += [long](Get-FieldValue $record 'Sequence')
    }

    return $sequences
}

function Test-DocumentValidation {
    param(
        [object]$Engine,
        [object]$Document
    )

    $arguments = [object[]]@($Document, $null)
    return Invoke-InstanceMethod `
        $Engine `
        'TryValidateDocumentLocked' `
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
        throw 'IM Data Core did not build; lightweight persistence tests were not run.'
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
    ('imdatacore-lightweight-' + [Guid]::NewGuid().ToString('N'))
$engines = New-Object System.Collections.ArrayList

try {
    [System.IO.Directory]::CreateDirectory($testRoot) | Out-Null
    Add-Type -AssemblyName System.Web.Extensions

    # Supply the managed surface of Unity JsonUtility before loading IMDC only for
    # unrelated compiled payload helpers that still use it. The lightweight
    # sidecar itself must use IMDC's production LightweightSidecarJson codec in
    # this test so the test cannot hide runtime collection-loss bugs.
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
        Remove-Item -LiteralPath $jsonShimPath -Force
    }

    $serializer = New-Object System.Web.Script.Serialization.JavaScriptSerializer
    $serializer.MaxJsonLength = [int]::MaxValue
    $serializer.RecursionLimit = 100

    $assembly = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
    $corePathsType = $assembly.GetType('IMDataCore.CorePaths', $true)
    $engineType = $assembly.GetType(
        'IMDataCore.LightweightCoreStorageEngine',
        $true)
    $documentType = $assembly.GetType(
        'IMDataCore.LightweightSidecarDocument',
        $true)
    $checkpointType = $assembly.GetType(
        'IMDataCore.LightweightCheckpointRecord',
        $true)
    $eventRecordType = $assembly.GetType(
        'IMDataCore.LightweightEventRecord',
        $true)
    $customMutationType = $assembly.GetType(
        'IMDataCore.LightweightCustomMutationRecord',
        $true)
    $stampType = $assembly.GetType('IMDataCore.VanillaSaveStamp', $true)
    $pendingEventType = $assembly.GetType('IMDataCore.PendingEvent', $true)

    $constantFlags = [System.Reflection.BindingFlags]'Static,Public,NonPublic'
    $formatName = [string]$engineType.GetField(
        'SidecarFormatName',
        $constantFlags).GetRawConstantValue()
    $formatVersion = [int]$engineType.GetField(
        'SidecarFormatVersion',
        $constantFlags).GetRawConstantValue()
    Assert-Equal `
        'IMDataCore.LightweightSidecar' `
        $formatName `
        'The lightweight sidecar format name changed unexpectedly.'
    Assert-Equal 1 $formatVersion 'The lightweight sidecar format version changed unexpectedly.'

    $publicFieldFlags = [System.Reflection.BindingFlags]'Instance,Public'
    $documentFieldNames = @(
        $documentType.GetFields($publicFieldFlags) |
            Sort-Object MetadataToken |
            ForEach-Object { $_.Name }
    )
    $checkpointFieldNames = @(
        $checkpointType.GetFields($publicFieldFlags) |
            Sort-Object MetadataToken |
            ForEach-Object { $_.Name }
    )
    Assert-SequenceEqual `
        @(
            'FormatName',
            'FormatVersion',
            'RelativeSavePath',
            'LastIssuedSequence',
            'Checkpoints',
            'Events',
            'CustomMutations') `
        $documentFieldNames `
        'The lightweight envelope acquired an unexpected persisted field.'
    Assert-SequenceEqual `
        @(
            'RelativeSavePath',
            'LastSave',
            'PlaytimeSeconds',
            'GameDateTime',
            'Sequence') `
        $checkpointFieldNames `
        'Checkpoints must remain stamp-to-sequence mappings without snapshots.'

    foreach ($dtoType in @(
            $documentType,
            $checkpointType,
            $eventRecordType,
            $customMutationType)) {
        foreach ($field in $dtoType.GetFields($publicFieldFlags)) {
            $persistedTypes = @($field.FieldType)
            if ($field.FieldType.IsGenericType) {
                $persistedTypes += $field.FieldType.GetGenericArguments()
            }

            foreach ($persistedType in $persistedTypes) {
                Assert-False `
                    ($persistedType.Assembly.GetName().Name -eq 'Assembly-CSharp' -or
                        $persistedType.FullName -match 'SaveManager|SavedData|staticVars') `
                    ("Persisted DTO field references a vanilla structure: " +
                        $dtoType.FullName + '.' + $field.Name)
            }
        }
    }

    $relativePath = 'manual_saves\1c5ec635\save.json'
    $vanillaSavePath = Join-Path (Join-Path $testRoot 'data') $relativePath
    [System.IO.Directory]::CreateDirectory(
        [System.IO.Path]::GetDirectoryName($vanillaSavePath)) | Out-Null
    $vanillaSentinel = '{"owner":"vanilla"}'
    [System.IO.File]::WriteAllText($vanillaSavePath, $vanillaSentinel)

    $scopeArguments = [object[]]@($testRoot, $vanillaSavePath, $null)
    $scopeCall = Invoke-StaticMethod `
        $corePathsType `
        'TryCreateSaveScope' `
        $scopeArguments
    Assert-True `
        ([bool]$scopeCall.ReturnValue) `
        'CorePaths could not create the temporary physical save scope.'
    $scope = $scopeCall.Arguments[2]
    $sidecarPath = [string](Get-FieldValue $scope 'SidecarFilePath')
    Assert-PathEqual `
        (Join-Path (Join-Path $testRoot 'IMDataCore') $relativePath) `
        $sidecarPath `
        'The persistence test scope did not use the exact mirrored sidecar path.'

    $dayOne = New-TestDate '2026-01-01 09:00:00'
    $dayTwo = New-TestDate '2026-01-02 09:00:00'
    $dayThree = New-TestDate '2026-01-03 09:00:00'
    $dayFour = New-TestDate '2026-01-04 09:00:00'
    $namespaceIdentifier = 'tests.persistence'
    $dataKey = 'branch-value'
    $earlyValue = '{"value":"early"}'
    $futureValue = '{"value":"future"}'
    $branchValue = '{"value":"branch"}'

    $engine = New-ScopedEngine $engineType $scope
    $engines.Add($engine) | Out-Null
    $listDefinition = [System.Collections.Generic.List[object]].GetGenericTypeDefinition()
    $pendingListType = $listDefinition.MakeGenericType(
        [Type[]]@($pendingEventType))
    $pendingEvents = [System.Activator]::CreateInstance($pendingListType)
    $pendingEvents.Add((New-PendingEvent $pendingEventType 1 $dayOne 101 'early_event'))
    $pendingEvents.Add((New-PendingEvent $pendingEventType 3 $dayTwo 101 'checkpoint_event'))
    $pendingEvents.Add((New-PendingEvent $pendingEventType 5 $dayThree 101 'future_event'))
    $pendingEvents.Add((New-PendingEvent $pendingEventType 7 $dayFour 101 'latest_event'))

    $appendArguments = [object[]]@($pendingEvents, $null)
    $appendCall = Invoke-InstanceMethod $engine 'AppendEvents' $appendArguments
    Assert-True `
        ([bool]$appendCall.ReturnValue) `
        ('Appending sequenced events failed: ' + [string]$appendCall.Arguments[1])

    $setArguments = [object[]]@(
        [long]2,
        $dayOne,
        $namespaceIdentifier,
        $dataKey,
        $earlyValue,
        $null)
    $setCall = Invoke-InstanceMethod $engine 'TrySetCustomData' $setArguments
    Assert-True `
        ([bool]$setCall.ReturnValue) `
        ('Appending the early SET mutation failed: ' + [string]$setCall.Arguments[5])

    $setArguments = [object[]]@(
        [long]4,
        $dayThree,
        $namespaceIdentifier,
        $dataKey,
        $futureValue,
        $null)
    $setCall = Invoke-InstanceMethod $engine 'TrySetCustomData' $setArguments
    Assert-True `
        ([bool]$setCall.ReturnValue) `
        ('Appending the future SET mutation failed: ' + [string]$setCall.Arguments[5])

    $removeArguments = [object[]]@(
        [long]6,
        $dayFour,
        $namespaceIdentifier,
        $dataKey,
        $null)
    $removeCall = Invoke-InstanceMethod `
        $engine `
        'TryRemoveCustomData' `
        $removeArguments
    Assert-True `
        ([bool]$removeCall.ReturnValue) `
        ('Appending the REMOVE mutation failed: ' +
            [string]$removeCall.Arguments[4])
    Assert-CustomMissing $engine $namespaceIdentifier $dataKey

    $normalizedRelativePath = $relativePath.Replace('\', '/')
    $olderStamp = New-VanillaStamp `
        $stampType `
        $normalizedRelativePath `
        '2026-08-11 10:00:00' `
        100 `
        $dayTwo
    $newerStamp = New-VanillaStamp `
        $stampType `
        $normalizedRelativePath `
        '2026-08-11 10:05:00' `
        200 `
        $dayFour

    foreach ($checkpointSpec in @(
            [pscustomobject]@{ Stamp = $olderStamp; Sequence = [long]3 },
            [pscustomobject]@{ Stamp = $newerStamp; Sequence = [long]7 })) {
        $checkpointArguments = [object[]]@(
            $checkpointSpec.Stamp,
            $checkpointSpec.Sequence,
            $null)
        $checkpointCall = Invoke-InstanceMethod `
            $engine `
            'AddOrReplaceCheckpoint' `
            $checkpointArguments
        Assert-True `
            ([bool]$checkpointCall.ReturnValue) `
            ('Adding an exact checkpoint failed: ' +
                [string]$checkpointCall.Arguments[2])
    }

    $initialPersistence = Invoke-TestPersistenceBoundary `
        $engine `
        $scope `
        $serializer
    Assert-True `
        ([bool]$initialPersistence.Succeeded) `
        ('Initial atomic persistence failed: ' + $initialPersistence.ErrorMessage)
    Assert-True `
        (Test-Path -LiteralPath $sidecarPath -PathType Leaf) `
        'The lightweight sidecar was not persisted beneath the temporary IMDC root.'
    $initialJson = [System.IO.File]::ReadAllText($sidecarPath)
    Assert-True `
        ($initialJson.Contains('"Checkpoints":[')) `
        'The production sidecar JSON omitted the Checkpoints collection.'
    Assert-True `
        ($initialJson.Contains('"Events":[')) `
        'The production sidecar JSON omitted the Events collection.'
    Assert-True `
        ($initialJson.Contains('"CustomMutations":[')) `
        'The production sidecar JSON omitted the CustomMutations collection.'

    # Regression for the real failure observed in Idol Manager: a header-only
    # sidecar with a sequence watermark but no persisted collections must be
    # rejected instead of being normalized into three empty lists.
    $sidecarJsonType = $engineType.Assembly.GetType(
        'IMDataCore.LightweightSidecarJson',
        $true)
    $headerOnlyRejected = $false
    try {
        Invoke-StaticMethod `
            $sidecarJsonType `
            'Deserialize' `
            ([object[]]@(
                '{"FormatName":"IMDataCore.LightweightSidecar","FormatVersion":1,"RelativeSavePath":"manual_saves/1c5ec635/save.json","LastIssuedSequence":2711}')) |
            Out-Null
    }
    catch {
        $headerOnlyRejected = $true
    }
    Assert-True $headerOnlyRejected 'A header-only/truncated sidecar was accepted.'
    $initialBytes = [Convert]::ToBase64String(
        [System.IO.File]::ReadAllBytes($sidecarPath))
    Assert-Equal $formatName `
        ([string](Get-FieldValue $initialPersistence.Document 'FormatName')) `
        'The persisted document has the wrong format name.'
    Assert-Equal $formatVersion `
        ([int](Get-FieldValue $initialPersistence.Document 'FormatVersion')) `
        'The persisted document has the wrong format version.'
    Assert-False `
        ($initialJson -match 'CheckpointSnapshot|SnapshotJson|SavedData|staticVars__|PlayerData') `
        'The lightweight sidecar contains a checkpoint snapshot or vanilla structure.'

    $loadedEngine = Import-TestSidecar `
        $engineType `
        $documentType `
        $scope `
        $initialJson `
        $serializer `
        $engines
    Assert-SequenceEqual `
        @([long]7, [long]5, [long]3, [long]1) `
        @(Get-EventIdentifiers $loadedEngine 101) `
        'Reload did not rebuild the complete active event index.'
    Assert-CustomMissing $loadedEngine $namespaceIdentifier $dataKey

    $activateArguments = [object[]]@($olderStamp, $null, $null, $null)
    $activateCall = Invoke-InstanceMethod `
        $loadedEngine `
        'TryActivateCheckpoint' `
        $activateArguments
    Assert-True `
        ([bool]$activateCall.ReturnValue) `
        ('Exact rollback failed: ' + [string]$activateCall.Arguments[3])
    Assert-True `
        ([bool]$activateCall.Arguments[1]) `
        'The older exact checkpoint was not found.'
    Assert-Equal `
        ([long]3) `
        ([long]$activateCall.Arguments[2]) `
        'Exact rollback activated the wrong sequence watermark.'
    Assert-SequenceEqual `
        @([long]3, [long]1) `
        @(Get-EventIdentifiers $loadedEngine 101) `
        'Exact rollback retained events after the older checkpoint.'
    Assert-CustomValue `
        $loadedEngine `
        $namespaceIdentifier `
        $dataKey `
        $earlyValue
    Assert-Equal `
        $initialBytes `
        ([Convert]::ToBase64String(
            [System.IO.File]::ReadAllBytes($sidecarPath))) `
        'Exact in-memory rollback changed durable sidecar bytes.'

    $fallbackEngine = Import-TestSidecar `
        $engineType `
        $documentType `
        $scope `
        $initialJson `
        $serializer `
        $engines
    $missingStamp = New-VanillaStamp `
        $stampType `
        $normalizedRelativePath `
        '2026-08-11 10:02:30' `
        150 `
        $dayTwo
    $missingArguments = [object[]]@($missingStamp, $null, $null, $null)
    $missingCall = Invoke-InstanceMethod `
        $fallbackEngine `
        'TryActivateCheckpoint' `
        $missingArguments
    Assert-True `
        ([bool]$missingCall.ReturnValue) `
        ('No-exact lookup failed: ' + [string]$missingCall.Arguments[3])
    Assert-False `
        ([bool]$missingCall.Arguments[1]) `
        'A nonexistent composite checkpoint identity matched unexpectedly.'

    $fallbackCutoff = $dayTwo.AddHours(12)
    $fallbackArguments = [object[]]@($fallbackCutoff, $null, $null)
    $fallbackCall = Invoke-InstanceMethod `
        $fallbackEngine `
        'TryActivateThroughGameDate' `
        $fallbackArguments
    Assert-True `
        ([bool]$fallbackCall.ReturnValue) `
        ('Game-date fallback failed: ' + [string]$fallbackCall.Arguments[2])
    Assert-Equal `
        ([long]3) `
        ([long]$fallbackCall.Arguments[1]) `
        'Game-date fallback reported the wrong active sequence.'
    Assert-SequenceEqual `
        @([long]3, [long]1) `
        @(Get-EventIdentifiers $fallbackEngine 101) `
        'Game-date fallback retained later events.'
    Assert-CustomValue `
        $fallbackEngine `
        $namespaceIdentifier `
        $dataKey `
        $earlyValue
    Assert-Equal `
        $initialBytes `
        ([Convert]::ToBase64String(
            [System.IO.File]::ReadAllBytes($sidecarPath))) `
        'Game-date fallback changed durable sidecar bytes.'

    # Commit a new branch from the older exact checkpoint. New sequence values are
    # greater than the discarded durable watermark, while old future records must
    # disappear from the newly persisted source history.
    $branchEvents = [System.Activator]::CreateInstance($pendingListType)
    $branchEvents.Add((New-PendingEvent `
        $pendingEventType `
        8 `
        $dayTwo.AddHours(13) `
        101 `
        'divergent_event'))
    $branchAppendArguments = [object[]]@($branchEvents, $null)
    $branchAppendCall = Invoke-InstanceMethod `
        $loadedEngine `
        'AppendEvents' `
        $branchAppendArguments
    Assert-True `
        ([bool]$branchAppendCall.ReturnValue) `
        ('Appending the divergent event failed: ' +
            [string]$branchAppendCall.Arguments[1])

    $branchSetArguments = [object[]]@(
        [long]9,
        $dayTwo.AddHours(13),
        $namespaceIdentifier,
        $dataKey,
        $branchValue,
        $null)
    $branchSetCall = Invoke-InstanceMethod `
        $loadedEngine `
        'TrySetCustomData' `
        $branchSetArguments
    Assert-True `
        ([bool]$branchSetCall.ReturnValue) `
        ('Appending the divergent SET mutation failed: ' +
            [string]$branchSetCall.Arguments[5])

    $branchStamp = New-VanillaStamp `
        $stampType `
        $normalizedRelativePath `
        '2026-08-11 10:10:00' `
        175 `
        $dayTwo.AddHours(13)
    $branchCheckpointArguments = [object[]]@($branchStamp, [long]9, $null)
    $branchCheckpointCall = Invoke-InstanceMethod `
        $loadedEngine `
        'AddOrReplaceCheckpoint' `
        $branchCheckpointArguments
    Assert-True `
        ([bool]$branchCheckpointCall.ReturnValue) `
        ('Adding the divergent checkpoint failed: ' +
            [string]$branchCheckpointCall.Arguments[2])

    $branchPersistence = Invoke-TestPersistenceBoundary `
        $loadedEngine `
        $scope `
        $serializer
    Assert-True `
        ([bool]$branchPersistence.Succeeded) `
        ('Divergent atomic persistence failed: ' +
            $branchPersistence.ErrorMessage)
    $branchBytes = [Convert]::ToBase64String(
        [System.IO.File]::ReadAllBytes($sidecarPath))
    Assert-False `
        ($branchBytes -ceq $initialBytes) `
        'Explicit divergent persistence did not change the sidecar.'
    Assert-Equal `
        ([long]9) `
        ([long](Get-FieldValue $branchPersistence.Document 'LastIssuedSequence')) `
        'The divergent sidecar has the wrong last-issued sequence.'
    Assert-SequenceEqual `
        @([long]1, [long]3, [long]8) `
        @(Get-RecordSequences $branchPersistence.Document 'Events') `
        'The divergent sidecar retained old future events or lost branch events.'
    Assert-SequenceEqual `
        @([long]2, [long]9) `
        @(Get-RecordSequences $branchPersistence.Document 'CustomMutations') `
        'The divergent sidecar retained old future custom mutations.'
    Assert-SequenceEqual `
        @([long]3, [long]9) `
        @(Get-RecordSequences $branchPersistence.Document 'Checkpoints') `
        'The divergent sidecar retained the discarded future checkpoint.'

    $branchReload = Import-TestSidecar `
        $engineType `
        $documentType `
        $scope `
        ([System.IO.File]::ReadAllText($sidecarPath)) `
        $serializer `
        $engines
    Assert-SequenceEqual `
        @([long]8, [long]3, [long]1) `
        @(Get-EventIdentifiers $branchReload 101) `
        'Reloading the divergent sidecar restored discarded future events.'
    Assert-CustomValue `
        $branchReload `
        $namespaceIdentifier `
        $dataKey `
        $branchValue

    $discardedCheckpointArguments = [object[]]@(
        $newerStamp,
        $null,
        $null,
        $null)
    $discardedCheckpointCall = Invoke-InstanceMethod `
        $branchReload `
        'TryActivateCheckpoint' `
        $discardedCheckpointArguments
    Assert-True `
        ([bool]$discardedCheckpointCall.ReturnValue) `
        ('Discarded-checkpoint lookup failed: ' +
            [string]$discardedCheckpointCall.Arguments[3])
    Assert-False `
        ([bool]$discardedCheckpointCall.Arguments[1]) `
        'The discarded future checkpoint survived divergent persistence.'

    # A failed replacement must leave the prior durable primary intact, clean its
    # temporary file, and avoid committing the active mutation as durable state.
    $uncommittedEvents = [System.Activator]::CreateInstance($pendingListType)
    $uncommittedEvents.Add((New-PendingEvent `
        $pendingEventType `
        10 `
        $dayTwo.AddHours(14) `
        101 `
        'uncommitted_event'))
    $uncommittedArguments = [object[]]@($uncommittedEvents, $null)
    $uncommittedCall = Invoke-InstanceMethod `
        $branchReload `
        'AppendEvents' `
        $uncommittedArguments
    Assert-True `
        ([bool]$uncommittedCall.ReturnValue) `
        ('Could not stage the atomic-failure event: ' +
            [string]$uncommittedCall.Arguments[1])

    $durableBranchBytes = [Convert]::ToBase64String(
        [System.IO.File]::ReadAllBytes($sidecarPath))
    $heldPrimary = [System.IO.File]::Open(
        $sidecarPath,
        [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read,
        [System.IO.FileShare]::Read)
    try {
        $failedPersistence = Invoke-TestPersistenceBoundary `
            $branchReload `
            $scope `
            $serializer
    }
    finally {
        $heldPrimary.Dispose()
    }
    Assert-False `
        ([bool]$failedPersistence.Succeeded) `
        'Replacing a locked primary unexpectedly succeeded.'
    Assert-Equal `
        $durableBranchBytes `
        ([Convert]::ToBase64String(
            [System.IO.File]::ReadAllBytes($sidecarPath))) `
        'A failed atomic replacement changed the durable primary.'
    Assert-False `
        (Test-Path -LiteralPath ($sidecarPath + '.imdc.bak')) `
        'A failed atomic replacement left a backup file behind.'
    $temporaryPattern = [System.IO.Path]::GetFileName($sidecarPath) +
        '.imdc.tmp.*'
    Assert-Equal `
        0 `
        ([System.IO.Directory]::GetFiles(
            [System.IO.Path]::GetDirectoryName($sidecarPath),
            $temporaryPattern).Length) `
        'A failed atomic replacement left a temporary file behind.'

    $postFailureReload = Import-TestSidecar `
        $engineType `
        $documentType `
        $scope `
        ([System.IO.File]::ReadAllText($sidecarPath)) `
        $serializer `
        $engines
    Assert-SequenceEqual `
        @([long]8, [long]3, [long]1) `
        @(Get-EventIdentifiers $postFailureReload 101) `
        'A failed atomic replacement made the staged event durable.'

    # Invalid/custom rows tagged as money transactions are not public ledger
    # results. They must neither consume maxCount nor imply truncation; only one
    # more successfully parsed transaction beyond the result limit may do that.
    $moneyEngine = New-ScopedEngine $engineType $scope
    $engines.Add($moneyEngine) | Out-Null
    $moneyRows = [System.Activator]::CreateInstance($pendingListType)
    $moneyRowDefinitions = @(
        [pscustomobject]@{
            Sequence = [long]1
            Date = $dayOne
            Payload = '{'
        },
        [pscustomobject]@{
            Sequence = [long]2
            Date = $dayOne.AddMinutes(10)
            Payload = '"custom-money-payload"'
        },
        [pscustomobject]@{
            Sequence = [long]3
            Date = $dayOne.AddMinutes(20)
            Payload = '{"amount":100,"balance_before":1000,"balance_after":1100,"category_code":"events","detail_code":"first","section_code":"income","detail_json":"","transaction_group":"valid-1","source_assembly":"tests","source_type":"ledger","source_method":"first"}'
        },
        [pscustomobject]@{
            Sequence = [long]4
            Date = $dayOne.AddMinutes(30)
            Payload = ''
        },
        [pscustomobject]@{
            Sequence = [long]5
            Date = $dayOne.AddMinutes(40)
            Payload = '{"amount":200,"balance_before":1100,"balance_after":1300,"category_code":"events","detail_code":"second","section_code":"income","detail_json":"","transaction_group":"valid-2","source_assembly":"tests","source_type":"ledger","source_method":"second"}'
        }
    )
    foreach ($rowDefinition in $moneyRowDefinitions) {
        $moneyRow = New-PendingEvent `
            $pendingEventType `
            $rowDefinition.Sequence `
            $rowDefinition.Date `
            -1 `
            'money_transaction'
        Set-FieldValue $moneyRow 'PayloadJson' $rowDefinition.Payload
        $moneyRows.Add($moneyRow)
    }

    $moneyAppendArguments = [object[]]@($moneyRows, $null)
    $moneyAppendCall = Invoke-InstanceMethod `
        $moneyEngine `
        'AppendEvents' `
        $moneyAppendArguments
    Assert-True `
        ([bool]$moneyAppendCall.ReturnValue) `
        ('Appending mixed money rows failed: ' +
            [string]$moneyAppendCall.Arguments[1])

    $moneyRangeStart = $dayOne.Date
    $moneyRangeEnd = $moneyRangeStart.AddDays(1)
    $fullValidQuery = Invoke-MoneyQuery `
        $moneyEngine `
        $moneyRangeStart `
        $moneyRangeEnd `
        2
    Assert-Equal `
        2 `
        $fullValidQuery.Transactions.Count `
        'Malformed/custom rows consumed the money result limit.'
    Assert-SequenceEqual `
        @([long]3, [long]5) `
        @($fullValidQuery.Transactions | ForEach-Object { [long]$_.EventId }) `
        'Later valid money rows did not fill the requested result count.'
    Assert-SequenceEqual `
        @([long]100, [long]200) `
        @($fullValidQuery.Transactions | ForEach-Object { [long]$_.Amount }) `
        'The money query returned the wrong valid parsed transactions.'
    Assert-False `
        $fullValidQuery.WasTruncated `
        'Invalid money payloads incorrectly caused wasTruncated.'

    $extraValidRow = New-PendingEvent `
        $pendingEventType `
        6 `
        $dayOne.AddMinutes(50) `
        -1 `
        'money_transaction'
    Set-FieldValue `
        $extraValidRow `
        'PayloadJson' `
        '{"amount":300,"balance_before":1300,"balance_after":1600,"category_code":"events","detail_code":"third","section_code":"income","detail_json":"","transaction_group":"valid-3","source_assembly":"tests","source_type":"ledger","source_method":"third"}'
    $extraMoneyRows = [System.Activator]::CreateInstance($pendingListType)
    $extraMoneyRows.Add($extraValidRow)
    $extraAppendArguments = [object[]]@($extraMoneyRows, $null)
    $extraAppendCall = Invoke-InstanceMethod `
        $moneyEngine `
        'AppendEvents' `
        $extraAppendArguments
    Assert-True `
        ([bool]$extraAppendCall.ReturnValue) `
        ('Appending the additional valid money row failed: ' +
            [string]$extraAppendCall.Arguments[1])

    $truncatedValidQuery = Invoke-MoneyQuery `
        $moneyEngine `
        $moneyRangeStart `
        $moneyRangeEnd `
        2
    Assert-Equal `
        2 `
        $truncatedValidQuery.Transactions.Count `
        'The truncated money query returned more than maxCount valid rows.'
    Assert-SequenceEqual `
        @([long]3, [long]5) `
        @($truncatedValidQuery.Transactions | ForEach-Object { [long]$_.EventId }) `
        'Truncation changed which first valid money rows were returned.'
    Assert-True `
        $truncatedValidQuery.WasTruncated `
        'An additional valid parsed transaction did not set wasTruncated.'

    # Validation rejects identities that cannot select one watermark and documents
    # whose claimed watermark is behind persisted mutation history.
    $duplicateDocument = $serializer.Deserialize($initialJson, $documentType)
    $duplicateCheckpoints = Get-FieldValue $duplicateDocument 'Checkpoints'
    $duplicateCheckpoint = $serializer.Deserialize(
        $serializer.Serialize($duplicateCheckpoints[0]),
        $checkpointType)
    $duplicateCheckpoints.Add($duplicateCheckpoint)
    $validationEngine = New-ScopedEngine $engineType $scope
    $engines.Add($validationEngine) | Out-Null
    $duplicateValidation = Test-DocumentValidation `
        $validationEngine `
        $duplicateDocument
    Assert-False `
        ([bool]$duplicateValidation.ReturnValue) `
        'A sidecar with duplicate composite checkpoint identities passed validation.'
    Assert-True `
        ([string]$duplicateValidation.Arguments[1] -match 'duplicate checkpoint identities') `
        ('Duplicate-checkpoint validation returned the wrong error: ' +
            [string]$duplicateValidation.Arguments[1])

    $watermarkDocument = $serializer.Deserialize($initialJson, $documentType)
    (Get-FieldValue $watermarkDocument 'Checkpoints').Clear()
    Set-FieldValue $watermarkDocument 'LastIssuedSequence' ([long]6)
    $watermarkValidation = Test-DocumentValidation `
        $validationEngine `
        $watermarkDocument
    Assert-False `
        ([bool]$watermarkValidation.ReturnValue) `
        'A sidecar with a sequence beyond its watermark passed validation.'
    Assert-True `
        ([string]$watermarkValidation.Arguments[1] -match 'watermark is inconsistent') `
        ('Invalid-watermark validation returned the wrong error: ' +
            [string]$watermarkValidation.Arguments[1])

    Assert-Equal `
        $vanillaSentinel `
        ([System.IO.File]::ReadAllText($vanillaSavePath)) `
        'The persistence harness or compiled engine changed the vanilla save sentinel.'

    Write-Host (
        'Lightweight persistence regression tests passed: exact rollback, ' +
        'game-date fallback, divergent commit, money-query filtering, ' +
        'compact schema, and validation.')
}
finally {
    for ($index = $engines.Count - 1; $index -ge 0; $index--) {
        $candidateEngine = $engines[$index]
        if ($null -ne $candidateEngine) {
            try {
                ([System.IDisposable]$candidateEngine).Dispose()
            }
            catch {
                Write-Warning ('Could not dispose a test engine: ' + $_.Exception.Message)
            }
        }
    }

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
                    'imdatacore-lightweight-',
                    [System.StringComparison]::Ordinal)) `
            'Refused to clean a persistence test directory outside the expected temp scope.'
        Remove-Item -LiteralPath $normalizedTestRoot -Recurse -Force
    }
}
