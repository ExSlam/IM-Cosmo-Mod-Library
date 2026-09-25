$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$required = @(
  'Directory.Build.props',
  'IMDataCore.DataMigrationTool.sln',
  'src\IMDataCore.DataMigrationTool\IMDataCore.DataMigrationTool.csproj',
  'src\IMDataCore.DataMigrationTool\Assets\data_migration_tool.ico',
  'src\IMDataCore.DataMigrationTool\Assets\data_migration_tool.png',
  'src\IMDataCore.DataMigrationTool\Assets\data_migration_tool_banner.png',
  'src\IMDataCore.DataMigrationTool\Program.cs',
  'src\IMDataCore.DataMigrationTool\Migration\MigrationService.cs',
  'src\IMDataCore.DataMigrationTool\Migration\V6Writer.cs',
  'src\IMDataCore.DataMigrationTool\Migration\VanillaSaveReader.cs',
  'src\IMDataCore.DataMigrationTool\Migration\VanillaReverseMatcher.cs',
  'src\IMDataCore.DataMigrationTool\Migration\LegacyContaminationScanner.cs',
  'src\IMDataCore.DataMigrationTool\Migration\LegacyRepairService.cs',
  'src\IMDataCore.DataMigrationTool\Migration\LegacyCleanupService.cs',
  'src\IMDataCore.DataMigrationTool\Migration\BulkConversionService.cs',
  'src\IMDataCore.DataMigrationTool\Gui\MainForm.cs',
  'src\IMDataCore.DataMigrationTool\Gui\Localization.cs',
  'src\IMDataCore.DataMigrationTool\Gui\LocalizedLogFormatter.cs',
  'src\IMDataCore.DataMigrationTool\Gui\BulkScanForm.cs',
  'src\IMDataCore.DataMigrationTool\Gui\BulkConvertForm.cs',
  'src\IMDataCore.DataMigrationTool\Gui\RepairForm.cs',
  'src\IMDataCore.DataMigrationTool\Gui\UiSettings.cs',
  'src\IMDataCore.DataMigrationTool\Gui\UiStyling.cs',
  'src\IMDataCore.DataMigrationTool\Gui\HelpForm.cs',
  'src\IMDataCore.DataMigrationTool\Gui\HelpContent.cs',
  'docs\REVERSE_MATCHING.md',
  'docs\CONTAMINATION_SCAN.md',
  'docs\BRANCH_REPAIR.md',
  'docs\HELP.md'
)
foreach ($item in $required) {
  $path = Join-Path $root $item
  if (!(Test-Path $path)) { throw "Missing required file: $item" }
}


$toolInfo = Get-Content -Raw (Join-Path $root 'src\IMDataCore.DataMigrationTool\ToolInfo.cs')
$localization = Get-Content -Raw (Join-Path $root 'src\IMDataCore.DataMigrationTool\Gui\Localization.cs')
if ($toolInfo -notmatch 'ProductName\s*=\s*"IMDataCore Data Migration Tool"' -or $toolInfo -notmatch 'Version\s*=\s*"2\.0\.2"') {
  throw 'Product name/version metadata is not IMDataCore Data Migration Tool 2.0.2.'
}
foreach ($needle in @('アイエムデータコア データ移行ツール', 'Инструмент переноса данных Ай-Эм Дата Кор', '아이엠데이터코어 데이터 이전 도구', '偶像经理数据核心迁移工具')) {
  if ($localization -notmatch [regex]::Escape($needle)) { throw "Native-script product localization is missing: $needle" }
}
$oldBrand = 'Time' + 'bridge'
$oldBrandHits = Get-ChildItem $root -Recurse -File -Include *.cs,*.md,*.ps1,*.props,*.csproj,*.sln,*.txt |
  Select-String -Pattern $oldBrand
if ($oldBrandHits) { throw 'Old product branding remains in the source package.' }

$projectPath = Join-Path $root 'src\IMDataCore.DataMigrationTool\IMDataCore.DataMigrationTool.csproj'
$projectText = Get-Content -Raw $projectPath
if ($projectText -notmatch '<AssemblyName>IMDataCore Data Migration Tool</AssemblyName>') { throw 'AssemblyName must produce IMDataCore Data Migration Tool.exe.' }
[xml]$project = Get-Content -Raw $projectPath
[xml]$localProps = Get-Content -Raw (Join-Path $root 'Directory.Build.props')

if ($project.OuterXml -notmatch '<EnableDefaultCompileItems>true</EnableDefaultCompileItems>') {
  throw 'Project must explicitly set EnableDefaultCompileItems=true so Program.cs cannot disappear under parent repo settings.'
}
if ($localProps.OuterXml -notmatch '<EnableDefaultCompileItems>true</EnableDefaultCompileItems>') {
  throw 'Local Directory.Build.props must keep EnableDefaultCompileItems=true.'
}


$saveReader = Get-Content -Raw (Join-Path $root 'src\IMDataCore.DataMigrationTool\Migration\VanillaSaveReader.cs')
$mainForm = Get-Content -Raw (Join-Path $root 'src\IMDataCore.DataMigrationTool\Gui\MainForm.cs')
$cli = Get-Content -Raw (Join-Path $root 'src\IMDataCore.DataMigrationTool\Cli.cs')
foreach ($needle in @('SaveFolderName', 'GroupName', 'Playtime_Seconds', 'LegacyFileSaveKeyCandidate', 'FindLegacySourceCandidates')) {
  if ($saveReader -notmatch [regex]::Escape($needle)) { throw "Vanilla save identity/matching support is missing: $needle" }
}
if ($mainForm -notmatch 'row_save_identity' -or $mainForm -notmatch 'row_likely_legacy') {
  throw 'GUI save identity / legacy match controls are missing.'
}
if ($cli -notmatch 'save-info') { throw 'CLI save-info command is missing.' }
if ($cli -notmatch 'reverse-match') { throw 'CLI reverse-match command is missing.' }
if ($cli -notmatch 'bulk-scan') { throw 'CLI bulk-scan command is missing.' }
if ($cli -notmatch 'repair-analyze' -or $cli -notmatch '--repair-mode') { throw 'CLI branch repair commands are missing.' }
$reverseMatcher = Get-Content -Raw (Join-Path $root 'src\IMDataCore.DataMigrationTool\Migration\VanillaReverseMatcher.cs')
foreach ($needle in @('LegacyAgencySaveKeyCandidate', 'LegacyAgencyFallbackKeyCandidate', 'LegacyFileSaveKeyCandidate', 'file_path_token', 'ReverseMatchScanResult')) {
  if ($reverseMatcher -notmatch [regex]::Escape($needle)) { throw "Reverse matching support is missing: $needle" }
}
if ($mainForm -notmatch 'Likely vanilla save' -and $mainForm -notmatch 'row_reverse_vanilla') { throw 'GUI reverse-match controls are missing.' }


$contaminationScanner = Get-Content -Raw (Join-Path $root 'src\IMDataCore.DataMigrationTool\Migration\LegacyContaminationScanner.cs')
foreach ($needle in @('ScanRoot', 'AnalyzeTimeline', 'WriteCsv', 'WriteJson', 'DivergenceEventId')) {
  if ($contaminationScanner -notmatch [regex]::Escape($needle)) { throw "Bulk contamination scanner support is missing: $needle" }
}
$bulkForm = Get-Content -Raw (Join-Path $root 'src\IMDataCore.DataMigrationTool\Gui\BulkScanForm.cs')
if ($bulkForm -notmatch 'bulk_primary_divergence' -or $bulkForm -notmatch 'ExportCsv') { throw 'GUI bulk contamination scan controls are missing.' }
if ($mainForm -notmatch 'BulkScanForm') { throw 'Main GUI does not expose the bulk contamination scanner.' }


$repairService = Get-Content -Raw (Join-Path $root 'src\IMDataCore.DataMigrationTool\Migration\LegacyRepairService.cs')
foreach ($needle in @('ConservativeBranchSalvage', 'CleanBaseline', 'DirectIdentityMatches', 'RepairDroppedCustomDataCount', 'NormalizeEventDate')) {
  if ($repairService -notmatch [regex]::Escape($needle)) { throw "Branch repair support is missing: $needle" }
}
$repairForm = Get-Content -Raw (Join-Path $root 'src\IMDataCore.DataMigrationTool\Gui\RepairForm.cs')
if ($repairForm -notmatch 'repair_mode_salvage' -or $repairForm -notmatch 'repair_mode_clean') { throw 'GUI branch repair modes are missing.' }
if ($mainForm -notmatch 'RepairForm') { throw 'Main GUI does not expose branch repair.' }
$v6Writer = Get-Content -Raw (Join-Path $root 'src\IMDataCore.DataMigrationTool\Migration\V6Writer.cs')
if ($v6Writer -notmatch 'cosmo.imdatacore.data-migration-tool.branch-repair') { throw 'Repair provenance extension is missing.' }
if ($saveReader -notmatch 'IdolIds' -or $saveReader -notmatch 'StaffIds') { throw 'Vanilla identity sets required for branch scoring are missing.' }


$bulkConvertService = Get-Content -Raw (Join-Path $root 'src\IMDataCore.DataMigrationTool\Migration\BulkConversionService.cs')
$cleanupService = Get-Content -Raw (Join-Path $root 'src\IMDataCore.DataMigrationTool\Migration\LegacyCleanupService.cs')
$bulkConvertForm = Get-Content -Raw (Join-Path $root 'src\IMDataCore.DataMigrationTool\Gui\BulkConvertForm.cs')
foreach ($needle in @('RecycleSuccessfulSources', 'RecycleOrphanedSources', 'IsStrictOrphan', 'recycleAfterSuccess')) {
  if ($bulkConvertService -notmatch [regex]::Escape($needle)) { throw "Bulk cleanup support is missing: $needle" }
}
if ($cleanupService -notmatch 'RecycleOption.SendToRecycleBin') { throw 'Legacy cleanup must use the Windows Recycle Bin.' }
if ($bulkConvertForm -notmatch 'bulk_cleanup_after_success' -or $bulkConvertForm -notmatch 'bulk_cleanup_orphans_button') { throw 'Bulk cleanup GUI controls are missing.' }
$migrationService = Get-Content -Raw (Join-Path $root 'src\IMDataCore.DataMigrationTool\Migration\MigrationService.cs')
if ($migrationService -notmatch 'ValidateWrittenSidecar') { throw 'Post-write sidecar verification is missing.' }

$helpForm = Get-Content -Raw (Join-Path $root 'src\IMDataCore.DataMigrationTool\Gui\HelpForm.cs')
$helpContent = Get-Content -Raw (Join-Path $root 'src\IMDataCore.DataMigrationTool\Gui\HelpContent.cs')
if ($helpForm -notmatch 'CreateScrollablePopupHost' -or $helpForm -notmatch 'ScrollControlIntoView') { throw 'Help popup must use the shared scroll host and contextual topic scrolling.' }
foreach ($needle in @('IndividualMigration', 'BulkScan', 'BulkConvert', 'MatchResolver', 'FutureTail', 'Cleanup', 'Statuses')) {
  if ($helpContent -notmatch [regex]::Escape($needle)) { throw "Help content is missing topic: $needle" }
}
if ($mainForm -notmatch 'HelpForm') { throw 'Main GUI does not expose Help.' }

$program = Get-Content -Raw (Join-Path $root 'src\IMDataCore.DataMigrationTool\Program.cs')
if ($program -notmatch 'static\s+int\s+Main\s*\(\s*string\[\]\s+args\s*\)') {
  throw 'Program.cs does not contain the expected static int Main(string[] args) entry point.'
}

$bad = Get-ChildItem $root -Recurse -File -Include *.cs,*.md,*.ps1,*.props,*.csproj |
  Where-Object { $_.FullName -ne $PSCommandPath } |
  Select-String -Pattern 'TODO_PLACEHOLDER|REPLACE_ME|YOUR_CODE_HERE'
if ($bad) { throw 'Placeholder markers remain in package.' }

Write-Host 'Static package checks passed.'
Write-Host 'Entry point, parent-MSBuild isolation, save identity, matching, contamination scan, responsive UI, branch repair, Recycle-Bin cleanup, and plain-language Help checks passed.'
Write-Host 'This script does not compile the C# project; run Build.ps1 for compiler validation.'
