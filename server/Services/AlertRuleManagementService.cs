using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Models;
using OneSecurity.Server.Repositories;

namespace OneSecurity.Server.Services
{
    public class AlertRuleManagementService : IAlertRuleManagementService
    {
        private readonly IAlertRuleRepository _ruleRepository;
        private readonly IRuleCacheService _ruleCacheService;

        public AlertRuleManagementService(IAlertRuleRepository ruleRepository, IRuleCacheService ruleCacheService)
        {
            _ruleRepository = ruleRepository;
            _ruleCacheService = ruleCacheService;
        }

        public async Task<AlertRuleListResponse> GetPagedAsync(AlertRuleFilterRequest filter)
        {
            var totalItems = await _ruleRepository.CountAsync(filter.Name, filter.IsEnabled);
            var items = await _ruleRepository.GetPagedAsync(filter.Name, filter.IsEnabled, filter.Page, filter.PageSize);

            var dtos = items.Select(r => new AlertRuleListItemDto
            {
                Id = r.Id,
                Name = r.Name,
                EventType = r.EventType,
                AlertSeverity = r.AlertSeverity,
                IsEnabled = r.IsEnabled,
                Priority = r.Priority,
                Category = r.Category,
                Version = r.Version,
                UpdatedAt = r.UpdatedAt
            }).ToList();

            return new AlertRuleListResponse
            {
                TotalItems = totalItems,
                Page = filter.Page,
                PageSize = filter.PageSize,
                Items = dtos
            };
        }

        public async Task<AlertRuleDetailDto?> GetDetailAsync(long id)
        {
            var rule = await _ruleRepository.GetByIdAsync(id);
            if (rule == null)
            {
                return null;
            }

            return MapToDetailDto(rule);
        }

        public async Task<AlertRuleDetailDto?> CreateAsync(CreateAlertRuleRequest request)
        {
            // 1. Kiểm tra trùng Name
            var nameExists = await _ruleRepository.ExistsNameAsync(request.Name, null);
            if (nameExists)
            {
                throw new InvalidOperationException($"AlertRule with name '{request.Name}' already exists.");
            }

            // 2. Kiểm tra ConditionExpression là JSON hợp lệ
            ValidateJsonExpression(request.ConditionExpression);

            // 3. Khởi tạo đối tượng Entity
            var rule = new AlertRule
            {
                Name = request.Name,
                EventType = request.EventType,
                ConditionExpression = request.ConditionExpression,
                AlertSeverity = request.AlertSeverity,
                IsEnabled = request.IsEnabled,
                TelegramChatId = request.TelegramChatId,
                Priority = request.Priority,
                Category = request.Category,
                Version = request.Version,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // 4. Lưu CSDL
            await _ruleRepository.AddAsync(rule);
            await _ruleRepository.SaveChangesAsync();
            await _ruleCacheService.ReloadAsync();

            return MapToDetailDto(rule);
        }

        public async Task<AlertRuleDetailDto?> UpdateAsync(long id, UpdateAlertRuleRequest request)
        {
            // 1. Kiểm tra tồn tại
            var rule = await _ruleRepository.GetByIdAsync(id);
            if (rule == null)
            {
                return null;
            }

            // 2. Kiểm tra trùng Name (loại trừ chính bản ghi đang sửa)
            var nameExists = await _ruleRepository.ExistsNameAsync(request.Name, id);
            if (nameExists)
            {
                throw new InvalidOperationException($"AlertRule with name '{request.Name}' already exists.");
            }

            // 3. Kiểm tra ConditionExpression là JSON hợp lệ
            ValidateJsonExpression(request.ConditionExpression);

            // 4. Cập nhật các thuộc tính
            rule.Name = request.Name;
            rule.EventType = request.EventType;
            rule.ConditionExpression = request.ConditionExpression;
            rule.AlertSeverity = request.AlertSeverity;
            rule.IsEnabled = request.IsEnabled;
            rule.TelegramChatId = request.TelegramChatId;
            rule.Priority = request.Priority;
            rule.Category = request.Category;
            rule.Version = request.Version;
            rule.UpdatedAt = DateTime.UtcNow;

            // 5. Lưu CSDL
            _ruleRepository.Update(rule);
            await _ruleRepository.SaveChangesAsync();
            await _ruleCacheService.ReloadAsync();

            return MapToDetailDto(rule);
        }

        public async Task<bool> EnableAsync(long id)
        {
            var rule = await _ruleRepository.GetByIdAsync(id);
            if (rule == null)
            {
                return false;
            }

            rule.IsEnabled = true;
            rule.UpdatedAt = DateTime.UtcNow;

            _ruleRepository.Update(rule);
            await _ruleRepository.SaveChangesAsync();
            await _ruleCacheService.ReloadAsync();
            return true;
        }

        public async Task<bool> DisableAsync(long id)
        {
            var rule = await _ruleRepository.GetByIdAsync(id);
            if (rule == null)
            {
                return false;
            }

            rule.IsEnabled = false;
            rule.UpdatedAt = DateTime.UtcNow;

            _ruleRepository.Update(rule);
            await _ruleRepository.SaveChangesAsync();
            await _ruleCacheService.ReloadAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var rule = await _ruleRepository.GetByIdAsync(id);
            if (rule == null)
            {
                return false;
            }

            _ruleRepository.Delete(rule);
            await _ruleRepository.SaveChangesAsync();
            await _ruleCacheService.ReloadAsync();
            return true;
        }

        private void ValidateJsonExpression(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
            }
            catch (JsonException)
            {
                throw new ArgumentException("ConditionExpression must be a valid JSON string.");
            }
        }

        private AlertRuleDetailDto MapToDetailDto(AlertRule rule)
        {
            return new AlertRuleDetailDto
            {
                Id = rule.Id,
                Name = rule.Name,
                EventType = rule.EventType,
                ConditionExpression = rule.ConditionExpression,
                AlertSeverity = rule.AlertSeverity,
                IsEnabled = rule.IsEnabled,
                TelegramChatId = rule.TelegramChatId,
                Priority = rule.Priority,
                Category = rule.Category,
                Version = rule.Version,
                CreatedAt = rule.CreatedAt,
                UpdatedAt = rule.UpdatedAt
            };
        }
    }
}
