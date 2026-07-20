namespace OneSecurity.Server.Models.Enums
{
    public enum ResponseActionType
    {
        Shutdown,
        Restart,
        IsolateHost,
        UnisolateHost,
        KillProcess,
        StopService,
        StartService,
        BlockIPAddress,
        UnblockIPAddress,
        RunScript,
        CollectDiagnostics,
        RunScan,
        SyncConfiguration,
        CollectLogs,
        RestartAgent,
        RestartCollector,
        RestartIIS,
        RestartSQL,
        
        // Aliases for Frontend Compatibility
        RestartSqlServer,
        SyncConfig,
        Reboot
    }
}
