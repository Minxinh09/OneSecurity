# Task 2 Brief: Implement Master Mode Collector Server

## Goal:
Implement the Master Mode log collector server that listens on a dedicated port, exposes endpoints `POST /api/collector/heartbeat` and `POST /api/collector/events`, and forwards the payloads to the Central Server.

## Scope:
- In `agent/main.go`, implement `startCollectorServer(port int)` which starts a local web server handling:
  - `POST /api/collector/heartbeat`: Receives client heartbeats. Extracts target `agentId` from query string (`r.URL.Query().Get("agentId")`), then forwards the heartbeat payload to the Central Server endpoint `http://localhost:5000/api/agents/{agentId}/heartbeat` using HTTP POST with header `X-Api-Key` and `Content-Type`.
  - `POST /api/collector/events`: Receives client events. Forwards the payload to `http://localhost:5000/api/events`.
- Write unit tests in `agent/main_test.go` to mock a local receiver and verify that `startCollectorServer` runs and handles endpoints without errors.
- Run tests using `..\tools\go\bin\go.exe test -v ./agent` and ensure they pass.
