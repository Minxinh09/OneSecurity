using System.Collections.Generic;

namespace OneSecurity.Server.DTOs
{
    public class AlertListResponse
    {
        public int TotalItems { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public required List<AlertListItemDto> Items { get; set; }
    }
}
