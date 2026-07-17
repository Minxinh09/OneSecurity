using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneSecurity.Collector.Configuration;
using OneSecurity.Collector.DTOs;
using OneSecurity.Collector.Infrastructure;
using OneSecurity.Collector.Services;

namespace OneSecurity.Collector.Controllers
{
    [ApiController]
    [Route("api/v1/collector")]
    public class CollectorController : ControllerBase
    {
        private readonly IValidationService _validationService;
        private readonly INormalizationService _normalizationService;
        private readonly IEnrichmentService _enrichmentService;
        private readonly IDeduplicationService _deduplicationService;
        private readonly IAgentRegistryService _registryService;
        private readonly IBatchQueue _queue;
        private readonly IForwardService _forwardService;
        private readonly IDlqService _dlqService;
        private readonly CollectorOptions _options;
        private readonly ILogger<CollectorController> _logger;
        private readonly HttpClient _httpClient;
        private readonly ICollectorCacheService _cacheService;

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Concurrent.ConcurrentQueue<AgentCommand>> _commandQueues = new();

        public CollectorController(
            IValidationService validationService,
            INormalizationService normalizationService,
            IEnrichmentService enrichmentService,
            IDeduplicationService deduplicationService,
            IAgentRegistryService registryService,
            IBatchQueue queue,
            IForwardService forwardService,
            IDlqService dlqService,
            IOptions<CollectorOptions> options,
            ILogger<CollectorController> logger,
            HttpClient httpClient,
            ICollectorCacheService cacheService)
        {
            _validationService = validationService;
            _normalizationService = normalizationService;
            _enrichmentService = enrichmentService;
            _deduplicationService = deduplicationService;
            _registryService = registryService;
            _queue = queue;
            _forwardService = forwardService;
            _dlqService = dlqService;
            _options = options.Value;
            _logger = logger;
            _httpClient = httpClient;
            _cacheService = cacheService;
            
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri(_options.ServerBaseUrl);
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterAgentRequest request, CancellationToken cancellationToken)
        {
            if (!IsAuthorized()) return Unauthorized("Invalid X-Collector-API-Key.");

            _logger.LogInformation("Received registration request for Hostname: {Hostname}", request.Hostname);

            // Validate enrollment token
            if (string.IsNullOrEmpty(request.EnrollmentToken))
            {
                // Bypassed to allow easy automatic enrollment of multiple agents in dev lab
                /*
                if (!_cacheService.IsHostnameAssigned(request.Hostname, _options.CollectorId))
                {
                    return BadRequest(new { message = "Enrollment token is required for new agent registration." });
                }
                */
            }
            else
            {
                if (!_cacheService.IsTokenValid(request.EnrollmentToken, request.Hostname, _options.CollectorId, out var validationError))
                {
                    return BadRequest(new { message = validationError });
                }
            }

            // Bind collector ID to request
            request.CollectorId = _options.CollectorId;

            var result = await _forwardService.ForwardRegisterAsync(request, cancellationToken);
            if (result.IsSuccess && result.Response != null)
            {
                _registryService.RegisterOrUpdate(result.Response.AgentId, request.Hostname, request.IpAddress, request.OsInfo);
                return Ok(result.Response);
            }

            if (result.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                return Conflict(new { message = result.ErrorMessage ?? "Agent is already registered." });
            }

            return StatusCode((int)result.StatusCode, result.ErrorMessage ?? "Failed to register agent with central server.");
        }

        [HttpPost("heartbeat")]
        public async Task<IActionResult> Heartbeat([FromBody] HeartbeatRequest request)
        {
            if (!IsAuthorized()) return Unauthorized("Invalid X-Collector-API-Key.");

            var (isValid, errMsg) = _validationService.ValidateHeartbeat(request);
            if (!isValid) return BadRequest(errMsg);

            // Validate Agent registration and Collector assignment mismatch
            if (!_cacheService.HasAgent(request.AgentId))
            {
                return BadRequest(new { message = $"Unregistered AgentId: {request.AgentId}" });
            }
            var hostname = _cacheService.GetHostname(request.AgentId);
            if (string.IsNullOrEmpty(hostname) || !_cacheService.IsHostnameAssigned(hostname, _options.CollectorId))
            {
                return BadRequest(new { message = "Collector assignment mismatch." });
            }

            var normalized = _normalizationService.NormalizeHeartbeat(request);

            if (_deduplicationService.IsDuplicate(normalized.MessageId, normalized.AgentId))
            {
                _logger.LogInformation("Rejected duplicate heartbeat from AgentId: {AgentId}", normalized.AgentId);
                return Ok(new HeartbeatResponse { AgentId = normalized.AgentId, Status = "online" });
            }

            string correlationId = Guid.NewGuid().ToString("N");
            var enriched = _enrichmentService.EnrichHeartbeat(normalized, correlationId, "Collector-01");

            _registryService.UpdateLastSeen(normalized.AgentId);

            if (_queue.TryEnqueueHeartbeat(enriched))
            {
                return Ok(new HeartbeatResponse { AgentId = normalized.AgentId, Status = "online" });
            }

            _logger.LogWarning("Heartbeat queue overflowed. Directing payload to DLQ.");
            await _dlqService.EnqueueDeadLetterAsync("Heartbeat", enriched, "Queue size exceeded");
            return StatusCode(429, "Queue full. Message redirected to DLQ.");
        }

        [HttpPost("metrics")]
        public async Task<IActionResult> Metrics([FromBody] MetricRequest request)
        {
            if (!IsAuthorized()) return Unauthorized("Invalid X-Collector-API-Key.");

            var (isValid, errMsg) = _validationService.ValidateMetric(request);
            if (!isValid) return BadRequest(errMsg);

            // Validate Agent registration and Collector assignment mismatch
            if (!_cacheService.HasAgent(request.AgentId))
            {
                return BadRequest(new { message = $"Unregistered AgentId: {request.AgentId}" });
            }
            var hostname = _cacheService.GetHostname(request.AgentId);
            if (string.IsNullOrEmpty(hostname) || !_cacheService.IsHostnameAssigned(hostname, _options.CollectorId))
            {
                return BadRequest(new { message = "Collector assignment mismatch." });
            }

            var normalized = _normalizationService.NormalizeMetric(request);

            if (_deduplicationService.IsDuplicate(normalized.MessageId, normalized.AgentId))
            {
                _logger.LogInformation("Rejected duplicate metrics from AgentId: {AgentId}", normalized.AgentId);
                return Ok(new MetricResponse { AgentId = normalized.AgentId, Timestamp = DateTime.UtcNow });
            }

            string correlationId = Guid.NewGuid().ToString("N");
            var enriched = _enrichmentService.EnrichMetric(normalized, correlationId, "Collector-01");

            if (_queue.TryEnqueueMetric(enriched))
            {
                return Ok(new MetricResponse { AgentId = normalized.AgentId, Timestamp = DateTime.UtcNow });
            }

            _logger.LogWarning("Metrics queue overflowed. Directing payload to DLQ.");
            await _dlqService.EnqueueDeadLetterAsync("Metric", enriched, "Queue size exceeded");
            return StatusCode(429, "Queue full. Message redirected to DLQ.");
        }

        [HttpPost("events")]
        public async Task<IActionResult> Events([FromBody] SecurityEventRequest request)
        {
            if (!IsAuthorized()) return Unauthorized("Invalid X-Collector-API-Key.");

            var (isValid, errMsg) = _validationService.ValidateSecurityEvent(request);
            if (!isValid) return BadRequest(errMsg);

            // Validate Agent registration and Collector assignment mismatch
            if (!_cacheService.HasAgent(request.AgentId))
            {
                return BadRequest(new { message = $"Unregistered AgentId: {request.AgentId}" });
            }
            var hostname = _cacheService.GetHostname(request.AgentId);
            if (string.IsNullOrEmpty(hostname) || !_cacheService.IsHostnameAssigned(hostname, _options.CollectorId))
            {
                return BadRequest(new { message = "Collector assignment mismatch." });
            }

            var normalized = _normalizationService.NormalizeSecurityEvent(request);

            if (_deduplicationService.IsDuplicate(normalized.MessageId, normalized.AgentId))
            {
                _logger.LogInformation("Rejected duplicate security event: {EventId} from AgentId: {AgentId}", normalized.EventId, normalized.AgentId);
                return Ok(new SecurityEventResponse { EventId = normalized.EventId, AgentId = normalized.AgentId, ReceivedAt = DateTime.UtcNow });
            }

            string correlationId = Guid.NewGuid().ToString("N");
            var enriched = _enrichmentService.EnrichSecurityEvent(normalized, correlationId, "Collector-01");

            if (_queue.TryEnqueueSecurityEvent(enriched))
            {
                return Ok(new SecurityEventResponse { EventId = normalized.EventId, AgentId = normalized.AgentId, ReceivedAt = DateTime.UtcNow });
            }

            _logger.LogWarning("Security events queue overflowed. Directing payload to DLQ.");
            await _dlqService.EnqueueDeadLetterAsync("SecurityEvent", enriched, "Queue size exceeded");
            return StatusCode(429, "Queue full. Message redirected to DLQ.");
        }

        [HttpPost("commands")]
        public IActionResult QueueCommand([FromBody] AgentCommand command)
        {
            if (!IsAuthorized()) return Unauthorized("Invalid X-Collector-API-Key.");

            if (command == null || string.IsNullOrEmpty(command.AgentId))
            {
                return BadRequest("Invalid command payload.");
            }

            var queue = _commandQueues.GetOrAdd(command.AgentId, _ => new System.Collections.Concurrent.ConcurrentQueue<AgentCommand>());
            queue.Enqueue(command);

            _logger.LogInformation("Queued command {CommandId} ({ActionType}) for AgentId: {AgentId}", 
                command.CommandId, command.ActionType, command.AgentId);

            return Ok(new { success = true });
        }

        [HttpGet("commands")]
        public IActionResult GetCommand([FromQuery] string agentId)
        {
            if (!IsAuthorized()) return Unauthorized("Invalid X-Collector-API-Key.");

            if (string.IsNullOrEmpty(agentId))
            {
                return BadRequest("Missing agentId parameter.");
            }

            if (_commandQueues.TryGetValue(agentId, out var queue) && queue.TryDequeue(out var command))
            {
                _logger.LogInformation("Delivered command {CommandId} ({ActionType}) to AgentId: {AgentId}", 
                    command.CommandId, command.ActionType, agentId);
                return Ok(command);
            }

            return NoContent();
        }

        public class CommandResultRequest
        {
            public required string CommandId { get; set; }
            public required string Status { get; set; }
            public string? Message { get; set; }
        }

        [HttpPost("commands/result")]
        public async Task<IActionResult> ReportResult([FromBody] CommandResultRequest result, CancellationToken cancellationToken)
        {
            if (!IsAuthorized()) return Unauthorized("Invalid X-Collector-API-Key.");

            if (result == null || string.IsNullOrEmpty(result.CommandId))
            {
                return BadRequest("Invalid result payload.");
            }

            _logger.LogInformation("Relaying result for command {CommandId} to Central Server", result.CommandId);

            try
            {
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(result), System.Text.Encoding.UTF8, "application/json");
                
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/responses/callback");
                request.Headers.Add("X-Api-Key", _options.ApiKey);
                request.Content = content;

                var response = await _httpClient.SendAsync(request, cancellationToken);
                
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return Ok(new { success = true });
                }

                _logger.LogError("Server rejected command result callback. Status: {StatusCode}, Response: {Msg}", 
                    response.StatusCode, responseBody);
                return StatusCode((int)response.StatusCode, responseBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to relay command result callback to Central Server.");
                return StatusCode(502, "Error forwarding result to central server: " + ex.Message);
            }
        }

        private bool IsAuthorized()
        {
            if (Request.Headers.TryGetValue("X-Collector-API-Key", out var key1))
            {
                return key1.ToString() == _options.ApiKey;
            }
            if (Request.Headers.TryGetValue("X-Api-Key", out var key2))
            {
                return key2.ToString() == _options.ApiKey;
            }
            return false;
        }
    }
}
