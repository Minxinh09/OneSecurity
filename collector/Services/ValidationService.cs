using System;
using System.Text.RegularExpressions;
using OneSecurity.Collector.DTOs;

namespace OneSecurity.Collector.Services
{
    public interface IValidationService
    {
        (bool IsValid, string? ErrorMessage) ValidateHeartbeat(HeartbeatRequest request);
        (bool IsValid, string? ErrorMessage) ValidateMetric(MetricRequest request);
        (bool IsValid, string? ErrorMessage) ValidateSecurityEvent(SecurityEventRequest request);
    }

    public class ValidationService : IValidationService
    {
        private static readonly Regex AgentIdRegex = new Regex(@"^[a-zA-Z0-9_\-]+$", RegexOptions.Compiled);

        public (bool IsValid, string? ErrorMessage) ValidateHeartbeat(HeartbeatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.AgentId) || !AgentIdRegex.IsMatch(request.AgentId))
            {
                return (false, "Invalid AgentId format. Allowed: alphanumeric, hyphens, and underscores.");
            }

            if (!string.IsNullOrWhiteSpace(request.Timestamp))
            {
                var (isValidTime, timeErr) = ValidateTimestamp(request.Timestamp);
                if (!isValidTime) return (false, timeErr);
            }

            return (true, null);
        }

        public (bool IsValid, string? ErrorMessage) ValidateMetric(MetricRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.AgentId) || !AgentIdRegex.IsMatch(request.AgentId))
            {
                return (false, "Invalid AgentId format.");
            }

            if (request.CpuUsagePercent < 0 || request.CpuUsagePercent > 100)
            {
                return (false, "CpuUsagePercent must be between 0 and 100.");
            }

            if (request.RamUsagePercent < 0 || request.RamUsagePercent > 100)
            {
                return (false, "RamUsagePercent must be between 0 and 100.");
            }

            if (request.DiskUsagePercent < 0 || request.DiskUsagePercent > 100)
            {
                return (false, "DiskUsagePercent must be between 0 and 100.");
            }

            if (request.NetworkInBytes < 0 || request.NetworkOutBytes < 0)
            {
                return (false, "Network bytes usage must be positive.");
            }

            if (!string.IsNullOrWhiteSpace(request.Timestamp))
            {
                var (isValidTime, timeErr) = ValidateTimestamp(request.Timestamp);
                if (!isValidTime) return (false, timeErr);
            }

            return (true, null);
        }

        public (bool IsValid, string? ErrorMessage) ValidateSecurityEvent(SecurityEventRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.AgentId) || !AgentIdRegex.IsMatch(request.AgentId))
            {
                return (false, "Invalid AgentId format.");
            }

            if (string.IsNullOrWhiteSpace(request.EventId))
            {
                return (false, "EventId is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return (false, "Title is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Details))
            {
                return (false, "Details field is required.");
            }

            string severity = request.Severity?.ToLower() ?? "";
            if (severity != "info" && severity != "warning" && severity != "critical")
            {
                return (false, "Severity must be 'info', 'warning', or 'critical'.");
            }

            if (!string.IsNullOrWhiteSpace(request.Timestamp))
            {
                var (isValidTime, timeErr) = ValidateTimestamp(request.Timestamp);
                if (!isValidTime) return (false, timeErr);
            }

            return (true, null);
        }

        private (bool IsValid, string? ErrorMessage) ValidateTimestamp(string timestampStr)
        {
            if (DateTimeOffset.TryParse(timestampStr, out var parsedOffset))
            {
                var utcTimestamp = parsedOffset.UtcDateTime;
                var now = DateTime.UtcNow;
                var diff = utcTimestamp - now;

                if (diff.TotalMinutes > 5)
                {
                    return (false, "Timestamp is too far in the future (max 5 minutes).");
                }

                if (diff.TotalHours < -24)
                {
                    return (false, "Timestamp is too old (max 24 hours).");
                }

                return (true, null);
            }

            return (false, "Timestamp is in an invalid format.");
        }
    }
}
