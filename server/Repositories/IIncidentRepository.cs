using System;
using System.Threading.Tasks;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Repositories
{
    public interface IIncidentRepository
    {
        Task<IncidentListResponse> GetPagedAsync(
            int page, 
            int pageSize, 
            string? status = null, 
            string? severity = null, 
            string? assignedUserId = null, 
            string? searchQuery = null);
            
        Task<Incident?> GetByIdAsync(long id);
        Task AddAsync(Incident incident);
        void Update(Incident incident);
        void Delete(Incident incident);
        Task SaveChangesAsync();
    }
}
