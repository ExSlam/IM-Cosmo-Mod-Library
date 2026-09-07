using SaveNLoadFixes.Transport;

namespace SaveNLoadFixes
{
    /// <summary>
    /// Reflection-friendly companion progress bridge. IM Data Core uses this only
    /// when Save n Load Fixes is the healthy authoritative SavedData transport.
    /// </summary>
    public static class SaveProgressApi
    {
        public static int Version
        {
            get { return 1; }
        }

        public static bool IsCoordinatorActive
        {
            get { return SaveProgressCoordinator.IsUiOwnerActive; }
        }

        public static bool TryBeginIMDataCorePersistence(
            SaveManager.SavedData savedData,
            out string errorMessage)
        {
            return SaveProgressCoordinator.BeginCompanionPersistence(
                savedData,
                out errorMessage);
        }

        public static bool TryReportIMDataCorePersistenceResult(
            SaveManager.SavedData savedData,
            bool succeeded,
            string detail,
            out string errorMessage)
        {
            return SaveProgressCoordinator.ReportCompanionPersistenceResult(
                savedData,
                succeeded,
                detail,
                out errorMessage);
        }
    }
}
