# Task 2 Report: Implement Master Mode Collector Server

## Overview
We have successfully implemented the Master Mode log collector server for the OneSecurity Agent, allowing it to act as a collector proxy on local networks.

## Implementation Details

1. **Test-Driven Development (TDD) Workflow:**
   - Designed and wrote `TestCollectorServer` in [main_test.go](file:///C:/Users/MinhHQ/.gemini/antigravity/scratch/onesecurity/agent/main_test.go) to verify endpoints. The test creates a mock Central Server using `httptest.NewServer`, starts the Collector Server in a background goroutine on a dynamically selected free port, issues requests to `/api/collector/heartbeat` and `/api/collector/events`, and checks that payloads are forwarded correctly with the proper headers (including `X-Api-Key`).
   - Ran the tests and verified they failed (compilation failed because `startCollectorServer` was undefined).

2. **Collector Server (`startCollectorServer`):**
   - Implemented `startCollectorServer(port int)` in [main.go](file:///C:/Users/MinhHQ/.gemini/antigravity/scratch/onesecurity/agent/main.go) using a custom `http.ServeMux` to avoid polluting the global HTTP packet namespace.
   - Exposed routes:
     - `POST /api/collector/heartbeat` -> handled by `handleCollectorHeartbeat`
     - `POST /api/collector/events` -> handled by `handleCollectorEvents`

3. **HTTP Handlers:**
   - **`handleCollectorHeartbeat`:** Extracts `agentId` from the request query parameters, reads the request body, and forwards it to `globalConfig.ServerUrl + "/api/agents/{agentId}/heartbeat"` with headers `Content-Type: application/json` and `X-Api-Key: globalConfig.ApiKey`. The response status code and body from the Central Server are successfully written back to the client.
   - **`handleCollectorEvents`:** Reads the request body containing client agent events and forwards it to `globalConfig.ServerUrl + "/api/events"`. Headers, response status code, and body are written back to the client.

4. **Integration into main lifecycle:**
   - Modified `main()` in [main.go](file:///C:/Users/MinhHQ/.gemini/antigravity/scratch/onesecurity/agent/main.go) to detect when the Agent is configured with `Mode: "master"`. If so, it extracts the target port from the `CollectorUrl` (or defaults to `9000` if not specified) and spawns the collector server in a background goroutine before continuing execution of the agent.

## Test Execution Results

Running the tests from the agent folder yielded successful runs for all test suites:

```
C:\Users\MinhHQ\.gemini\antigravity\scratch\onesecurity\tools\go\bin\go.exe test -v .
=== RUN   TestParseFlagsOverrides
--- PASS: TestParseFlagsOverrides (0.00s)
=== RUN   TestCollectorServer
Starting Master Mode Collector Server on: http://localhost:52931/
[Collector] Forwarding heartbeat for agent client-agent-007 to Central Server
[Collector] Forwarding events to Central Server
--- PASS: TestCollectorServer (0.16s)
PASS
ok  	onesecurity-agent	0.804s
```

All implemented functionality is thoroughly tested and passes cleanly.
