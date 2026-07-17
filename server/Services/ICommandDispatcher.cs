using System.Threading.Tasks;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Services
{
    public interface ICommandDispatcher
    {
        Task<bool> DispatchAsync(ResponseAction action);
    }
}
