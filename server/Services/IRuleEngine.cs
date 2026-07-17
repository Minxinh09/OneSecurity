using System.Threading.Tasks;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Services
{
    public interface IRuleEngine
    {
        Task<RuleEvaluationResult> EvaluateAsync(RuleEvaluationContext context, AlertRule rule);
    }
}
