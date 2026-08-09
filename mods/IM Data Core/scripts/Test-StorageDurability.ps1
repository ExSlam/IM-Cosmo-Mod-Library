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

function Invoke-Public {
    param(
        [object]$Instance,
        [string]$Name,
        [object[]]$Arguments
    )

    $flags = [System.Reflection.BindingFlags]'Instance,Public'
    $method = Get-MethodByArity $Instance.GetType() $Name $Arguments.Count $flags
    $invokeArguments = [object[]]::new($Arguments.Count)
    for ($index = 0; $index -lt $Arguments.Count; $index++) {
        $candidate = $Arguments[$index]
        if ($candidate -is [System.Management.Automation.PSObject]) {
            $candidate = $candidate.PSObject.BaseObject
        }

        $invokeArguments[$index] = $candidate
    }

    $result = $method.Invoke($Instance, $invokeArguments)
    for ($index = 0; $index -lt $Arguments.Count; $index++) {
        $Arguments[$index] = $invokeArguments[$index]
    }

    return $result
}

$projectDirectory = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot '..'))
$projectPath = Join-Path $projectDirectory 'IM Data Core.csproj'
$assemblyPath = Join-Path $projectDirectory 'bin\Debug\net46\com.cosmo.imdatacore.dll'
$dependencyRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot '..\..\..\..\dll'))

if (-not $SkipBuild) {
    & dotnet build $projectPath --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw 'IM Data Core did not build; storage regression tests were not run.'
    }
}

Assert-True (Test-Path -LiteralPath $assemblyPath) 'The IM Data Core assembly is missing.'
Assert-True (Test-Path -LiteralPath $dependencyRoot) 'The game dependency directory is missing.'

$dependencyResolver = [System.ResolveEventHandler]{
    param($sender, $eventArgs)

    $simpleName = (New-Object System.Reflection.AssemblyName($eventArgs.Name)).Name
    $candidatePath = Join-Path $dependencyRoot ($simpleName + '.dll')
    if (Test-Path -LiteralPath $candidatePath) {
        return [System.Reflection.Assembly]::LoadFrom($candidatePath)
    }

    return $null
}.GetNewClosure()

[System.AppDomain]::CurrentDomain.add_AssemblyResolve($dependencyResolver)
$testRoot = Join-Path (
    [System.IO.Path]::GetTempPath()) (
        'imdatacore-storage-regression-' + [Guid]::NewGuid().ToString('N'))
$sqliteEngine = $null
$flatEngine = $null

try {
    [System.IO.Directory]::CreateDirectory($testRoot) | Out-Null
    $assembly = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
    $sqliteType = $assembly.GetType('IMDataCore.SqliteCoreStorageEngine', $true)
    $flatType = $assembly.GetType('IMDataCore.FlatFileCoreStorageEngine', $true)

    # Exercise the flat engine's actual durable-file primitive without invoking
    # UnityEngine.JsonUtility, whose internal calls require a running Unity player.
    $flatEngine = [System.Activator]::CreateInstance($flatType, $true)
    $flatPath = Join-Path $testRoot 'flat-atomic.json'
    $storagePathField = $flatType.GetField(
        'storagePath',
        [System.Reflection.BindingFlags]'Instance,NonPublic')
    $storagePathField.SetValue($flatEngine, $flatPath)
    $writeAtomicMethod = Get-MethodByArity `
        $flatType `
        'WriteSerializedStateAtomicallyLocked' `
        2 `
        ([System.Reflection.BindingFlags]'Instance,NonPublic')

    $writeAtomicMethod.Invoke($flatEngine, [object[]]@('{"version":1}', $true)) | Out-Null
    $writeAtomicMethod.Invoke($flatEngine, [object[]]@('{"version":2}', $true)) | Out-Null
    Assert-True `
        ([System.IO.File]::ReadAllText($flatPath) -eq '{"version":2}') `
        'Flat atomic replacement did not install the new primary.'
    Assert-True `
        ([System.IO.File]::ReadAllText($flatPath + '.bak') -eq '{"version":1}') `
        'Flat atomic replacement did not preserve the prior primary as backup.'

    $writeFailed = $false
    $heldPrimary = [System.IO.File]::Open(
        $flatPath,
        [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read,
        [System.IO.FileShare]::Read)
    try {
        try {
            $writeAtomicMethod.Invoke(
                $flatEngine,
                [object[]]@('{"version":3}', $true)) | Out-Null
        }
        catch {
            $writeFailed = $true
        }
    }
    finally {
        $heldPrimary.Dispose()
    }

    Assert-True $writeFailed 'The locked-primary flat write unexpectedly succeeded.'
    Assert-True `
        ([System.IO.File]::ReadAllText($flatPath) -eq '{"version":2}') `
        'A failed flat write changed the durable primary.'
    Assert-True `
        ([System.IO.File]::ReadAllText($flatPath + '.bak') -eq '{"version":1}') `
        'A failed flat write changed the durable backup.'
    Assert-True `
        (-not [System.IO.File]::Exists($flatPath + '.tmp')) `
        'A failed flat write left a temporary file behind.'

    # The pure-managed envelope gate runs before Unity JsonUtility. Verify that
    # syntactically valid empty/partial roots cannot be mistaken for legacy state,
    # including field names hidden inside a payload string.
    $readTopLevelFields = Get-MethodByArity `
        $flatType `
        'ReadTopLevelJsonFieldNames' `
        1 `
        ([System.Reflection.BindingFlags]'Static,NonPublic')
    $requireLegacyField = Get-MethodByArity `
        $flatType `
        'RequireLegacyStateField' `
        2 `
        ([System.Reflection.BindingFlags]'Static,NonPublic')
    $requiredLegacyFields = @(
        'NextEventId',
        'Events',
        'CustomData',
        'SingleParticipation',
        'StatusWindows',
        'ShowCastWindows',
        'ContractWindows',
        'RelationshipWindows',
        'TourParticipation',
        'AwardResults',
        'ElectionResults',
        'PushWindows'
    )
    $invalidLegacyRoots = @(
        '{}',
        '{"NextEventId":1,"Events":[],"CustomData":[],"SingleParticipation":[],"StatusWindows":[],"payload":"ShowCastWindows ContractWindows RelationshipWindows TourParticipation AwardResults ElectionResults PushWindows"}'
    )
    foreach ($invalidRoot in $invalidLegacyRoots) {
        $topLevelFields = $readTopLevelFields.Invoke(
            $null,
            [object[]]@($invalidRoot))
        $wasRejected = $false
        try {
            foreach ($requiredField in $requiredLegacyFields) {
                $requireLegacyField.Invoke(
                    $null,
                    [object[]]@($topLevelFields, $requiredField)) | Out-Null
            }
        }
        catch {
            $wasRejected = $true
        }

        Assert-True $wasRejected `
            'A syntactically valid empty/partial legacy flat root passed validation.'
    }

    # Guard the exact version-1 projection used to verify and migrate hashes made by
    # the previous envelope before the checkpoint-history field existed.
    $versionOneType = $flatType.GetNestedType(
        'FlatFileStateVersionOne',
        [System.Reflection.BindingFlags]'NonPublic')
    $versionOneFields = @(
        $versionOneType.GetFields(
            [System.Reflection.BindingFlags]'Instance,Public') |
            Sort-Object MetadataToken |
            ForEach-Object { $_.Name }
    )
    $expectedVersionOneFields = @(
        'FormatVersion',
        'IntegritySha256',
        'NextEventId',
        'Events',
        'CustomData',
        'SingleParticipation',
        'StatusWindows',
        'ShowCastWindows',
        'ContractWindows',
        'RelationshipWindows',
        'TourParticipation',
        'AwardResults',
        'ElectionResults',
        'PushWindows',
        'CheckpointFingerprint',
        'CheckpointEventWatermark',
        'CheckpointSnapshotJson',
        'CheckpointCreatedUtc'
    )
    Assert-True `
        (($versionOneFields -join '|') -eq
            ($expectedVersionOneFields -join '|')) `
        'The flat version-1 migration projection no longer matches its durable field order.'

    # SQLite can run outside Unity. Populate every save-scoped mutable table,
    # checkpoint it, apply destructive/same-timestamp post-save changes, and restore.
    $sqliteEngine = [System.Activator]::CreateInstance($sqliteType, $true)
    $databasePath = Join-Path $testRoot 'checkpoint.db'
    $initializeArguments = [object[]]@($databasePath, $null)
    Assert-True `
        ([bool](Invoke-Public $sqliteEngine 'Initialize' $initializeArguments)) `
        ('SQLite initialize failed: ' + [string]$initializeArguments[1])

    $parameterType = $sqliteType.GetNestedType(
        'SqliteParameter',
        [System.Reflection.BindingFlags]'NonPublic')
    $emptyParameters = [System.Array]::CreateInstance($parameterType, 0)
    $executeNonQuery = Get-MethodByArity `
        $sqliteType `
        'ExecuteNonQuery' `
        2 `
        ([System.Reflection.BindingFlags]'Instance,NonPublic')
    $executeScalar = Get-MethodByArity `
        $sqliteType `
        'ExecuteScalar' `
        2 `
        ([System.Reflection.BindingFlags]'Instance,NonPublic')

    function Invoke-SqlNonQuery {
        param([string]$Sql)
        $executeNonQuery.Invoke($sqliteEngine, [object[]]@($Sql, $emptyParameters)) | Out-Null
    }

    function Invoke-SqlScalar {
        param([string]$Sql)
        return $executeScalar.Invoke($sqliteEngine, [object[]]@($Sql, $emptyParameters))
    }

    $saveKey = 'storage_regression'
    $sameTimestamp = '2025-01-02T03:04:05.0000000'
    $preCheckpointSql = @(
        "INSERT INTO event_stream(save_key, game_date_key, game_datetime, idol_id, entity_kind, entity_id, event_type, source_patch, payload_json, namespace_id) VALUES('$saveKey', 20250102, '$sameTimestamp', 7, 'test', 'pre', 'pre_event', 'harness', '{}', 'harness');",
        "INSERT INTO single_participation(save_key, single_id, idol_id, row_index, position_index, is_center, release_date) VALUES('$saveKey', 1, 7, 0, 1, 1, '$sameTimestamp');",
        "INSERT INTO status_window(save_key, idol_id, status_type, start_date, end_date) VALUES('$saveKey', 7, 'active', '$sameTimestamp', NULL);",
        "INSERT INTO show_cast_window(save_key, show_id, idol_id, start_date, end_date, end_reason) VALUES('$saveKey', 'show_pre', 7, '$sameTimestamp', NULL, '');",
        "INSERT INTO contract_window(save_key, contract_key, idol_id, start_date, end_date, end_reason) VALUES('$saveKey', 'contract_pre', 7, '$sameTimestamp', NULL, '');",
        "INSERT INTO relationship_window(save_key, relationship_key, idol_id, relationship_type, start_date, end_date, end_reason) VALUES('$saveKey', 'relationship_pre', 7, 'dating', '$sameTimestamp', NULL, '');",
        "INSERT INTO tour_participation(save_key, tour_id, idol_id, lifecycle_action, event_date) VALUES('$saveKey', 'tour_pre', 7, 'started', '$sameTimestamp');",
        "INSERT INTO award_result_projection(save_key, award_key, idol_id, event_date) VALUES('$saveKey', 'award_pre', 7, '$sameTimestamp');",
        "INSERT INTO election_result_projection(save_key, election_id, idol_id, event_date) VALUES('$saveKey', 'election_pre', 7, '$sameTimestamp');",
        "INSERT INTO push_window(save_key, slot_key, idol_id, start_date, end_date, last_days_in_slot, end_reason) VALUES('$saveKey', 'slot_pre', 7, '$sameTimestamp', NULL, 3, '');"
    )
    foreach ($sql in $preCheckpointSql) {
        Invoke-SqlNonQuery $sql
    }

    $setArguments = [object[]]@($saveKey, 'harness', 'value', '{"generation":1}', $null)
    Assert-True `
        ([bool](Invoke-Public $sqliteEngine 'TrySetCustomData' $setArguments)) `
        ('Pre-checkpoint custom write failed: ' + [string]$setArguments[4])

    $fingerprint = 'v1|length=123|sha256=storage-regression'
    $recordArguments = [object[]]@($saveKey, $fingerprint, $null)
    Assert-True `
        ([bool](Invoke-Public $sqliteEngine 'TryRecordSaveGeneration' $recordArguments)) `
        ('Checkpoint record failed: ' + [string]$recordArguments[2])

    $postCheckpointSql = @(
        "INSERT INTO event_stream(save_key, game_date_key, game_datetime, idol_id, entity_kind, entity_id, event_type, source_patch, payload_json, namespace_id) VALUES('$saveKey', 20250102, '$sameTimestamp', 7, 'test', 'post', 'post_event', 'harness', '{}', 'harness');",
        "UPDATE single_participation SET position_index = 99 WHERE save_key = '$saveKey';",
        "UPDATE status_window SET end_date = '$sameTimestamp' WHERE save_key = '$saveKey';",
        "UPDATE show_cast_window SET end_date = '$sameTimestamp', end_reason = 'post' WHERE save_key = '$saveKey';",
        "DELETE FROM contract_window WHERE save_key = '$saveKey';",
        "UPDATE relationship_window SET end_date = '$sameTimestamp', end_reason = 'post' WHERE save_key = '$saveKey';",
        "DELETE FROM tour_participation WHERE save_key = '$saveKey';",
        "UPDATE award_result_projection SET event_date = '2099-01-01T00:00:00.0000000' WHERE save_key = '$saveKey';",
        "DELETE FROM election_result_projection WHERE save_key = '$saveKey';",
        "UPDATE push_window SET end_date = '$sameTimestamp', last_days_in_slot = 99, end_reason = 'post' WHERE save_key = '$saveKey';"
    )
    foreach ($sql in $postCheckpointSql) {
        Invoke-SqlNonQuery $sql
    }

    $setArguments = [object[]]@($saveKey, 'harness', 'value', '{"generation":2}', $null)
    Assert-True `
        ([bool](Invoke-Public $sqliteEngine 'TrySetCustomData' $setArguments)) `
        ('Post-checkpoint custom write failed: ' + [string]$setArguments[4])
    $setArguments = [object[]]@($saveKey, 'harness', 'post_only', 'true', $null)
    Assert-True `
        ([bool](Invoke-Public $sqliteEngine 'TrySetCustomData' $setArguments)) `
        ('Post-only custom write failed: ' + [string]$setArguments[4])

    $rollbackArguments = [object[]]@($saveKey, $fingerprint, $false, $null)
    Assert-True `
        ([bool](Invoke-Public $sqliteEngine 'TryRollbackToSaveGeneration' $rollbackArguments)) `
        ('Checkpoint rollback failed: ' + [string]$rollbackArguments[3])
    Assert-True ([bool]$rollbackArguments[2]) 'The matching checkpoint was not reported as found.'

    $surfaceChecks = @(
        @("SELECT COUNT(*) FROM event_stream WHERE save_key='$saveKey';", 1, 'event watermark'),
        @("SELECT position_index FROM single_participation WHERE save_key='$saveKey';", 1, 'single upsert restore'),
        @("SELECT end_date IS NULL FROM status_window WHERE save_key='$saveKey';", 1, 'status reopen'),
        @("SELECT end_date IS NULL AND end_reason='' FROM show_cast_window WHERE save_key='$saveKey';", 1, 'show restore'),
        @("SELECT COUNT(*) FROM contract_window WHERE save_key='$saveKey' AND contract_key='contract_pre';", 1, 'contract removal restore'),
        @("SELECT end_date IS NULL AND end_reason='' FROM relationship_window WHERE save_key='$saveKey';", 1, 'relationship restore'),
        @("SELECT COUNT(*) FROM tour_participation WHERE save_key='$saveKey' AND tour_id='tour_pre';", 1, 'tour removal restore'),
        @("SELECT COUNT(*) FROM award_result_projection WHERE save_key='$saveKey' AND event_date='$sameTimestamp';", 1, 'award upsert restore'),
        @("SELECT COUNT(*) FROM election_result_projection WHERE save_key='$saveKey' AND election_id='election_pre';", 1, 'election removal restore'),
        @("SELECT end_date IS NULL AND last_days_in_slot=3 AND end_reason='' FROM push_window WHERE save_key='$saveKey';", 1, 'push restore')
    )
    foreach ($check in $surfaceChecks) {
        $actual = [long](Invoke-SqlScalar ([string]$check[0]))
        Assert-True ($actual -eq [long]$check[1]) ("SQLite $($check[2]) check failed; got $actual.")
    }

    $getArguments = [object[]]@($saveKey, 'harness', 'value', $null, $null)
    Assert-True `
        ([bool](Invoke-Public $sqliteEngine 'TryGetCustomData' $getArguments)) `
        ('Restored custom value was not found: ' + [string]$getArguments[4])
    Assert-True `
        ([string]$getArguments[3] -eq '{"generation":1}') `
        'The custom value was not restored to its checkpoint generation.'
    $getArguments = [object[]]@($saveKey, 'harness', 'post_only', $null, $null)
    Assert-True `
        (-not [bool](Invoke-Public $sqliteEngine 'TryGetCustomData' $getArguments)) `
        'Post-checkpoint custom data survived rollback.'

    # Reusing an identical fingerprint must replace its checkpoint with the latest state.
    $setArguments = [object[]]@($saveKey, 'harness', 'value', '{"generation":3}', $null)
    Assert-True ([bool](Invoke-Public $sqliteEngine 'TrySetCustomData' $setArguments)) 'Generation 3 write failed.'
    $recordArguments = [object[]]@($saveKey, $fingerprint, $null)
    Assert-True ([bool](Invoke-Public $sqliteEngine 'TryRecordSaveGeneration' $recordArguments)) 'Latest checkpoint replacement failed.'
    $setArguments = [object[]]@($saveKey, 'harness', 'value', '{"generation":4}', $null)
    Assert-True ([bool](Invoke-Public $sqliteEngine 'TrySetCustomData' $setArguments)) 'Generation 4 write failed.'
    $rollbackArguments = [object[]]@($saveKey, $fingerprint, $false, $null)
    Assert-True ([bool](Invoke-Public $sqliteEngine 'TryRollbackToSaveGeneration' $rollbackArguments)) 'Second checkpoint rollback failed.'
    $getArguments = [object[]]@($saveKey, 'harness', 'value', $null, $null)
    Assert-True ([bool](Invoke-Public $sqliteEngine 'TryGetCustomData' $getArguments)) 'Latest checkpoint custom value was not found.'
    Assert-True ([string]$getArguments[3] -eq '{"generation":3}') 'Identical fingerprint did not map to the latest checkpoint.'

    # Remapping must carry checkpoint metadata, manifests, snapshots, and live rows.
    $targetSaveKey = 'storage_regression_remapped'
    $remapArguments = [object[]]@($saveKey, $targetSaveKey, $null)
    Assert-True ([bool](Invoke-Public $sqliteEngine 'TryRemapSaveKey' $remapArguments)) 'Save-key remap failed.'
    $setArguments = [object[]]@($targetSaveKey, 'harness', 'value', '{"generation":5}', $null)
    Assert-True ([bool](Invoke-Public $sqliteEngine 'TrySetCustomData' $setArguments)) 'Post-remap mutation failed.'
    $rollbackArguments = [object[]]@($targetSaveKey, $fingerprint, $false, $null)
    Assert-True ([bool](Invoke-Public $sqliteEngine 'TryRollbackToSaveGeneration' $rollbackArguments)) 'Post-remap checkpoint rollback failed.'
    Assert-True ([bool]$rollbackArguments[2]) 'The remapped checkpoint was not found.'
    $getArguments = [object[]]@($targetSaveKey, 'harness', 'value', $null, $null)
    Assert-True ([bool](Invoke-Public $sqliteEngine 'TryGetCustomData' $getArguments)) 'Remapped checkpoint data was not restored.'
    Assert-True ([string]$getArguments[3] -eq '{"generation":3}') 'Remapped checkpoint restored the wrong custom generation.'
    Assert-True `
        ([long](Invoke-SqlScalar "SELECT COUNT(*) FROM custom_data WHERE save_key='$saveKey';") -eq 0L) `
        'Source-key rows remained after remap.'

    # Retain a bounded history: the ninth distinct checkpoint evicts only the
    # oldest, while any of the latest eight can restore custom/projection state.
    for ($generation = 1; $generation -le 9; $generation++) {
        $historyValue = '{"generation":' + $generation + '}'
        $setArguments = [object[]]@(
            $targetSaveKey,
            'harness',
            'history',
            $historyValue,
            $null)
        Assert-True `
            ([bool](Invoke-Public $sqliteEngine 'TrySetCustomData' $setArguments)) `
            "History generation $generation write failed."
        $historyFingerprint =
            'v1|length=123|sha256=history-' + $generation
        $recordArguments = [object[]]@(
            $targetSaveKey,
            $historyFingerprint,
            $null)
        Assert-True `
            ([bool](Invoke-Public $sqliteEngine 'TryRecordSaveGeneration' $recordArguments)) `
            "History generation $generation checkpoint failed."
    }

    Assert-True `
        ([long](Invoke-SqlScalar 'SELECT COUNT(*) FROM storage_save_generation;') -eq 8L) `
        'SQLite did not enforce the eight-generation retention bound.'
    $setArguments = [object[]]@(
        $targetSaveKey,
        'harness',
        'history',
        '{"generation":99}',
        $null)
    Assert-True `
        ([bool](Invoke-Public $sqliteEngine 'TrySetCustomData' $setArguments)) `
        'Post-history mutation failed.'

    $rollbackArguments = [object[]]@(
        $targetSaveKey,
        'v1|length=123|sha256=history-2',
        $false,
        $null)
    Assert-True `
        ([bool](Invoke-Public $sqliteEngine 'TryRollbackToSaveGeneration' $rollbackArguments)) `
        'Old retained checkpoint rollback failed.'
    Assert-True ([bool]$rollbackArguments[2]) `
        'The second-oldest retained generation was not found.'
    $getArguments = [object[]]@(
        $targetSaveKey,
        'harness',
        'history',
        $null,
        $null)
    Assert-True `
        ([bool](Invoke-Public $sqliteEngine 'TryGetCustomData' $getArguments)) `
        'The old retained history value was not found.'
    Assert-True `
        ([string]$getArguments[3] -eq '{"generation":2}') `
        'The old retained checkpoint restored the wrong custom value.'

    $rollbackArguments = [object[]]@(
        $targetSaveKey,
        'v1|length=123|sha256=history-1',
        $false,
        $null)
    Assert-True `
        ([bool](Invoke-Public $sqliteEngine 'TryRollbackToSaveGeneration' $rollbackArguments)) `
        'Evicted-checkpoint lookup returned an engine error.'
    Assert-True (-not [bool]$rollbackArguments[2]) `
        'The ninth checkpoint did not evict the oldest generation.'

    $rollbackArguments = [object[]]@(
        $targetSaveKey,
        'v1|length=123|sha256=history-9',
        $false,
        $null)
    Assert-True `
        ([bool](Invoke-Public $sqliteEngine 'TryRollbackToSaveGeneration' $rollbackArguments)) `
        'Newest retained checkpoint rollback failed after an older rollback.'
    Assert-True ([bool]$rollbackArguments[2]) `
        'The newest retained generation was lost after an older rollback.'
    $getArguments = [object[]]@(
        $targetSaveKey,
        'harness',
        'history',
        $null,
        $null)
    Assert-True `
        ([bool](Invoke-Public $sqliteEngine 'TryGetCustomData' $getArguments)) `
        'The newest retained history value was not found.'
    Assert-True `
        ([string]$getArguments[3] -eq '{"generation":9}') `
        'The newest retained checkpoint restored the wrong custom value.'

    $integrityArguments = [object[]]@($null)
    Assert-True `
        ([bool](Invoke-Public $sqliteEngine 'TryValidateIntegrity' $integrityArguments)) `
        ('SQLite integrity validation failed: ' + [string]$integrityArguments[0])

    # Build one database in the interim single-checkpoint schema, then reopen it
    # through the current engine and verify its exact snapshot is migrated atomically.
    Invoke-Public $sqliteEngine 'Dispose' ([object[]]@()) | Out-Null
    $sqliteEngine = [System.Activator]::CreateInstance($sqliteType, $true)
    $legacyDatabasePath = Join-Path $testRoot 'legacy-checkpoint.db'
    $initializeArguments = [object[]]@($legacyDatabasePath, $null)
    Assert-True `
        ([bool](Invoke-Public $sqliteEngine 'Initialize' $initializeArguments)) `
        ('Legacy migration database initialize failed: ' +
            [string]$initializeArguments[1])

    $legacySaveKey = 'legacy_checkpoint'
    $legacyFingerprint = 'v1|length=88|sha256=legacy-checkpoint'
    Invoke-SqlNonQuery `
        "INSERT INTO custom_data(save_key, namespace_id, data_key, value_json, updated_utc) VALUES('$legacySaveKey', 'harness', 'legacy_value', '{""generation"":1}', '2025-01-02T03:04:05.0000000Z');"
    $legacyMutableTables = @(
        'single_participation',
        'status_window',
        'show_cast_window',
        'contract_window',
        'relationship_window',
        'tour_participation',
        'award_result_projection',
        'election_result_projection',
        'push_window',
        'custom_data'
    )
    foreach ($tableName in $legacyMutableTables) {
        $tableNameHex = -join @(
            [System.Text.Encoding]::UTF8.GetBytes($tableName) |
                ForEach-Object { $_.ToString('x2') }
        )
        $snapshotName = 'storage_save_checkpoint_data_' + $tableNameHex
        Invoke-SqlNonQuery `
            "CREATE TABLE `"$snapshotName`" AS SELECT * FROM `"$tableName`" WHERE 0;"
        Invoke-SqlNonQuery `
            "INSERT INTO `"$snapshotName`" SELECT * FROM `"$tableName`" WHERE save_key='$legacySaveKey';"
        Invoke-SqlNonQuery `
            "INSERT INTO storage_save_checkpoint_table(save_key, table_name, snapshot_table_name) VALUES('$legacySaveKey', '$tableName', '$snapshotName');"
    }
    Invoke-SqlNonQuery `
        "INSERT INTO storage_save_checkpoint(save_key, vanilla_save_fingerprint, event_watermark, mutable_table_count, checkpoint_created_utc) VALUES('$legacySaveKey', '$legacyFingerprint', 0, $($legacyMutableTables.Count), '2025-01-02T03:04:05.0000000Z');"

    Invoke-Public $sqliteEngine 'Dispose' ([object[]]@()) | Out-Null
    $sqliteEngine = [System.Activator]::CreateInstance($sqliteType, $true)
    $initializeArguments = [object[]]@($legacyDatabasePath, $null)
    Assert-True `
        ([bool](Invoke-Public $sqliteEngine 'Initialize' $initializeArguments)) `
        ('Legacy checkpoint migration failed: ' +
            [string]$initializeArguments[1])
    Assert-True `
        ([long](Invoke-SqlScalar 'SELECT COUNT(*) FROM storage_save_generation;') -eq 1L) `
        'The interim single checkpoint was not migrated into history.'
    Assert-True `
        ([long](Invoke-SqlScalar 'SELECT COUNT(*) FROM storage_save_checkpoint;') -eq 0L) `
        'Legacy checkpoint metadata remained after migration.'
    Assert-True `
        ([long](Invoke-SqlScalar "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name LIKE 'storage_save_checkpoint_data_%';") -eq 0L) `
        'Legacy checkpoint snapshot tables remained after migration.'

    $setArguments = [object[]]@(
        $legacySaveKey,
        'harness',
        'legacy_value',
        '{"generation":2}',
        $null)
    Assert-True `
        ([bool](Invoke-Public $sqliteEngine 'TrySetCustomData' $setArguments)) `
        'Post-migration custom mutation failed.'
    $rollbackArguments = [object[]]@(
        $legacySaveKey,
        $legacyFingerprint,
        $false,
        $null)
    Assert-True `
        ([bool](Invoke-Public $sqliteEngine 'TryRollbackToSaveGeneration' $rollbackArguments)) `
        'Migrated checkpoint rollback failed.'
    Assert-True ([bool]$rollbackArguments[2]) `
        'The migrated checkpoint was not found.'
    $getArguments = [object[]]@(
        $legacySaveKey,
        'harness',
        'legacy_value',
        $null,
        $null)
    Assert-True `
        ([bool](Invoke-Public $sqliteEngine 'TryGetCustomData' $getArguments)) `
        'Migrated custom value was not restored.'
    Assert-True `
        ([string]$getArguments[3] -eq '{"generation":1}') `
        'The migrated checkpoint restored the wrong custom value.'

    Write-Host 'PASS: flat atomic replace/backup/failure preservation'
    Write-Host 'PASS: flat legacy root rejection and v1 migration field layout'
    Write-Host 'PASS: SQLite exact checkpoint across event/custom/all projection tables'
    Write-Host 'PASS: identical-fingerprint replacement, save-key remap, and quick_check'
    Write-Host 'PASS: eight-generation history, older restore, and bounded eviction'
    Write-Host 'PASS: interim single-checkpoint schema migration'
}
finally {
    if ($sqliteEngine -ne $null) {
        try {
            Invoke-Public $sqliteEngine 'Dispose' ([object[]]@()) | Out-Null
        }
        catch {
        }
    }

    if ($flatEngine -ne $null) {
        try {
            Invoke-Public $flatEngine 'Dispose' ([object[]]@()) | Out-Null
        }
        catch {
        }
    }

    [System.AppDomain]::CurrentDomain.remove_AssemblyResolve($dependencyResolver)

    if (Test-Path -LiteralPath $testRoot) {
        $normalizedTestRoot = [System.IO.Path]::GetFullPath($testRoot).TrimEnd(
            [System.IO.Path]::DirectorySeparatorChar,
            [System.IO.Path]::AltDirectorySeparatorChar)
        $normalizedTempRoot = [System.IO.Path]::GetFullPath(
            [System.IO.Path]::GetTempPath()).TrimEnd(
                [System.IO.Path]::DirectorySeparatorChar,
                [System.IO.Path]::AltDirectorySeparatorChar)
        $expectedPrefix = $normalizedTempRoot +
            [System.IO.Path]::DirectorySeparatorChar +
            'imdatacore-storage-regression-'
        if (-not $normalizedTestRoot.StartsWith(
            $expectedPrefix,
            [System.StringComparison]::OrdinalIgnoreCase)) {
            throw 'Refused to clean an unexpected storage regression directory.'
        }

        [System.IO.Directory]::Delete($normalizedTestRoot, $true)
    }
}
