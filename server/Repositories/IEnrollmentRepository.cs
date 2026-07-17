using System.Collections.Generic;
using System.Threading.Tasks;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Repositories
{
    public interface IEnrollmentRepository
    {
        Task<EnrollmentToken?> GetByIdAsync(long id);
        Task<EnrollmentToken?> GetByTokenAsync(string token);
        Task<IEnumerable<EnrollmentToken>> GetAllAsync();
        Task AddAsync(EnrollmentToken enrollmentToken);
        void Update(EnrollmentToken enrollmentToken);
        Task<int> SaveChangesAsync();
    }
}
