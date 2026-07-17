# Task 4 Report: Offline Queue Buffering and Worker Recovery

## Overview
We have successfully implemented file-backed offline event buffering and background recovery mechanics for the OneSecurity Agent's Collector Server. When the Central Server goes offline, collector events are safely written to `offline_buffer.json` (JSON Lines format). A background recovery worker periodically checks for restored connectivity to flush the offline buffer back to the Central Server.

## Implementation Details

1. **Test-Driven Development (TDD) Workflow:**
   - Designed and wrote `TestOfflineBufferAndRecovery` in [main_test.go](file:///C:/Users/MinhHQ/.gemini/antigravity/scratch/onesecurity/agent/main_test.go).
   - The test simulates a failure scenario where the Central Server is unreachable, ensuring:
     - Events sent to `handleCollectorEvents` return a `200 OK` response with `{"buffered":true}`.
     - Events are successfully stored in `offline_buffer.json`.
     - The recovery worker detects when the Central Server comes back online, pings `/api/overview`, flushes all buffered events in a single batch to `/api/events`, and deletes the buffer file.
   - Run the tests initially to verify compilation/test failure as variables/stubs were not yet present.

2. **Offline Buffering Helper Functions:**
   - **`appendOfflineBuffer(eventBytes []byte)`**: Thread-safe function that locks a global `offlineMutex sync.Mutex`, decodes a JSON payload (either a single event or a slice of events), and appends each event as a single line in `offline_buffer.json` (JSON Lines format).
   - **`flushOfflineBuffer()`**: Locked function that opens `offline_buffer.json`, reads all events line-by-line, parses them into a batch (`[]EventReport`), closes the file, and forwards them in a single batch to the Central Server's `/api/events` endpoint. If successful, deletes `offline_buffer.json`.

3. **Background Recovery Worker:**
   - **`startOfflineRecoveryWorker()`**: Spins up a background goroutine using a configurable ticker `recoveryInterval` (defaulting to 5 seconds, set to 100ms in testing).
   - The worker periodically checks if `offline_buffer.json` exists. If it does, it pings the Central Server's `/api/overview` endpoint. Upon a successful HTTP `200 OK` response, it invokes `flushOfflineBuffer()`.

4. **Integration Hooks:**
   - **`handleCollectorEvents`**: Modified the handler in [main.go](file:///C:/Users/MinhHQ/.gemini/antigravity/scratch/onesecurity/agent/main.go) to catch errors when forwarding events to the Central Server. If forwarding fails, it appends the events to the offline buffer and returns a response containing `{"buffered":true}`.
   - **`main()`**: Updated to call `startOfflineRecoveryWorker()` alongside `startCollectorServer()` when the agent is configured in `"master"` mode.

## Test Execution Results

Running the tests from the workspace root yielded successful runs for all test suites:

```
C:\Users\MinhHQ\.gemini\antigravity\scratch\onesecurity\tools\go\bin\go.exe test -v ./agent
=== RUN   TestParseFlagsOverrides
--- PASS: TestParseFlagsOverrides (0.00s)
=== RUN   TestCollectorServer
Starting Master Mode Collector Server on: http://localhost:53326/
[Collector] Forwarding heartbeat for agent client-agent-007 to Central Server
[Collector] Forwarding events to Central Server
--- PASS: TestCollectorServer (0.13s)
=== RUN   TestGetEndpoints
=== RUN   TestGetEndpoints/Client_mode_with_CollectorUrl
=== RUN   TestGetEndpoints/Client_mode_with_empty_CollectorUrl
=== RUN   TestGetEndpoints/Master_mode_with_CollectorUrl
=== RUN   TestGetEndpoints/Master_mode_with_empty_CollectorUrl
=== RUN   TestGetEndpoints/Simulator_mode_with_CollectorUrl
=== RUN   TestGetEndpoints/Client_mode_case_insensitive_mode_test
--- PASS: TestGetEndpoints (0.00s)
    --- PASS: TestGetEndpoints/Client_mode_with_CollectorUrl (0.00s)
    --- PASS: TestGetEndpoints/Client_mode_with_empty_CollectorUrl (0.00s)
    --- PASS: TestGetEndpoints/Master_mode_with_CollectorUrl (0.00s)
    --- PASS: TestGetEndpoints/Master_mode_with_empty_CollectorUrl (0.00s)
    --- PASS: TestGetEndpoints/Simulator_mode_with_CollectorUrl (0.00s)
    --- PASS: TestGetEndpoints/Client_mode_case_insensitive_mode_test (0.00s)
=== RUN   TestOfflineBufferAndRecovery
[Collector] Forwarding events to Central Server
[Collector] Error contacting Central Server: Post "http://localhost:59999/api/events": dial tcp [::1]:59999: connectex: No connection could be made because the target machine actively refused it.. Appending to offline buffer.
Successfully flushed 1 events from offline buffer.
--- PASS: TestOfflineBufferAndRecovery (0.32s)
PASS
ok  	onesecurity-agent	1.068s
```

All implemented functionality passes all unit tests successfully.
