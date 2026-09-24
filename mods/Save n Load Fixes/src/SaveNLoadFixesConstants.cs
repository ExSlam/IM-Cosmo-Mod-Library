namespace SaveNLoadFixes
{
    internal static class SaveNLoadFixesConstants
    {
        internal const string ModName = "Save n Load Fixes";
        internal const string HarmonyId = "com.cosmo.savenloadfixes";
        internal const string Version = "5.5.4";
        internal const string DevelopmentStage =
            "A35 business-payment widening and TBS Balance Patch 1.0.0 compatibility cumulative over A34/A33 and earlier repairs";
        internal const string LogPrefix = "[Save n Load Fixes] ";

        internal const string DataSaverSaveMethodName = "saveData";
        internal const string DataSaverLoadMethodName = "loadData";
        internal const int LoadWaitTimeoutMilliseconds = -1;

        internal const int TransportApiVersion = 3;
        internal const string TransportNotActiveMessage =
            "Save n Load Fixes ordered transport is not authoritative because one or more required caller-level patches are unhealthy.";
    }
}
