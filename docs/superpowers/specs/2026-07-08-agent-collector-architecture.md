# Design Spec: Agent Collector Architecture (Unified Master/Client EDR Agent)

- **Date:** 2026-07-08
- **Author:** Antigravity (Security Architect)
- **Status:** Proposed

---

## 1. Goal & Background

To represent a realistic enterprise security monitoring environment (SIEM/EDR), we need to transition from direct endpoint-to-server communications to an aggregated topology. 

In this new design:
1. Each hospital deploys a single **Master Agent (Log Collector)** on their local network.
2. Individual servers (e.g. database server, clinical application host) run lightweight **Sub-agents (Endpoint Clients)**.
3. Sub-agents send all metrics and security events to their local Master Agent.
4. The Master Agent acts as a buffer and gateway: it registers sub-agents, forwards their telemetry, and securely stores events on disk in an **Offline Buffer Queue** if the connection to the Central Server is lost. When connection is restored, it flushes the buffered events to prevent data loss.

---

## 2. Proposed Changes

We will modify the Go Agent (`agent/main.go`) to support two operating modes via the CLI `-mode` flag.

### A. Go Agent CLI Flags & Global Config
We will extend command-line flags in the Go Agent:
- `-mode`: Operating mode, either `master` (Collector) or `client` (Endpoint). Defaults to `client`.
- `-collector`: The base URL of the Master Agent (e.g. `http://localhost:9000`). Used only in `client` mode. If empty, the client falls back to communicating directly with the Central Server.

### B. Client Mode (`-mode client`)
- **Heartbeat Loop:** Sends the heartbeat payload to the local Master Agent (`http://localhost:9000/api/collector/heartbeat`) instead of the Central Server.
- **Event Reporting:** Sends security event reports to the Master Agent (`http://localhost:9000/api/collector/events`).
- **Web Portal:** Still hosts the local mock HIS portal.

### C. Master Mode (`-mode master`)
- **Web Server:** Starts a lightweight HTTP server on the configured port (e.g., `9000` or `9001`) with the following endpoints:
  - `POST /api/collector/heartbeat`: Receives a heartbeat from a sub-agent, adds a custom header or metadata, and forwards it to the Central Server (`http://localhost:5000/api/agents/{agentId}/heartbeat`).
  - `POST /api/collector/events`: Receives a list of `EventReport` objects from a sub-agent.
- **Offline Buffering Logic (Stateful Queue):**
  - Inside `POST /api/collector/events`:
    - The Master Agent checks if the Central Server is reachable.
    - If **online**, it forwards the events immediately to `http://localhost:5000/api/events`.
    - If **offline** (connection refused or timeout), it appends the events to a local file named `offline_buffer.json` (JSON Lines format) and logs:
      `[WARNING] Central Server offline. Event buffered in offline queue (File: offline_buffer.json)`.
- **Offline Recovery Worker:**
  - A background goroutine checks connection to the Central Server every 5 seconds.
  - If the server is back online and `offline_buffer.json` exists:
    - It reads the file, parses all buffered events, sends them in a single batch to `http://localhost:5000/api/events`.
    - Upon receiving a successful HTTP `200 OK`, it deletes the `offline_buffer.json` file.
- **Heartbeat:** Sends the Master Agent's own heartbeats to the Central Server to report health.

---

## 3. Data Flows

### Real-Time Flow (Online)
```
Sub-agent (Port 8080) -> SQLi Event -> Master Agent (Port 9000) -> Central Server (Port 5000) -> SignalR -> Dashboard (Port 5174)
```

### Buffering & Recovery Flow (Offline)
```
[Central Server Offline]
Sub-agent -> XSS Event -> Master Agent -> (Fails sending) -> Write to offline_buffer.json

[Central Server Restored]
Master Agent Worker -> Detects Online -> Reads offline_buffer.json -> Batch Send -> Central Server -> Dashboard
```

---

## 4. Verification Plan

We will perform manual testing to verify this architecture:
1. Start the Central Server (Port 5000) and Dashboard (Port 5174).
2. Start **Hospital A Master Agent** on port 9000:
   `.\onesecurity-agent.exe -mode master -port 9000 -agentid agent-master-hosp-a`
3. Start **Hospital A Sub-agent** on port 8080:
   `.\onesecurity-agent.exe -mode client -port 8080 -agentid agent-win-his-01 -collector http://localhost:9000`
4. Verify on the Dashboard that both the Master Agent (`agent-master-hosp-a`) and the Sub-agent (`agent-win-his-01`) show up as **Online**.
5. Stop the Central Server process.
6. Trigger a SQL Injection attack on `http://localhost:8080/`. Verify that:
   - The sub-agent logs the attack and forwards it to the collector at port 9000.
   - The Master Agent prints: `[WARNING] Central Server offline. Event buffered in offline queue...`
   - A file `offline_buffer.json` is created in the agent folder containing the event.
7. Start the Central Server again.
8. Verify that the Master Agent background worker detects the connection, flushes the event, and `offline_buffer.json` is deleted.
9. Verify that the SQL Injection alert immediately appears on the Dashboard with the correct original timestamp.
