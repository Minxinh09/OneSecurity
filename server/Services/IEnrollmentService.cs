using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Services
{
    public interface IEnrollmentService
    {
        Task<EnrollmentToken> GenerateTokenAsync(long assetId, long policyId, long collectorId, int maxUses, string? reason, string? createdBy);
        Task<EnrollmentToken?> ValidateAndConsumeTokenAsync(string token, string hostname, long collectorId);
        Task<IEnumerable<EnrollmentToken>> GetAllTokensAsync();
        Task<EnrollmentToken?> GetTokenDetailsAsync(long id);
    }
}
