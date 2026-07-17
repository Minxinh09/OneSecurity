using System.Collections.Generic;

namespace OneSecurity.Server.DTOs
{
    public class AlertRuleListResponse
    {
        public int TotalItems { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public required List<AlertRuleListItemDto> Items { get; set; }
    }
}
