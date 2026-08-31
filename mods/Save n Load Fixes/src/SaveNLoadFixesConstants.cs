namespace SaveNLoadFixes
{
    internal static class SaveNLoadFixesConstants
    {
        internal const string ModName = "Save n Load Fixes";
        internal const string HarmonyId = "com.cosmo.savenloadfixes";
        internal const string Version = "0.52.0";
        internal const string DevelopmentStage =
            "Sprint 1D Task 50 - A23 deterministic legacy rival bootstrap cumulative over Task 49";
        internal const string LogPrefix = "[Save n Load Fixes] ";

        internal const string DataSaverSaveMethodName = "saveData";
        internal const string DataSaverLoadMethodName = "loadData";
        internal const int LoadWaitTimeoutMilliseconds = 30000;

        internal const int TransportApiVersion = 1;
        internal const string TransportNotActiveMessage =
            "Save n Load Fixes ordered transport is not authoritative because one or more required caller-level patches are unhealthy.";
    }
}
