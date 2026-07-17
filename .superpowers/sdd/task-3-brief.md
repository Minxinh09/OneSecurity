# Task 3 Brief: Client Mode Telemetry Redirection

## Goal:
Redirect heartbeat loops and event reporting in Client Mode to point to the configured `-collector` URL instead of the Central Server.

## Scope:
- In `agent/main.go`, implement helper functions:
  - `getEventEndpoint() string`: Returns the target event endpoint. If running as a client and `globalConfig.CollectorUrl` is set, returns `globalConfig.CollectorUrl + "/api/collector/events"`. Else, returns `globalConfig.ServerUrl + "/api/events"`.
  - `getHeartbeatEndpoint(agentId string) string`: Returns the target heartbeat endpoint. If running as a client and `globalConfig.CollectorUrl` is set, returns `globalConfig.CollectorUrl + "/api/collector/heartbeat?agentId=" + agentId`. Else, returns `globalConfig.ServerUrl + "/api/agents/" + agentId + "/heartbeat"`.
- Modify `sendEvent(ev EventReport)` in `agent/main.go` to send requests to `getEventEndpoint()`.
- Modify `startHeartbeatLoop()` in `agent/main.go` to send heartbeat requests to `getHeartbeatEndpoint(globalConfig.AgentId)`.
- Write unit tests in `agent/main_test.go` to verify endpoint redirection when `CollectorUrl` is configured.
- Run tests using `..\tools\go\bin\go.exe test -v ./agent` and ensure they pass.
