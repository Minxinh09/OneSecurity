using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OneSecurity.Server.DTOs;

namespace OneSecurity.Server.Services
{
    public interface ITimelineService
    {
        Task<List<TimelineItemDto>> GetUnifiedTimelineAsync(DateTime? from = null);
    }
}
