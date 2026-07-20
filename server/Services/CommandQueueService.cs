using System.Threading.Tasks;
using OneSecurity.Server.Models;
using OneSecurity.Server.Repositories;

namespace OneSecurity.Server.Services
{
    public class CommandQueueService : ICommandQueueService
    {
        private readonly IResponseActionRepository _repository;

        public CommandQueueService(IResponseActionRepository repository)
        {
            _repository = repository;
        }

        public async Task<ResponseAction?> GetNextCommandAsync(string agentId)
        {
            if (string.IsNullOrEmpty(agentId))
            {
                return null;
            }

            // Gọi xuống phương thức Repository đã được tối ưu hóa ở Step 2
            return await _repository.GetNextPendingActionAsync(agentId);
        }
    }
}