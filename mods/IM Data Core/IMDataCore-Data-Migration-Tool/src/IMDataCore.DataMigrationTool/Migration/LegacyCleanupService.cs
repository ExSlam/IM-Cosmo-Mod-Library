using Microsoft.VisualBasic.FileIO;

namespace IMDataCore.DataMigrationTool.Migration;

internal sealed class LegacyCleanupItemResult
{
    public string SourcePath { get; init; } = "";
    public bool Success { get; set; }
    public bool AlreadyMissing { get; set; }
    public List<string> RecycledFiles { get; } = new();
    public List<string> RecycledDirectories { get; } = new();
    public string Summary { get; set; } = "";
}

internal sealed class LegacyCleanupBatchResult
{
    public List<LegacyCleanupItemResult> Items { get; } = new();
    public int SourceCount => Items.Count;
    public int SuccessCount => Items.Count(x => x.Success);
    public int FailedCount => Items.Count(x => !x.Success);
    public int RecycledFileCount => Items.Sum(x => x.RecycledFiles.Count);
}

internal static class LegacyCleanupService
{
    public static LegacyCleanupBatchResult RecycleSources(IEnumerable<string> sourcePaths)
    {
        var result = new LegacyCleanupBatchResult();
        foreach (string sourcePath in sourcePaths
                     .Where(x => !string.IsNullOrWhiteSpace(x))
                     .Select(Path.GetFullPath)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var item = new LegacyCleanupItemResult { SourcePath = sourcePath };
            result.Items.Add(item);
            try
            {
                List<string> files = GetSourceFamilyFiles(sourcePath).Where(File.Exists).ToList();
                string? sourceDirectory = Path.GetDirectoryName(sourcePath);
                if (files.Count == 0)
                {
                    item.AlreadyMissing = true;
                    TryRecycleEmptySourceDirectory(sourceDirectory, item);
                    item.Success = true;
                    item.Summary = item.RecycledDirectories.Count > 0
                        ? "Legacy source files were already absent; moved the now-empty legacy save folder to the Windows Recycle Bin."
                        : "Legacy source was already absent; nothing was recycled.";
                    AppLog.Info("Legacy cleanup: source already absent: " + sourcePath);
                    continue;
                }

                foreach (string file in files)
                {
                    FileSystem.DeleteFile(file, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    item.RecycledFiles.Add(file);
                    AppLog.Info("Legacy cleanup moved to Recycle Bin: " + file);
                }

                TryRecycleEmptySourceDirectory(sourceDirectory, item);
                item.Success = true;
                item.Summary = item.RecycledDirectories.Count > 0
                    ? $"Moved {item.RecycledFiles.Count:N0} legacy source file(s) and the now-empty per-save folder to the Windows Recycle Bin."
                    : $"Moved {item.RecycledFiles.Count:N0} legacy source file(s) to the Windows Recycle Bin. The containing folder was kept because it still contains other files or folders.";
            }
            catch (Exception ex)
            {
                item.Success = false;
                item.Summary = item.RecycledFiles.Count > 0
                    ? $"Moved {item.RecycledFiles.Count:N0} file(s) before cleanup failed: {ex.Message}"
                    : ex.Message;
                AppLog.Error("Legacy cleanup failed: " + sourcePath, ex);
            }
        }
        return result;
    }


    private static void TryRecycleEmptySourceDirectory(string? sourceDirectory, LegacyCleanupItemResult item)
    {
        if (string.IsNullOrWhiteSpace(sourceDirectory) || !Directory.Exists(sourceDirectory)) return;

        // Only remove the immediate per-save directory after every remaining entry is gone.
        // This deliberately never removes a non-empty directory or walks upward into the shared
        // legacy `saves` root, so unrelated files/backups cannot be swept up by cleanup.
        if (Directory.EnumerateFileSystemEntries(sourceDirectory).Any())
        {
            AppLog.Info("Legacy cleanup kept non-empty source folder: " + sourceDirectory);
            return;
        }

        FileSystem.DeleteDirectory(sourceDirectory, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
        item.RecycledDirectories.Add(sourceDirectory);
        AppLog.Info("Legacy cleanup moved empty per-save folder to Recycle Bin: " + sourceDirectory);
    }

    public static bool CanRecycleWholeSource(MigrationService migration, MigrationPlan plan, out string reason)
    {
        try
        {
            LegacyInspection inspection = migration.InspectLegacy(plan.SourcePath);
            if (inspection.SaveKeys.Count > 1)
            {
                reason = $"Legacy source cleanup was skipped because this source contains {inspection.SaveKeys.Count:N0} save_key streams. Use Bulk Convert so the Data Migration Tool can verify that every stream in the shared database was handled before recycling the DB.";
                return false;
            }
        }
        catch (Exception ex)
        {
            reason = "Legacy source cleanup was skipped because the Data Migration Tool could not verify whether the source contains multiple save streams: " + ex.Message;
            return false;
        }

        reason = "";
        return true;
    }

    private static IEnumerable<string> GetSourceFamilyFiles(string sourcePath)
    {
        yield return sourcePath;

        if (!sourcePath.EndsWith(".db", StringComparison.OrdinalIgnoreCase)) yield break;
        // SQLite can leave these beside the DB. They belong to the same database state and should
        // never be left behind after the DB itself is intentionally recycled.
        yield return sourcePath + "-wal";
        yield return sourcePath + "-shm";
        yield return sourcePath + "-journal";
    }
}
