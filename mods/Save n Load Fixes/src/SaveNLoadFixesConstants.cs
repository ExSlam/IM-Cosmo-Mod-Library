namespace SaveNLoadFixes
{
    internal static class SaveNLoadFixesConstants
    {
        internal const string ModName = "Save n Load Fixes";
        internal const string HarmonyId = "com.cosmo.savenloadfixes";
        internal const string Version = "5.5.2";
        internal const string DevelopmentStage =
            "A34 monthly transition repair cumulative over A33.1-A33.6 wide numeric continuity and Sprint 1D Task 50 - A23";
        internal const string LogPrefix = "[Save n Load Fixes] ";

        internal const string DataSaverSaveMethodName = "saveData";
        internal const string DataSaverLoadMethodName = "loadData";
        internal const int LoadWaitTimeoutMilliseconds = -1;

        internal const int TransportApiVersion = 3;
        internal const string TransportNotActiveMessage =
            "Save n Load Fixes ordered transport is not authoritative because one or more required caller-level patches are unhealthy.";
    }
}
