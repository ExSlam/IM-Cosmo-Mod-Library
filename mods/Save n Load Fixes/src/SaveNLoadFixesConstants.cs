namespace SaveNLoadFixes
{
    internal static class SaveNLoadFixesConstants
    {
        internal const string ModName = "Save n Load Fixes";
        internal const string HarmonyId = "com.cosmo.savenloadfixes";
        internal const string Version = "5.6.0";
        internal const string DevelopmentStage =
            "Task 50 - A23 cumulative foundation; 5.6.0 optional-integration isolation and load-order-safe late binding cumulative over A33.9, research/schema-v5, business fan/payment, TweaksNQoL/BuffMe/TBS/FWS/A35/A34/A33 and earlier repairs";
        internal const string LogPrefix = "[Save n Load Fixes] ";

        internal const string DataSaverSaveMethodName = "saveData";
        internal const string DataSaverLoadMethodName = "loadData";
        internal const int LoadWaitTimeoutMilliseconds = -1;

        internal const int TransportApiVersion = 3;
        internal const string TransportNotActiveMessage =
            "Save n Load Fixes ordered transport is not authoritative because one or more required caller-level patches are unhealthy.";
    }
}
