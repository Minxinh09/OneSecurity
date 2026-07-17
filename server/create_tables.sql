CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;

CREATE TABLE "agent_configs" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_agent_configs" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "HeartbeatIntervalSeconds" INTEGER NOT NULL,
    "MetricsIntervalSeconds" INTEGER NOT NULL,
    "MonitoredLogsConfig" TEXT NOT NULL,
    "Version" INTEGER NOT NULL,
    "IsDefault" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL
);

CREATE TABLE "alert_rules" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_alert_rules" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "EventType" TEXT NULL,
    "ConditionExpression" TEXT NOT NULL,
    "AlertSeverity" TEXT NOT NULL,
    "IsEnabled" INTEGER NOT NULL,
    "TelegramChatId" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL
);

CREATE TABLE "agents" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_agents" PRIMARY KEY,
    "Hostname" TEXT NOT NULL,
    "IpAddress" TEXT NOT NULL,
    "OsInfo" TEXT NOT NULL,
    "HardwareSpecs" TEXT NULL,
    "Status" TEXT NOT NULL,
    "ConfigId" INTEGER NOT NULL,
    "RegisteredAt" TEXT NOT NULL,
    "LastSeenAt" TEXT NOT NULL,
    CONSTRAINT "FK_agents_agent_configs_ConfigId" FOREIGN KEY ("ConfigId") REFERENCES "agent_configs" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "metric_records" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_metric_records" PRIMARY KEY AUTOINCREMENT,
    "AgentId" TEXT NOT NULL,
    "Timestamp" TEXT NOT NULL,
    "CpuUsagePercent" TEXT NOT NULL,
    "RamUsagePercent" TEXT NOT NULL,
    "DiskUsagePercent" TEXT NOT NULL,
    "NetworkInBytes" INTEGER NOT NULL,
    "NetworkOutBytes" INTEGER NOT NULL,
    CONSTRAINT "FK_metric_records_agents_AgentId" FOREIGN KEY ("AgentId") REFERENCES "agents" ("Id") ON DELETE CASCADE
);

CREATE TABLE "security_events" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_security_events" PRIMARY KEY AUTOINCREMENT,
    "EventId" TEXT NOT NULL,
    "AgentId" TEXT NOT NULL,
    "Timestamp" TEXT NOT NULL,
    "Category" TEXT NOT NULL,
    "Severity" TEXT NOT NULL,
    "Source" TEXT NOT NULL,
    "Title" TEXT NOT NULL,
    "Details" TEXT NOT NULL,
    "RawData" TEXT NOT NULL,
    "ReceivedAt" TEXT NOT NULL,
    CONSTRAINT "FK_security_events_agents_AgentId" FOREIGN KEY ("AgentId") REFERENCES "agents" ("Id") ON DELETE CASCADE
);

CREATE TABLE "alerts" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_alerts" PRIMARY KEY AUTOINCREMENT,
    "AgentId" TEXT NOT NULL,
    "RuleId" INTEGER NULL,
    "TriggerEventId" INTEGER NULL,
    "RuleName" TEXT NOT NULL,
    "Severity" TEXT NOT NULL,
    "Title" TEXT NOT NULL,
    "Message" TEXT NOT NULL,
    "Category" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "IsAcknowledged" INTEGER NOT NULL,
    "AcknowledgedAt" TEXT NULL,
    "AcknowledgedBy" TEXT NULL,
    "TelegramSent" INTEGER NOT NULL,
    CONSTRAINT "FK_alerts_agents_AgentId" FOREIGN KEY ("AgentId") REFERENCES "agents" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_alerts_alert_rules_RuleId" FOREIGN KEY ("RuleId") REFERENCES "alert_rules" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_alerts_security_events_TriggerEventId" FOREIGN KEY ("TriggerEventId") REFERENCES "security_events" ("Id") ON DELETE RESTRICT
);

INSERT INTO "agent_configs" ("Id", "CreatedAt", "HeartbeatIntervalSeconds", "IsDefault", "MetricsIntervalSeconds", "MonitoredLogsConfig", "Name", "UpdatedAt", "Version")
VALUES (1, '2026-07-08 00:00:00', 10, 1, 30, '[]', 'Default Hospital Policy', '2026-07-08 00:00:00', 1);
SELECT changes();


INSERT INTO "agents" ("Id", "ConfigId", "HardwareSpecs", "Hostname", "IpAddress", "LastSeenAt", "OsInfo", "RegisteredAt", "Status")
VALUES ('agent-lnx-db-01', 1, NULL, 'ubuntu-hosp-a-db', '192.168.1.40', '2026-07-08 12:00:00', 'Ubuntu 22.04 LTS', '2026-07-08 00:00:00', 'online');
SELECT changes();

INSERT INTO "agents" ("Id", "ConfigId", "HardwareSpecs", "Hostname", "IpAddress", "LastSeenAt", "OsInfo", "RegisteredAt", "Status")
VALUES ('agent-lnx-web-02', 1, NULL, 'ubuntu-hosp-b-web', '192.168.1.51', '2026-07-08 12:00:00', 'Ubuntu 20.04 LTS', '2026-07-08 00:00:00', 'online');
SELECT changes();

INSERT INTO "agents" ("Id", "ConfigId", "HardwareSpecs", "Hostname", "IpAddress", "LastSeenAt", "OsInfo", "RegisteredAt", "Status")
VALUES ('agent-win-app-01', 1, NULL, 'win-hosp-a-app', '192.168.1.41', '2026-07-08 12:00:00', 'Windows Server 2019', '2026-07-08 00:00:00', 'online');
SELECT changes();

INSERT INTO "agents" ("Id", "ConfigId", "HardwareSpecs", "Hostname", "IpAddress", "LastSeenAt", "OsInfo", "RegisteredAt", "Status")
VALUES ('agent-win-his-01', 1, NULL, 'DESKTOP-FC9F6G9', '192.168.1.35', '2026-07-08 12:00:00', 'Windows Server 2022', '2026-07-08 00:00:00', 'online');
SELECT changes();

INSERT INTO "agents" ("Id", "ConfigId", "HardwareSpecs", "Hostname", "IpAddress", "LastSeenAt", "OsInfo", "RegisteredAt", "Status")
VALUES ('agent-win-his-02', 1, NULL, 'win-hosp-b-his', '192.168.1.50', '2026-07-08 12:00:00', 'Windows Server 2022', '2026-07-08 00:00:00', 'online');
SELECT changes();


CREATE UNIQUE INDEX "IX_agent_configs_Name" ON "agent_configs" ("Name");

CREATE INDEX "IX_agents_ConfigId" ON "agents" ("ConfigId");

CREATE INDEX "IX_agents_LastSeenAt" ON "agents" ("LastSeenAt");

CREATE INDEX "IX_agents_Status" ON "agents" ("Status");

CREATE INDEX "IX_alert_rules_IsEnabled_EventType" ON "alert_rules" ("IsEnabled", "EventType");

CREATE UNIQUE INDEX "IX_alert_rules_Name" ON "alert_rules" ("Name");

CREATE INDEX "IX_alerts_AgentId" ON "alerts" ("AgentId");

CREATE INDEX "IX_alerts_CreatedAt" ON "alerts" ("CreatedAt");

CREATE INDEX "IX_alerts_RuleId" ON "alerts" ("RuleId");

CREATE INDEX "IX_alerts_TriggerEventId" ON "alerts" ("TriggerEventId");

CREATE INDEX "IX_metric_records_agent_timestamp_desc" ON "metric_records" ("AgentId", "Timestamp");

CREATE INDEX "IX_metric_records_Timestamp" ON "metric_records" ("Timestamp");

CREATE INDEX "IX_security_events_agent_timestamp_desc" ON "security_events" ("AgentId", "Timestamp");

CREATE UNIQUE INDEX "IX_security_events_EventId" ON "security_events" ("EventId");

CREATE INDEX "IX_security_events_Severity" ON "security_events" ("Severity");

CREATE INDEX "IX_security_events_Timestamp" ON "security_events" ("Timestamp");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260709072437_InitialCreate', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

CREATE TABLE "AspNetRoles" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_AspNetRoles" PRIMARY KEY,
    "Name" TEXT NULL,
    "NormalizedName" TEXT NULL,
    "ConcurrencyStamp" TEXT NULL
);

CREATE TABLE "AspNetUsers" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_AspNetUsers" PRIMARY KEY,
    "FullName" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NULL,
    "LastLoginAt" TEXT NULL,
    "UserName" TEXT NULL,
    "NormalizedUserName" TEXT NULL,
    "Email" TEXT NULL,
    "NormalizedEmail" TEXT NULL,
    "EmailConfirmed" INTEGER NOT NULL,
    "PasswordHash" TEXT NULL,
    "SecurityStamp" TEXT NULL,
    "ConcurrencyStamp" TEXT NULL,
    "PhoneNumber" TEXT NULL,
    "PhoneNumberConfirmed" INTEGER NOT NULL,
    "TwoFactorEnabled" INTEGER NOT NULL,
    "LockoutEnd" TEXT NULL,
    "LockoutEnabled" INTEGER NOT NULL,
    "AccessFailedCount" INTEGER NOT NULL
);

CREATE TABLE "AspNetRoleClaims" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AspNetRoleClaims" PRIMARY KEY AUTOINCREMENT,
    "RoleId" TEXT NOT NULL,
    "ClaimType" TEXT NULL,
    "ClaimValue" TEXT NULL,
    CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserClaims" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AspNetUserClaims" PRIMARY KEY AUTOINCREMENT,
    "UserId" TEXT NOT NULL,
    "ClaimType" TEXT NULL,
    "ClaimValue" TEXT NULL,
    CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserLogins" (
    "LoginProvider" TEXT NOT NULL,
    "ProviderKey" TEXT NOT NULL,
    "ProviderDisplayName" TEXT NULL,
    "UserId" TEXT NOT NULL,
    CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
    CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserRoles" (
    "UserId" TEXT NOT NULL,
    "RoleId" TEXT NOT NULL,
    CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
    CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserTokens" (
    "UserId" TEXT NOT NULL,
    "LoginProvider" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Value" TEXT NULL,
    CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
    CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AspNetRoleClaims_RoleId" ON "AspNetRoleClaims" ("RoleId");

CREATE UNIQUE INDEX "RoleNameIndex" ON "AspNetRoles" ("NormalizedName");

CREATE INDEX "IX_AspNetUserClaims_UserId" ON "AspNetUserClaims" ("UserId");

CREATE INDEX "IX_AspNetUserLogins_UserId" ON "AspNetUserLogins" ("UserId");

CREATE INDEX "IX_AspNetUserRoles_RoleId" ON "AspNetUserRoles" ("RoleId");

CREATE INDEX "EmailIndex" ON "AspNetUsers" ("NormalizedEmail");

CREATE UNIQUE INDEX "UserNameIndex" ON "AspNetUsers" ("NormalizedUserName");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260710034810_AddIdentityAndAuth', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

CREATE TABLE "audit_logs" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_audit_logs" PRIMARY KEY AUTOINCREMENT,
    "TimestampUtc" TEXT NOT NULL,
    "UserId" TEXT NULL,
    "UserName" TEXT NULL,
    "Role" TEXT NULL,
    "Action" TEXT NOT NULL,
    "ResourceType" TEXT NOT NULL,
    "EntityId" TEXT NULL,
    "Description" TEXT NULL,
    "IpAddress" TEXT NULL,
    "UserAgent" TEXT NULL,
    "Success" INTEGER NOT NULL,
    "StatusCode" INTEGER NOT NULL,
    "Severity" TEXT NOT NULL,
    "CorrelationId" TEXT NULL
);

CREATE INDEX "IX_audit_logs_Action" ON "audit_logs" ("Action");

CREATE INDEX "IX_audit_logs_CorrelationId" ON "audit_logs" ("CorrelationId");

CREATE INDEX "IX_audit_logs_ResourceType" ON "audit_logs" ("ResourceType");

CREATE INDEX "IX_audit_logs_TimestampUtc" ON "audit_logs" ("TimestampUtc");

CREATE INDEX "IX_audit_logs_UserId" ON "audit_logs" ("UserId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260710065002_AddAuditLogs', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

DROP INDEX "IX_audit_logs_CorrelationId";

ALTER TABLE "audit_logs" RENAME COLUMN "Role" TO "Roles";

CREATE INDEX "IX_audit_logs_Severity" ON "audit_logs" ("Severity");

CREATE INDEX "IX_audit_logs_Success" ON "audit_logs" ("Success");

CREATE INDEX "IX_audit_logs_TimestampUtc_Severity" ON "audit_logs" ("TimestampUtc", "Severity");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260710072333_RefineAuditLogsSchema', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

ALTER TABLE "alerts" ADD "IncidentId" INTEGER NULL;

CREATE TABLE "incidents" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_incidents" PRIMARY KEY AUTOINCREMENT,
    "Title" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "Severity" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "AssignedUserId" TEXT NULL,
    "AssignedAt" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    "ResolvedAt" TEXT NULL,
    "ResolvedByUserId" TEXT NULL,
    "ClosedAt" TEXT NULL,
    "ClosedByUserId" TEXT NULL,
    "CreatedByUserId" TEXT NULL,
    CONSTRAINT "FK_incidents_AspNetUsers_AssignedUserId" FOREIGN KEY ("AssignedUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_incidents_AspNetUsers_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_alerts_IncidentId" ON "alerts" ("IncidentId");

CREATE INDEX "IX_incidents_AssignedUserId" ON "incidents" ("AssignedUserId");

CREATE INDEX "IX_incidents_CreatedAt" ON "incidents" ("CreatedAt");

CREATE INDEX "IX_incidents_CreatedByUserId" ON "incidents" ("CreatedByUserId");

CREATE INDEX "IX_incidents_Severity" ON "incidents" ("Severity");

CREATE INDEX "IX_incidents_Status" ON "incidents" ("Status");

CREATE TABLE "ef_temp_alerts" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_alerts" PRIMARY KEY AUTOINCREMENT,
    "AcknowledgedAt" TEXT NULL,
    "AcknowledgedBy" TEXT NULL,
    "AgentId" TEXT NOT NULL,
    "Category" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "IncidentId" INTEGER NULL,
    "IsAcknowledged" INTEGER NOT NULL,
    "Message" TEXT NOT NULL,
    "RuleId" INTEGER NULL,
    "RuleName" TEXT NOT NULL,
    "Severity" TEXT NOT NULL,
    "TelegramSent" INTEGER NOT NULL,
    "Title" TEXT NOT NULL,
    "TriggerEventId" INTEGER NULL,
    CONSTRAINT "FK_alerts_agents_AgentId" FOREIGN KEY ("AgentId") REFERENCES "agents" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_alerts_alert_rules_RuleId" FOREIGN KEY ("RuleId") REFERENCES "alert_rules" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_alerts_incidents_IncidentId" FOREIGN KEY ("IncidentId") REFERENCES "incidents" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_alerts_security_events_TriggerEventId" FOREIGN KEY ("TriggerEventId") REFERENCES "security_events" ("Id") ON DELETE RESTRICT
);

INSERT INTO "ef_temp_alerts" ("Id", "AcknowledgedAt", "AcknowledgedBy", "AgentId", "Category", "CreatedAt", "IncidentId", "IsAcknowledged", "Message", "RuleId", "RuleName", "Severity", "TelegramSent", "Title", "TriggerEventId")
SELECT "Id", "AcknowledgedAt", "AcknowledgedBy", "AgentId", "Category", "CreatedAt", "IncidentId", "IsAcknowledged", "Message", "RuleId", "RuleName", "Severity", "TelegramSent", "Title", "TriggerEventId"
FROM "alerts";

COMMIT;

PRAGMA foreign_keys = 0;

BEGIN TRANSACTION;

DROP TABLE "alerts";

ALTER TABLE "ef_temp_alerts" RENAME TO "alerts";

COMMIT;

PRAGMA foreign_keys = 1;

BEGIN TRANSACTION;

CREATE INDEX "IX_alerts_AgentId" ON "alerts" ("AgentId");

CREATE INDEX "IX_alerts_CreatedAt" ON "alerts" ("CreatedAt");

CREATE INDEX "IX_alerts_IncidentId" ON "alerts" ("IncidentId");

CREATE INDEX "IX_alerts_RuleId" ON "alerts" ("RuleId");

CREATE INDEX "IX_alerts_TriggerEventId" ON "alerts" ("TriggerEventId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260710075228_AddIncidentManagement', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

ALTER TABLE "alert_rules" ADD "Category" TEXT NOT NULL DEFAULT '';

ALTER TABLE "alert_rules" ADD "Priority" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "alert_rules" ADD "Version" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "agents" ADD "AgentVersion" TEXT NULL;

ALTER TABLE "agents" ADD "Capabilities" TEXT NULL;

ALTER TABLE "agents" ADD "CollectorVersion" TEXT NULL;

ALTER TABLE "agents" ADD "SupportedActions" TEXT NULL;

CREATE TABLE "response_actions" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_response_actions" PRIMARY KEY AUTOINCREMENT,
    "IncidentId" INTEGER NOT NULL,
    "AgentId" TEXT NOT NULL,
    "ActionType" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "RequestedByUserId" TEXT NOT NULL,
    "ApprovedByUserId" TEXT NULL,
    "RequestedAt" TEXT NOT NULL,
    "StartedAt" TEXT NULL,
    "CompletedAt" TEXT NULL,
    "ResultMessage" TEXT NULL,
    "CorrelationId" TEXT NOT NULL,
    "Metadata" TEXT NULL,
    CONSTRAINT "FK_response_actions_AspNetUsers_ApprovedByUserId" FOREIGN KEY ("ApprovedByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_response_actions_AspNetUsers_RequestedByUserId" FOREIGN KEY ("RequestedByUserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_response_actions_agents_AgentId" FOREIGN KEY ("AgentId") REFERENCES "agents" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_response_actions_incidents_IncidentId" FOREIGN KEY ("IncidentId") REFERENCES "incidents" ("Id") ON DELETE CASCADE
);

UPDATE "agents" SET "AgentVersion" = NULL, "Capabilities" = NULL, "CollectorVersion" = NULL, "SupportedActions" = NULL
WHERE "Id" = 'agent-lnx-db-01';
SELECT changes();


UPDATE "agents" SET "AgentVersion" = NULL, "Capabilities" = NULL, "CollectorVersion" = NULL, "SupportedActions" = NULL
WHERE "Id" = 'agent-lnx-web-02';
SELECT changes();


UPDATE "agents" SET "AgentVersion" = NULL, "Capabilities" = NULL, "CollectorVersion" = NULL, "SupportedActions" = NULL
WHERE "Id" = 'agent-win-app-01';
SELECT changes();


UPDATE "agents" SET "AgentVersion" = NULL, "Capabilities" = NULL, "CollectorVersion" = NULL, "SupportedActions" = NULL
WHERE "Id" = 'agent-win-his-01';
SELECT changes();


UPDATE "agents" SET "AgentVersion" = NULL, "Capabilities" = NULL, "CollectorVersion" = NULL, "SupportedActions" = NULL
WHERE "Id" = 'agent-win-his-02';
SELECT changes();


CREATE INDEX "IX_response_actions_AgentId" ON "response_actions" ("AgentId");

CREATE INDEX "IX_response_actions_ApprovedByUserId" ON "response_actions" ("ApprovedByUserId");

CREATE INDEX "IX_response_actions_IncidentId" ON "response_actions" ("IncidentId");

CREATE INDEX "IX_response_actions_RequestedByUserId" ON "response_actions" ("RequestedByUserId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260713073322_AddResponseActions', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

ALTER TABLE "agents" ADD "AssetId" INTEGER NULL;

CREATE TABLE "agent_policies" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_agent_policies" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "HeartbeatInterval" INTEGER NOT NULL,
    "MetricsInterval" INTEGER NOT NULL,
    "EnabledLogs" TEXT NOT NULL,
    "ResponseEnabled" INTEGER NOT NULL,
    "Description" TEXT NULL,
    "Version" INTEGER NOT NULL
);

CREATE TABLE "collector_nodes" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_collector_nodes" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "IPAddress" TEXT NOT NULL,
    "CollectorKey" TEXT NOT NULL,
    "SharedSecret" TEXT NOT NULL,
    "Version" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "Location" TEXT NULL,
    "LastHeartbeat" TEXT NOT NULL,
    "LastSync" TEXT NOT NULL,
    "ConfigurationVersion" INTEGER NOT NULL,
    "RulesVersion" INTEGER NOT NULL,
    "Description" TEXT NULL
);

CREATE TABLE "infrastructure_assets" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_infrastructure_assets" PRIMARY KEY AUTOINCREMENT,
    "Hostname" TEXT NOT NULL,
    "IPAddress" TEXT NOT NULL,
    "OperatingSystem" TEXT NOT NULL,
    "OperatingSystemVersion" TEXT NOT NULL,
    "Domain" TEXT NULL,
    "Department" TEXT NULL,
    "Building" TEXT NULL,
    "Owner" TEXT NULL,
    "Description" TEXT NULL,
    "Criticality" TEXT NOT NULL,
    "AssetType" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "CollectorId" INTEGER NOT NULL,
    "PolicyId" INTEGER NOT NULL,
    "LastSeen" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_infrastructure_assets_agent_policies_PolicyId" FOREIGN KEY ("PolicyId") REFERENCES "agent_policies" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_infrastructure_assets_collector_nodes_CollectorId" FOREIGN KEY ("CollectorId") REFERENCES "collector_nodes" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "enrollment_tokens" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_enrollment_tokens" PRIMARY KEY AUTOINCREMENT,
    "Token" TEXT NOT NULL,
    "AssetId" INTEGER NOT NULL,
    "PolicyId" INTEGER NOT NULL,
    "CollectorId" INTEGER NOT NULL,
    "ExpireAt" TEXT NOT NULL,
    "Used" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "Reason" TEXT NULL,
    "CreatedBy" TEXT NULL,
    "UsedAt" TEXT NULL,
    "MaxUses" INTEGER NOT NULL,
    "UsedCount" INTEGER NOT NULL,
    CONSTRAINT "FK_enrollment_tokens_agent_policies_PolicyId" FOREIGN KEY ("PolicyId") REFERENCES "agent_policies" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_enrollment_tokens_collector_nodes_CollectorId" FOREIGN KEY ("CollectorId") REFERENCES "collector_nodes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_enrollment_tokens_infrastructure_assets_AssetId" FOREIGN KEY ("AssetId") REFERENCES "infrastructure_assets" ("Id") ON DELETE CASCADE
);

INSERT INTO "agent_policies" ("Id", "Description", "EnabledLogs", "HeartbeatInterval", "MetricsInterval", "Name", "ResponseEnabled", "Version")
VALUES (1, 'Standard monitor policy with responses enabled.', 'ProcessMonitor,FileIntegrity,NetworkMonitor', 10, 10, 'Default Policy', 1, 1);
SELECT changes();


UPDATE "agents" SET "AssetId" = 2
WHERE "Id" = 'agent-lnx-db-01';
SELECT changes();


UPDATE "agents" SET "AssetId" = 5
WHERE "Id" = 'agent-lnx-web-02';
SELECT changes();


UPDATE "agents" SET "AssetId" = 3
WHERE "Id" = 'agent-win-app-01';
SELECT changes();


UPDATE "agents" SET "AssetId" = 1
WHERE "Id" = 'agent-win-his-01';
SELECT changes();


UPDATE "agents" SET "AssetId" = 4
WHERE "Id" = 'agent-win-his-02';
SELECT changes();


INSERT INTO "collector_nodes" ("Id", "CollectorKey", "ConfigurationVersion", "Description", "IPAddress", "LastHeartbeat", "LastSync", "Location", "Name", "RulesVersion", "SharedSecret", "Status", "Version")
VALUES (1, 'primary_key', 1, 'Primary SOC Collector Node', '127.0.0.1', '2026-07-08 00:00:00', '2026-07-08 00:00:00', 'HQ Data Center', 'Primary Collector', 1, '32ac234d7a942bdbe88387389da17e11b2cbb577ee2d8c17d5841705a8629724', 'Online', '1.2.0');
SELECT changes();


INSERT INTO "infrastructure_assets" ("Id", "AssetType", "Building", "CollectorId", "CreatedAt", "Criticality", "Department", "Description", "Domain", "Hostname", "IPAddress", "LastSeen", "OperatingSystem", "OperatingSystemVersion", "Owner", "PolicyId", "Status", "UpdatedAt")
VALUES (1, 'Server', 'Main Building', 1, '2026-07-08 00:00:00', 'High', 'IT', 'HIS Web Server', 'WORKGROUP', 'DESKTOP-FC9F6G9', '192.168.1.35', NULL, 'Windows', 'Server 2022', 'sysadmin', 1, 'Managed', '2026-07-08 00:00:00');
SELECT changes();

INSERT INTO "infrastructure_assets" ("Id", "AssetType", "Building", "CollectorId", "CreatedAt", "Criticality", "Department", "Description", "Domain", "Hostname", "IPAddress", "LastSeen", "OperatingSystem", "OperatingSystemVersion", "Owner", "PolicyId", "Status", "UpdatedAt")
VALUES (2, 'Server', 'Server Room A', 1, '2026-07-08 00:00:00', 'Critical', 'Security', 'Main Database Server', 'hospa.local', 'ubuntu-hosp-a-db', '192.168.1.40', NULL, 'Linux', 'Ubuntu 22.04 LTS', 'dbadmin', 1, 'Managed', '2026-07-08 00:00:00');
SELECT changes();

INSERT INTO "infrastructure_assets" ("Id", "AssetType", "Building", "CollectorId", "CreatedAt", "Criticality", "Department", "Description", "Domain", "Hostname", "IPAddress", "LastSeen", "OperatingSystem", "OperatingSystemVersion", "Owner", "PolicyId", "Status", "UpdatedAt")
VALUES (3, 'Server', 'Server Room A', 1, '2026-07-08 00:00:00', 'High', 'IT', 'Application Gateway', 'hospa.local', 'win-hosp-a-app', '192.168.1.41', NULL, 'Windows', 'Server 2019', 'sysadmin', 1, 'Managed', '2026-07-08 00:00:00');
SELECT changes();

INSERT INTO "infrastructure_assets" ("Id", "AssetType", "Building", "CollectorId", "CreatedAt", "Criticality", "Department", "Description", "Domain", "Hostname", "IPAddress", "LastSeen", "OperatingSystem", "OperatingSystemVersion", "Owner", "PolicyId", "Status", "UpdatedAt")
VALUES (4, 'Server', 'Clinic B', 1, '2026-07-08 00:00:00', 'High', 'Medical', 'Clinic Portal Host', 'hospb.local', 'win-hosp-b-his', '192.168.1.50', NULL, 'Windows', 'Server 2022', 'clinicadmin', 1, 'Managed', '2026-07-08 00:00:00');
SELECT changes();

INSERT INTO "infrastructure_assets" ("Id", "AssetType", "Building", "CollectorId", "CreatedAt", "Criticality", "Department", "Description", "Domain", "Hostname", "IPAddress", "LastSeen", "OperatingSystem", "OperatingSystemVersion", "Owner", "PolicyId", "Status", "UpdatedAt")
VALUES (5, 'Server', 'Clinic B', 1, '2026-07-08 00:00:00', 'Medium', 'Public Relations', 'Public Website Host', 'hospb.local', 'ubuntu-hosp-b-web', '192.168.1.51', NULL, 'Linux', 'Ubuntu 20.04 LTS', 'webadmin', 1, 'Managed', '2026-07-08 00:00:00');
SELECT changes();


CREATE INDEX "IX_agents_AssetId" ON "agents" ("AssetId");

CREATE UNIQUE INDEX "IX_collector_nodes_CollectorKey" ON "collector_nodes" ("CollectorKey");

CREATE INDEX "IX_enrollment_tokens_AssetId" ON "enrollment_tokens" ("AssetId");

CREATE INDEX "IX_enrollment_tokens_CollectorId" ON "enrollment_tokens" ("CollectorId");

CREATE INDEX "IX_enrollment_tokens_PolicyId" ON "enrollment_tokens" ("PolicyId");

CREATE UNIQUE INDEX "IX_enrollment_tokens_Token" ON "enrollment_tokens" ("Token");

CREATE INDEX "IX_infrastructure_assets_CollectorId" ON "infrastructure_assets" ("CollectorId");

CREATE UNIQUE INDEX "IX_infrastructure_assets_Hostname" ON "infrastructure_assets" ("Hostname");

CREATE INDEX "IX_infrastructure_assets_PolicyId" ON "infrastructure_assets" ("PolicyId");

CREATE TABLE "ef_temp_agents" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_agents" PRIMARY KEY,
    "AgentVersion" TEXT NULL,
    "AssetId" INTEGER NULL,
    "Capabilities" TEXT NULL,
    "CollectorVersion" TEXT NULL,
    "ConfigId" INTEGER NOT NULL,
    "HardwareSpecs" TEXT NULL,
    "Hostname" TEXT NOT NULL,
    "IpAddress" TEXT NOT NULL,
    "LastSeenAt" TEXT NOT NULL,
    "OsInfo" TEXT NOT NULL,
    "RegisteredAt" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "SupportedActions" TEXT NULL,
    CONSTRAINT "FK_agents_agent_configs_ConfigId" FOREIGN KEY ("ConfigId") REFERENCES "agent_configs" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_agents_infrastructure_assets_AssetId" FOREIGN KEY ("AssetId") REFERENCES "infrastructure_assets" ("Id") ON DELETE SET NULL
);

INSERT INTO "ef_temp_agents" ("Id", "AgentVersion", "AssetId", "Capabilities", "CollectorVersion", "ConfigId", "HardwareSpecs", "Hostname", "IpAddress", "LastSeenAt", "OsInfo", "RegisteredAt", "Status", "SupportedActions")
SELECT "Id", "AgentVersion", "AssetId", "Capabilities", "CollectorVersion", "ConfigId", "HardwareSpecs", "Hostname", "IpAddress", "LastSeenAt", "OsInfo", "RegisteredAt", "Status", "SupportedActions"
FROM "agents";

COMMIT;

PRAGMA foreign_keys = 0;

BEGIN TRANSACTION;

DROP TABLE "agents";

ALTER TABLE "ef_temp_agents" RENAME TO "agents";

COMMIT;

PRAGMA foreign_keys = 1;

BEGIN TRANSACTION;

CREATE INDEX "IX_agents_AssetId" ON "agents" ("AssetId");

CREATE INDEX "IX_agents_ConfigId" ON "agents" ("ConfigId");

CREATE INDEX "IX_agents_LastSeenAt" ON "agents" ("LastSeenAt");

CREATE INDEX "IX_agents_Status" ON "agents" ("Status");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260714042800_AddInfrastructureManagement', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

ALTER TABLE "infrastructure_assets" ADD "HospitalId" INTEGER NULL;

ALTER TABLE "AspNetUsers" ADD "HospitalId" INTEGER NULL;

ALTER TABLE "agents" ADD "HospitalId" INTEGER NULL;

CREATE TABLE "hospitals" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_hospitals" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "Code" TEXT NOT NULL,
    "ParentId" INTEGER NULL,
    CONSTRAINT "FK_hospitals_hospitals_ParentId" FOREIGN KEY ("ParentId") REFERENCES "hospitals" ("Id") ON DELETE RESTRICT
);

UPDATE "agents" SET "HospitalId" = 2
WHERE "Id" = 'agent-lnx-db-01';
SELECT changes();


UPDATE "agents" SET "HospitalId" = 5
WHERE "Id" = 'agent-lnx-web-02';
SELECT changes();


UPDATE "agents" SET "HospitalId" = 2
WHERE "Id" = 'agent-win-app-01';
SELECT changes();


UPDATE "agents" SET "HospitalId" = 1
WHERE "Id" = 'agent-win-his-01';
SELECT changes();


UPDATE "agents" SET "HospitalId" = 5
WHERE "Id" = 'agent-win-his-02';
SELECT changes();


INSERT INTO "hospitals" ("Id", "Code", "Name", "ParentId")
VALUES (1, 'ROOT', 'Hospital Tổng', NULL);
SELECT changes();


UPDATE "infrastructure_assets" SET "HospitalId" = 1
WHERE "Id" = 1;
SELECT changes();


UPDATE "infrastructure_assets" SET "HospitalId" = 2
WHERE "Id" = 2;
SELECT changes();


UPDATE "infrastructure_assets" SET "HospitalId" = 2
WHERE "Id" = 3;
SELECT changes();


UPDATE "infrastructure_assets" SET "HospitalId" = 5
WHERE "Id" = 4;
SELECT changes();


UPDATE "infrastructure_assets" SET "HospitalId" = 5
WHERE "Id" = 5;
SELECT changes();


INSERT INTO "hospitals" ("Id", "Code", "Name", "ParentId")
VALUES (2, 'HOSP_A', 'Hospital A', 1);
SELECT changes();

INSERT INTO "hospitals" ("Id", "Code", "Name", "ParentId")
VALUES (5, 'HOSP_B', 'Hospital B', 1);
SELECT changes();

INSERT INTO "hospitals" ("Id", "Code", "Name", "ParentId")
VALUES (3, 'HOSP_A1', 'Hospital A1', 2);
SELECT changes();

INSERT INTO "hospitals" ("Id", "Code", "Name", "ParentId")
VALUES (4, 'HOSP_A2', 'Hospital A2', 2);
SELECT changes();

INSERT INTO "hospitals" ("Id", "Code", "Name", "ParentId")
VALUES (6, 'HOSP_B1', 'Hospital B1', 5);
SELECT changes();

INSERT INTO "hospitals" ("Id", "Code", "Name", "ParentId")
VALUES (7, 'HOSP_B2', 'Hospital B2', 5);
SELECT changes();


CREATE INDEX "IX_infrastructure_assets_HospitalId" ON "infrastructure_assets" ("HospitalId");

CREATE INDEX "IX_AspNetUsers_HospitalId" ON "AspNetUsers" ("HospitalId");

CREATE INDEX "IX_agents_HospitalId" ON "agents" ("HospitalId");

CREATE INDEX "IX_hospitals_ParentId" ON "hospitals" ("ParentId");

CREATE TABLE "ef_temp_agents" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_agents" PRIMARY KEY,
    "AgentVersion" TEXT NULL,
    "AssetId" INTEGER NULL,
    "Capabilities" TEXT NULL,
    "CollectorVersion" TEXT NULL,
    "ConfigId" INTEGER NOT NULL,
    "HardwareSpecs" TEXT NULL,
    "HospitalId" INTEGER NULL,
    "Hostname" TEXT NOT NULL,
    "IpAddress" TEXT NOT NULL,
    "LastSeenAt" TEXT NOT NULL,
    "OsInfo" TEXT NOT NULL,
    "RegisteredAt" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "SupportedActions" TEXT NULL,
    CONSTRAINT "FK_agents_agent_configs_ConfigId" FOREIGN KEY ("ConfigId") REFERENCES "agent_configs" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_agents_hospitals_HospitalId" FOREIGN KEY ("HospitalId") REFERENCES "hospitals" ("Id"),
    CONSTRAINT "FK_agents_infrastructure_assets_AssetId" FOREIGN KEY ("AssetId") REFERENCES "infrastructure_assets" ("Id") ON DELETE SET NULL
);

INSERT INTO "ef_temp_agents" ("Id", "AgentVersion", "AssetId", "Capabilities", "CollectorVersion", "ConfigId", "HardwareSpecs", "HospitalId", "Hostname", "IpAddress", "LastSeenAt", "OsInfo", "RegisteredAt", "Status", "SupportedActions")
SELECT "Id", "AgentVersion", "AssetId", "Capabilities", "CollectorVersion", "ConfigId", "HardwareSpecs", "HospitalId", "Hostname", "IpAddress", "LastSeenAt", "OsInfo", "RegisteredAt", "Status", "SupportedActions"
FROM "agents";

CREATE TABLE "ef_temp_AspNetUsers" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_AspNetUsers" PRIMARY KEY,
    "AccessFailedCount" INTEGER NOT NULL,
    "ConcurrencyStamp" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    "Email" TEXT NULL,
    "EmailConfirmed" INTEGER NOT NULL,
    "FullName" TEXT NOT NULL,
    "HospitalId" INTEGER NULL,
    "IsActive" INTEGER NOT NULL,
    "LastLoginAt" TEXT NULL,
    "LockoutEnabled" INTEGER NOT NULL,
    "LockoutEnd" TEXT NULL,
    "NormalizedEmail" TEXT NULL,
    "NormalizedUserName" TEXT NULL,
    "PasswordHash" TEXT NULL,
    "PhoneNumber" TEXT NULL,
    "PhoneNumberConfirmed" INTEGER NOT NULL,
    "SecurityStamp" TEXT NULL,
    "TwoFactorEnabled" INTEGER NOT NULL,
    "UpdatedAt" TEXT NULL,
    "UserName" TEXT NULL,
    CONSTRAINT "FK_AspNetUsers_hospitals_HospitalId" FOREIGN KEY ("HospitalId") REFERENCES "hospitals" ("Id")
);

INSERT INTO "ef_temp_AspNetUsers" ("Id", "AccessFailedCount", "ConcurrencyStamp", "CreatedAt", "Email", "EmailConfirmed", "FullName", "HospitalId", "IsActive", "LastLoginAt", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UpdatedAt", "UserName")
SELECT "Id", "AccessFailedCount", "ConcurrencyStamp", "CreatedAt", "Email", "EmailConfirmed", "FullName", "HospitalId", "IsActive", "LastLoginAt", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UpdatedAt", "UserName"
FROM "AspNetUsers";

CREATE TABLE "ef_temp_infrastructure_assets" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_infrastructure_assets" PRIMARY KEY AUTOINCREMENT,
    "AssetType" TEXT NOT NULL,
    "Building" TEXT NULL,
    "CollectorId" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "Criticality" TEXT NOT NULL,
    "Department" TEXT NULL,
    "Description" TEXT NULL,
    "Domain" TEXT NULL,
    "HospitalId" INTEGER NULL,
    "Hostname" TEXT NOT NULL,
    "IPAddress" TEXT NOT NULL,
    "LastSeen" TEXT NULL,
    "OperatingSystem" TEXT NOT NULL,
    "OperatingSystemVersion" TEXT NOT NULL,
    "Owner" TEXT NULL,
    "PolicyId" INTEGER NOT NULL,
    "Status" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_infrastructure_assets_agent_policies_PolicyId" FOREIGN KEY ("PolicyId") REFERENCES "agent_policies" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_infrastructure_assets_collector_nodes_CollectorId" FOREIGN KEY ("CollectorId") REFERENCES "collector_nodes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_infrastructure_assets_hospitals_HospitalId" FOREIGN KEY ("HospitalId") REFERENCES "hospitals" ("Id")
);

INSERT INTO "ef_temp_infrastructure_assets" ("Id", "AssetType", "Building", "CollectorId", "CreatedAt", "Criticality", "Department", "Description", "Domain", "HospitalId", "Hostname", "IPAddress", "LastSeen", "OperatingSystem", "OperatingSystemVersion", "Owner", "PolicyId", "Status", "UpdatedAt")
SELECT "Id", "AssetType", "Building", "CollectorId", "CreatedAt", "Criticality", "Department", "Description", "Domain", "HospitalId", "Hostname", "IPAddress", "LastSeen", "OperatingSystem", "OperatingSystemVersion", "Owner", "PolicyId", "Status", "UpdatedAt"
FROM "infrastructure_assets";

COMMIT;

PRAGMA foreign_keys = 0;

BEGIN TRANSACTION;

DROP TABLE "agents";

ALTER TABLE "ef_temp_agents" RENAME TO "agents";

DROP TABLE "AspNetUsers";

ALTER TABLE "ef_temp_AspNetUsers" RENAME TO "AspNetUsers";

DROP TABLE "infrastructure_assets";

ALTER TABLE "ef_temp_infrastructure_assets" RENAME TO "infrastructure_assets";

COMMIT;

PRAGMA foreign_keys = 1;

BEGIN TRANSACTION;

CREATE INDEX "IX_agents_AssetId" ON "agents" ("AssetId");

CREATE INDEX "IX_agents_ConfigId" ON "agents" ("ConfigId");

CREATE INDEX "IX_agents_HospitalId" ON "agents" ("HospitalId");

CREATE INDEX "IX_agents_LastSeenAt" ON "agents" ("LastSeenAt");

CREATE INDEX "IX_agents_Status" ON "agents" ("Status");

CREATE INDEX "EmailIndex" ON "AspNetUsers" ("NormalizedEmail");

CREATE INDEX "IX_AspNetUsers_HospitalId" ON "AspNetUsers" ("HospitalId");

CREATE UNIQUE INDEX "UserNameIndex" ON "AspNetUsers" ("NormalizedUserName");

CREATE INDEX "IX_infrastructure_assets_CollectorId" ON "infrastructure_assets" ("CollectorId");

CREATE INDEX "IX_infrastructure_assets_HospitalId" ON "infrastructure_assets" ("HospitalId");

CREATE UNIQUE INDEX "IX_infrastructure_assets_Hostname" ON "infrastructure_assets" ("Hostname");

CREATE INDEX "IX_infrastructure_assets_PolicyId" ON "infrastructure_assets" ("PolicyId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260715041915_AddHospitalHierarchy', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260715042320_ConfigureHospitalRelationsAndFilters', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260715063819_ApplyHospitalAuthorizationFilters', '8.0.4');

COMMIT;

BEGIN TRANSACTION;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260715081340_AddHospitalHierarchyV2', '8.0.4');

COMMIT;

