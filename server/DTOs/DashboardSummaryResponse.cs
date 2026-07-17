namespace OneSecurity.Server.DTOs
{
    public class DashboardSummaryResponse
    {
        public int TotalAgents { get; set; }
        public int OnlineAgents { get; set; }
        public int OfflineAgents { get; set; }
        public int TotalEvents { get; set; }
        public int TotalAlerts { get; set; }
        public int UnresolvedAlerts { get; set; }
    }
}
