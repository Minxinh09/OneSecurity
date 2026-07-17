using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Http; // <--- Thêm namespace này để dùng IHttpContextAccessor
using OneSecurity.Server.Models;
using OneSecurity.Server.Services;

namespace OneSecurity.Server.Data
{
    public class LocalAgentDbContext : IdentityDbContext<ApplicationUser>
    {
        // ==========================================
        // Cấu hình Phân Quyền & Lọc Bệnh Viện
        // ==========================================
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private readonly IHospitalHierarchyCache? _hierarchyCache;
        private List<int>? _permittedHospitalIdsCached;
        private bool _permittedHospitalIdsLoaded = false;

        // Filter override parameter dùng cho các background tasks hoặc SignalR broadcasts
        public List<int>? FilterOverride { get; set; }

        // Constructor nhận thêm IHttpContextAccessor thông qua DI
        public LocalAgentDbContext(
            DbContextOptions<LocalAgentDbContext> options,
            IHttpContextAccessor? httpContextAccessor = null,
            IHospitalHierarchyCache? hierarchyCache = null) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
            _hierarchyCache = hierarchyCache;
        }

        public DbSet<Agent> Agents => Set<Agent>();
        public DbSet<AgentConfig> AgentConfigs => Set<AgentConfig>();
        public DbSet<MetricRecord> MetricRecords => Set<MetricRecord>();
        public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();
        public DbSet<AlertRule> AlertRules => Set<AlertRule>();
        public DbSet<Alert> Alerts => Set<Alert>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<Incident> Incidents => Set<Incident>();
        public DbSet<ResponseAction> ResponseActions => Set<ResponseAction>();
        public DbSet<CollectorNode> CollectorNodes => Set<CollectorNode>();
        public DbSet<AgentPolicy> AgentPolicies => Set<AgentPolicy>();
        public DbSet<InfrastructureAsset> InfrastructureAssets => Set<InfrastructureAsset>();
        public DbSet<EnrollmentToken> EnrollmentTokens => Set<EnrollmentToken>();
        public DbSet<Hospital> Hospitals => Set<Hospital>();

        // Helper property được gọi tự động mỗi khi EF Core biên dịch câu lệnh SQL Query
        public List<int>? PermittedHospitalIds
        {
            get
            {
                if (FilterOverride != null) return FilterOverride;
                if (!_permittedHospitalIdsLoaded)
                {
                    _permittedHospitalIdsCached = ResolvePermittedHospitalIds();
                    _permittedHospitalIdsLoaded = true;
                }
                return _permittedHospitalIdsCached;
            }
        }

        public string? CurrentUserId
        {
            get
            {
                var httpContext = _httpContextAccessor?.HttpContext;
                return httpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            }
        }

        // Phân tích quyền hạn của Client hiện tại dựa trên JWT Token Claims
        private List<int>? ResolvePermittedHospitalIds()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null || httpContext.User?.Identity?.IsAuthenticated != true || _hierarchyCache == null)
            {
                return null; // Không có context/chưa đăng nhập hoặc không có cache -> Không lọc dữ liệu
            }

            var user = httpContext.User;
            if (user.IsInRole("Administrator") || user.IsInRole("SuperAdmin"))
            {
                // Admin/SuperAdmin: Hỗ trợ giả lập dữ liệu của một nhánh bệnh viện từ Dropdown Header gửi lên
                if (httpContext.Request.Headers.TryGetValue("X-Hospital-Id", out var selectedHospIdStr) &&
                    int.TryParse(selectedHospIdStr, out var selectedHospId))
                {
                    return _hierarchyCache.GetDescendantIds(selectedHospId);
                }
                return null; // Không chọn giả lập -> Xem toàn bộ hệ thống
            }

            // Đối với User thường: Đọc claim "hospitalId" (đồng bộ chữ thường như token sinh ra)
            var hospitalIdStr = user.FindFirst("hospitalId")?.Value;
            if (string.IsNullOrEmpty(hospitalIdStr) || !int.TryParse(hospitalIdStr, out var hospitalId))
            {
                return new List<int>(); // Tài khoản không có claim hợp lệ -> Trả về danh sách rỗng để chặn dữ liệu
            }

            return _hierarchyCache.GetDescendantIds(hospitalId);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Hospital>(entity =>
            {
                entity.ToTable("hospitals");

                entity.HasKey(h => h.Id);

                entity.Property(h => h.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(h => h.Code)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.HasOne(h => h.Parent)
                    .WithMany(h => h.Children)
                    .HasForeignKey(h => h.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);
            }
            );

            // ==========================================
            // Cấu hình Bảng: AgentConfigs
            // ==========================================
            modelBuilder.Entity<AgentConfig>(entity =>
            {
                entity.ToTable("agent_configs");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
                entity.Property(c => c.MonitoredLogsConfig).IsRequired();
                entity.Property(c => c.IsDefault).IsRequired();
                entity.HasIndex(c => c.Name).IsUnique();
            });

            // ==========================================
            // Cấu hình Bảng: Agents
            // ==========================================
            modelBuilder.Entity<Agent>(entity =>
            {
                entity.ToTable("agents");
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Id).HasMaxLength(36); // UUID string
                entity.Property(a => a.Hostname).IsRequired().HasMaxLength(255);
                entity.Property(a => a.IpAddress).IsRequired().HasMaxLength(45);
                entity.Property(a => a.Status).IsRequired().HasMaxLength(20);

                entity.HasOne(a => a.Config)
                    .WithMany(c => c.Agents)
                    .HasForeignKey(a => a.ConfigId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Asset)
                    .WithMany(asst => asst.Agents)
                    .HasForeignKey(a => a.AssetId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(a => a.Status);
                entity.HasIndex(a => a.LastSeenAt);
            });

            // ==========================================
            // Cấu hình Bảng: MetricRecords
            // ==========================================
            modelBuilder.Entity<MetricRecord>(entity =>
            {
                entity.ToTable("metric_records");
                entity.HasKey(m => m.Id);
                entity.Property(m => m.AgentId).HasMaxLength(36);
                entity.Property(m => m.CpuUsagePercent).HasPrecision(5, 2);
                entity.Property(m => m.RamUsagePercent).HasPrecision(5, 2);
                entity.Property(m => m.DiskUsagePercent).HasPrecision(5, 2);

                entity.HasOne(m => m.Agent)
                    .WithMany(a => a.MetricRecords)
                    .HasForeignKey(m => m.AgentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(m => new { m.AgentId, m.Timestamp })
                    .HasDatabaseName("IX_metric_records_agent_timestamp_desc");
                entity.HasIndex(m => m.Timestamp);
            });

            // ==========================================
            // Cấu hình Bảng: SecurityEvents
            // ==========================================
            modelBuilder.Entity<SecurityEvent>(entity =>
            {
                entity.ToTable("security_events");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EventId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.AgentId).HasMaxLength(36);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Severity).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Source).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Details).IsRequired();
                entity.Property(e => e.RawData).IsRequired();

                entity.HasOne(e => e.Agent)
                    .WithMany(a => a.SecurityEvents)
                    .HasForeignKey(e => e.AgentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.AgentId, e.Timestamp })
                    .HasDatabaseName("IX_security_events_agent_timestamp_desc");
                entity.HasIndex(e => e.EventId).IsUnique();
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.Severity);
            });

            // ==========================================
            // Cấu hình Bảng: AlertRules
            // ==========================================
            modelBuilder.Entity<AlertRule>(entity =>
            {
                entity.ToTable("alert_rules");
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Name).IsRequired().HasMaxLength(100);
                entity.Property(r => r.AlertSeverity).IsRequired().HasMaxLength(20);
                entity.Property(r => r.ConditionExpression).IsRequired();
                entity.HasIndex(r => r.Name).IsUnique();
                entity.HasIndex(r => new { r.IsEnabled, r.EventType });
            });

            // ==========================================
            // Cấu hình Bảng: Alerts
            // ==========================================
            modelBuilder.Entity<Alert>(entity =>
            {
                entity.ToTable("alerts");
                entity.HasKey(al => al.Id);
                entity.Property(al => al.AgentId).HasMaxLength(36);
                entity.Property(al => al.RuleName).IsRequired().HasMaxLength(100);
                entity.Property(al => al.Title).IsRequired().HasMaxLength(255);
                entity.Property(al => al.Message).IsRequired();
                entity.Property(al => al.Severity).IsRequired().HasMaxLength(20);
                entity.Property(al => al.Category).IsRequired().HasMaxLength(100);

                entity.HasOne(al => al.Agent)
                    .WithMany(a => a.Alerts)
                    .HasForeignKey(al => al.AgentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(al => al.Rule)
                    .WithMany(r => r.Alerts)
                    .HasForeignKey(al => al.RuleId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(al => al.TriggerEvent)
                    .WithMany()
                    .HasForeignKey(al => al.TriggerEventId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(al => al.CreatedAt);
            });

            // ==========================================
            // Cấu hình Bảng: AuditLogs
            // ==========================================
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("audit_logs");
                entity.HasKey(a => a.Id);

                // Enum conversions
                entity.Property(x => x.Severity)
                    .HasConversion<string>();

                entity.Property(x => x.ResourceType)
                    .HasConversion<string>();

                // Single indices
                entity.HasIndex(a => a.TimestampUtc);
                entity.HasIndex(a => a.UserId);
                entity.HasIndex(a => a.Severity);
                entity.HasIndex(a => a.Action);
                entity.HasIndex(a => a.ResourceType);
                entity.HasIndex(a => a.Success);

                // Composite index
                entity.HasIndex(a => new { a.TimestampUtc, a.Severity })
                    .HasDatabaseName("IX_audit_logs_TimestampUtc_Severity");
            });

            // ==========================================
            // Cấu hình Bảng: Incidents
            // ==========================================
            modelBuilder.Entity<Incident>(entity =>
            {
                entity.ToTable("incidents");
                entity.HasKey(i => i.Id);
                entity.Property(i => i.Title).IsRequired().HasMaxLength(256);
                entity.Property(i => i.Description).IsRequired().HasMaxLength(1000);

                entity.Property(i => i.Status)
                    .HasConversion<string>();

                entity.Property(i => i.Severity)
                    .HasConversion<string>();

                entity.HasOne(i => i.AssignedUser)
                    .WithMany()
                    .HasForeignKey(i => i.AssignedUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(i => i.CreatedBy)
                    .WithMany()
                    .HasForeignKey(i => i.CreatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(i => i.Alerts)
                    .WithOne(a => a.Incident)
                    .HasForeignKey(a => a.IncidentId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Indexes
                entity.HasIndex(i => i.Status);
                entity.HasIndex(i => i.Severity);
                entity.HasIndex(i => i.AssignedUserId);
                entity.HasIndex(i => i.CreatedAt);
            });

            // ==========================================
            // Cấu hình Bảng: ResponseActions
            // ==========================================
            modelBuilder.Entity<ResponseAction>(entity =>
            {
                entity.ToTable("response_actions");
                entity.HasKey(ra => ra.Id);
                
                entity.Property(ra => ra.ActionType)
                    .HasConversion<string>();
                
                entity.Property(ra => ra.Status)
                    .HasConversion<string>();
                
                entity.HasOne(ra => ra.Incident)
                    .WithMany()
                    .HasForeignKey(ra => ra.IncidentId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(ra => ra.Agent)
                    .WithMany()
                    .HasForeignKey(ra => ra.AgentId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(ra => ra.RequestedByUser)
                    .WithMany()
                    .HasForeignKey(ra => ra.RequestedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(ra => ra.ApprovedByUser)
                    .WithMany()
                    .HasForeignKey(ra => ra.ApprovedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ra => ra.Hospital)
                    .WithMany()
                    .HasForeignKey(ra => ra.HospitalId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // Cấu hình Bảng: CollectorNode
            // ==========================================
            modelBuilder.Entity<CollectorNode>(entity =>
            {
                entity.ToTable("collector_nodes");
                entity.HasKey(c => c.Id);
                entity.HasIndex(c => c.CollectorKey).IsUnique();
            });

            // ==========================================
            // Cấu hình Bảng: AgentPolicy
            // ==========================================
            modelBuilder.Entity<AgentPolicy>(entity =>
            {
                entity.ToTable("agent_policies");
                entity.HasKey(p => p.Id);
            });

            // ==========================================
            // Cấu hình Bảng: InfrastructureAsset
            // ==========================================
            modelBuilder.Entity<InfrastructureAsset>(entity =>
            {
                entity.ToTable("infrastructure_assets");
                entity.HasKey(ia => ia.Id);
                entity.HasIndex(ia => ia.Hostname).IsUnique();

                entity.HasOne(ia => ia.Collector)
                    .WithMany()
                    .HasForeignKey(ia => ia.CollectorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ia => ia.Policy)
                    .WithMany()
                    .HasForeignKey(ia => ia.PolicyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // Cấu hình Bảng: EnrollmentToken
            // ==========================================
            modelBuilder.Entity<EnrollmentToken>(entity =>
            {
                entity.ToTable("enrollment_tokens");
                entity.HasKey(et => et.Id);
                entity.HasIndex(et => et.Token).IsUnique();

                entity.HasOne(et => et.Asset)
                    .WithMany()
                    .HasForeignKey(et => et.AssetId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(et => et.Policy)
                    .WithMany()
                    .HasForeignKey(et => et.PolicyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(et => et.Collector)
                    .WithMany()
                    .HasForeignKey(et => et.CollectorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =======================================================
            // ĐĂNG KÝ EF CORE GLOBAL QUERY FILTERS (TỰ ĐỘNG LỌC BẢO MẬT)
            // =======================================================
            modelBuilder.Entity<Agent>().HasQueryFilter(a => PermittedHospitalIds == null || PermittedHospitalIds.Contains(a.HospitalId ?? 0));
            modelBuilder.Entity<InfrastructureAsset>().HasQueryFilter(ia => PermittedHospitalIds == null || PermittedHospitalIds.Contains(ia.HospitalId ?? 0));
            modelBuilder.Entity<MetricRecord>().HasQueryFilter(m => PermittedHospitalIds == null || PermittedHospitalIds.Contains(m.Agent!.HospitalId ?? 0));
            modelBuilder.Entity<SecurityEvent>().HasQueryFilter(e => PermittedHospitalIds == null || PermittedHospitalIds.Contains(e.Agent!.HospitalId ?? 0));
            modelBuilder.Entity<Alert>().HasQueryFilter(al => PermittedHospitalIds == null || PermittedHospitalIds.Contains(al.Agent!.HospitalId ?? 0));
            modelBuilder.Entity<ResponseAction>().HasQueryFilter(ra => PermittedHospitalIds == null || PermittedHospitalIds.Contains(ra.Agent!.HospitalId ?? 0));
            modelBuilder.Entity<Incident>().HasQueryFilter(i => 
                PermittedHospitalIds == null || 
                i.AssignedUserId == CurrentUserId || 
                i.Alerts.Any(al => PermittedHospitalIds.Contains(al.Agent!.HospitalId ?? 0)));

            // ==========================================
            // Seed Data cho các bảng
            // ==========================================
            
             modelBuilder.Entity<Hospital>().HasData(
                new Hospital { Id = 1, Name = "Hospital Tổng", Code = "ROOT", ParentId = null },
                new Hospital { Id = 2, Name = "Hospital A", Code = "HOSP_A", ParentId = 1 },
                new Hospital { Id = 3, Name = "Hospital A1", Code = "HOSP_A1", ParentId = 2 },
                new Hospital { Id = 4, Name = "Hospital A2", Code = "HOSP_A2", ParentId = 2 },
                new Hospital { Id = 5, Name = "Hospital B", Code = "HOSP_B", ParentId = 1 },
                new Hospital { Id = 6, Name = "Hospital B1", Code = "HOSP_B1", ParentId = 5 },
                new Hospital { Id = 7, Name = "Hospital B2", Code = "HOSP_B2", ParentId = 5 }
            );

            modelBuilder.Entity<AgentConfig>().HasData(
                new AgentConfig
                {
                    Id = 1,
                    Name = "Default Hospital Policy",
                    HeartbeatIntervalSeconds = 10,
                    MetricsIntervalSeconds = 30,
                    MonitoredLogsConfig = "[]",
                    Version = 1,
                    IsDefault = true,
                    CreatedAt = DateTime.Parse("2026-07-08T00:00:00Z").ToUniversalTime(),
                    UpdatedAt = DateTime.Parse("2026-07-08T00:00:00Z").ToUniversalTime()
                }
            );

            modelBuilder.Entity<AgentPolicy>().HasData(
                new AgentPolicy
                {
                    Id = 1,
                    Name = "Default Policy",
                    HeartbeatInterval = 10,
                    MetricsInterval = 10,
                    EnabledLogs = "ProcessMonitor,FileIntegrity,NetworkMonitor",
                    ResponseEnabled = true,
                    Description = "Standard monitor policy with responses enabled.",
                    Version = 1
                }
            );

            modelBuilder.Entity<CollectorNode>().HasData(
                new CollectorNode
                {
                    Id = 1,
                    Name = "Primary Collector",
                    IPAddress = "127.0.0.1",
                    CollectorKey = "primary_key",
                    SharedSecret = HashSecret("collector_secret_2026"),
                    Version = "1.2.0",
                    Status = "Online",
                    Location = "HQ Data Center",
                    Description = "Primary SOC Collector Node",
                    LastHeartbeat = DateTime.Parse("2026-07-08T00:00:00Z").ToUniversalTime(),
                    LastSync = DateTime.Parse("2026-07-08T00:00:00Z").ToUniversalTime(),
                    ConfigurationVersion = 1,
                    RulesVersion = 1
                }
            );

            modelBuilder.Entity<InfrastructureAsset>().HasData(
                new InfrastructureAsset
                {
                    Id = 1,
                    Hostname = "DESKTOP-FC9F6G9",
                    IPAddress = "192.168.1.35",
                    OperatingSystem = "Windows",
                    OperatingSystemVersion = "Server 2022",
                    Domain = "WORKGROUP",
                    Department = "IT",
                    Building = "Main Building",
                    Owner = "sysadmin",
                    Description = "HIS Web Server",
                    Criticality = "High",
                    AssetType = "Server",
                    Status = "Managed",
                    CollectorId = 1,
                    PolicyId = 1,
                    HospitalId = 1,
                    CreatedAt = DateTime.Parse("2026-07-08T00:00:00Z").ToUniversalTime(),
                    UpdatedAt = DateTime.Parse("2026-07-08T00:00:00Z").ToUniversalTime()
                },
                new InfrastructureAsset
                {
                    Id = 2,
                    Hostname = "ubuntu-hosp-a-db",
                    IPAddress = "192.168.1.40",
                    OperatingSystem = "Linux",
                    OperatingSystemVersion = "Ubuntu 22.04 LTS",
                    Domain = "hospa.local",
                    Department = "Security",
                    Building = "Server Room A",
                    Owner = "dbadmin",
                    Description = "Main Database Server",
                    Criticality = "Critical",
                    AssetType = "Server",
                    Status = "Managed",
                    CollectorId = 1,
                    PolicyId = 1,
                    HospitalId = 2,
                    CreatedAt = DateTime.Parse("2026-07-08T00:00:00Z").ToUniversalTime(),
                    UpdatedAt = DateTime.Parse("2026-07-08T00:00:00Z").ToUniversalTime()
                },
                new InfrastructureAsset
                {
                    Id = 3,
                    Hostname = "win-hosp-a-app",
                    IPAddress = "192.168.1.41",
                    OperatingSystem = "Windows",
                    OperatingSystemVersion = "Server 2019",
                    Domain = "hospa.local",
                    Department = "IT",
                    Building = "Server Room A",
                    Owner = "sysadmin",
                    Description = "Application Gateway",
                    Criticality = "High",
                    AssetType = "Server",
                    Status = "Managed",
                    CollectorId = 1,
                    PolicyId = 1,
                    HospitalId = 2,
                    CreatedAt = DateTime.Parse("2026-07-08T00:00:00Z").ToUniversalTime(),
                    UpdatedAt = DateTime.Parse("2026-07-08T00:00:00Z").ToUniversalTime()
                },
                new InfrastructureAsset
                {
                    Id = 4,
                    Hostname = "win-hosp-b-his",
                    IPAddress = "192.168.1.50",
                    OperatingSystem = "Windows",
                    OperatingSystemVersion = "Server 2022",
                    Domain = "hospb.local",
                    Department = "Medical",
                    Building = "Clinic B",
                    Owner = "clinicadmin",
                    Description = "Clinic Portal Host",
                    Criticality = "High",
                    AssetType = "Server",
                    Status = "Managed",
                    CollectorId = 1,
                    PolicyId = 1,
                    HospitalId = 5,
                    CreatedAt = DateTime.Parse("2026-07-08T00:00:00Z").ToUniversalTime(),
                    UpdatedAt = DateTime.Parse("2026-07-08T00:00:00Z").ToUniversalTime()
                },
                new InfrastructureAsset
                {
                    Id = 5,
                    Hostname = "ubuntu-hosp-b-web",
                    IPAddress = "192.168.1.51",
                    OperatingSystem = "Linux",
                    OperatingSystemVersion = "Ubuntu 20.04 LTS",
                    Domain = "hospb.local",
                    Department = "Public Relations",
                    Building = "Clinic B",
                    Owner = "webadmin",
                    Description = "Public Website Host",
                    Criticality = "Medium",
                    AssetType = "Server",
                    Status = "Managed",
                    CollectorId = 1,
                    PolicyId = 1,
                    HospitalId = 5,     
                    CreatedAt = DateTime.Parse("2026-07-08T00:00:00Z").ToUniversalTime(),
                    UpdatedAt = DateTime.Parse("2026-07-08T00:00:00Z").ToUniversalTime()
                }
            );

            modelBuilder.Entity<Agent>().HasData(
                new Agent
                {
                    Id = "agent-win-his-01",
                    Hostname = "DESKTOP-FC9F6G9",
                    IpAddress = "192.168.1.35",
                    OsInfo = "Windows Server 2022",
                    Status = "online",
                    ConfigId = 1,
                    AssetId = 1,
                    HospitalId = 1,
                    RegisteredAt = DateTime.Parse("2026-07-08T00:00:00Z").ToUniversalTime(),
                    LastSeenAt = DateTime.Parse("2026-07-08T12:00:00Z").ToUniversalTime()
                },
                new Agent
                {
                    Id = "agent-lnx-db-01",
                    Hostname = "ubuntu-hosp-a-db",
                    IpAddress = "192.168.1.40",
                    OsInfo = "Ubuntu 22.04 LTS",
                    Status = "online",
                    ConfigId = 1,
                    AssetId = 2,
                    HospitalId = 2,
                    RegisteredAt = DateTime.Parse("2026-07-08T00:00:00Z").ToUniversalTime(),
                    LastSeenAt = DateTime.Parse("2026-07-08T12:00:00Z").ToUniversalTime()
                },
                new Agent
                {
                    Id = "agent-win-app-01",
                    Hostname = "win-hosp-a-app",
                    IpAddress = "192.168.1.41",
                    OsInfo = "Windows Server 2019",
                    Status = "online",
                    ConfigId = 1,
                    AssetId = 3,
                    HospitalId = 2,
                    RegisteredAt = DateTime.Parse("2026-07-08T00:00:00Z").ToUniversalTime(),
                    LastSeenAt = DateTime.Parse("2026-07-08T12:00:00Z").ToUniversalTime()
                },
                new Agent
                {
                    Id = "agent-win-his-02",
                    Hostname = "win-hosp-b-his",
                    IpAddress = "192.168.1.50",
                    OsInfo = "Windows Server 2022",
                    Status = "online",
                    ConfigId = 1,
                    AssetId = 4,
                    HospitalId = 5,
                    RegisteredAt = DateTime.Parse("2026-07-08T00:00:00Z").ToUniversalTime(),
                    LastSeenAt = DateTime.Parse("2026-07-08T12:00:00Z").ToUniversalTime()
                },
                new Agent
                {
                    Id = "agent-lnx-web-02",
                    Hostname = "ubuntu-hosp-b-web",
                    IpAddress = "192.168.1.51",
                    OsInfo = "Ubuntu 20.04 LTS",
                    Status = "online",
                    ConfigId = 1,
                    AssetId = 5,
                    HospitalId = 5,
                    RegisteredAt = DateTime.Parse("2026-07-08T00:00:00Z").ToUniversalTime(),
                    LastSeenAt = DateTime.Parse("2026-07-08T12:00:00Z").ToUniversalTime()
                }
            );
        }

        private static string HashSecret(string secret)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(secret);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}