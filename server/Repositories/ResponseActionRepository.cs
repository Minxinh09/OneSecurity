using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneSecurity.Server.Data;
using OneSecurity.Server.Models;
using OneSecurity.Server.Models.Enums;

namespace OneSecurity.Server.Repositories
{
    public class ResponseActionRepository : IResponseActionRepository
    {
        private readonly LocalAgentDbContext _context;

        public ResponseActionRepository(LocalAgentDbContext context)
        {
            _context = context;
        }

        public async Task<ResponseAction?> GetByIdAsync(long id)
        {
            return await _context.ResponseActions
                .IgnoreQueryFilters()
                .Include(r => r.Incident)
                .Include(r => r.Agent)
                .Include(r => r.RequestedByUser)
                .Include(r => r.ApprovedByUser)
                .Include(r => r.Hospital)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<List<ResponseAction>> GetByAgentIdAsync(string agentId)
        {
            return await _context.ResponseActions
                .IgnoreQueryFilters()
                .Include(r => r.Agent)
                .Include(r => r.RequestedByUser)
                .Include(r => r.Hospital)
                .Where(r => r.AgentId == agentId)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task<List<ResponseAction>> GetPendingActionsAsync()
        {
            return await _context.ResponseActions
                .Include(r => r.Agent)
                .Include(r => r.RequestedByUser)
                .Include(r => r.Hospital)
                .Where(r => r.Status == ResponseStatus.Pending)
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task<(List<ResponseAction> Items, int TotalCount)> GetPagedAsync(
            int page, 
            int pageSize, 
            string? status = null, 
            string? actionType = null, 
            string? agentId = null,
            string? requestedBy = null)
        {
            var query = _context.ResponseActions
                .Include(r => r.Agent)
                .Include(r => r.RequestedByUser)
                .Include(r => r.ApprovedByUser)
                .Include(r => r.Hospital)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<ResponseStatus>(status, true, out var statusEnum))
                {
                    query = query.Where(r => r.Status == statusEnum);
                }
            }

            if (!string.IsNullOrEmpty(actionType))
            {
                if (Enum.TryParse<ResponseActionType>(actionType, true, out var typeEnum))
                {
                    query = query.Where(r => r.ActionType == typeEnum);
                }
            }

            if (!string.IsNullOrEmpty(agentId))
            {
                query = query.Where(r => r.AgentId == agentId);
            }

            if (!string.IsNullOrEmpty(requestedBy))
            {
                query = query.Where(r => r.RequestedByUser != null && r.RequestedByUser.UserName == requestedBy);
            }

            var totalCount = await query.CountAsync();
            
            var items = await query
                .OrderByDescending(r => r.RequestedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task AddAsync(ResponseAction action)
        {
            if (action.Agent != null && _context.Entry(action.Agent).State == EntityState.Detached)
            {
                _context.Attach(action.Agent);
            }
            if (action.Incident != null && _context.Entry(action.Incident).State == EntityState.Detached)
            {
                _context.Attach(action.Incident);
            }
            await _context.ResponseActions.AddAsync(action);
        }

        public void Update(ResponseAction action)
        {
            _context.ResponseActions.Update(action);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
