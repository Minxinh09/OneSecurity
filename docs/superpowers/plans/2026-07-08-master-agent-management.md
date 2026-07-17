# Master Agent Management & Hybrid Offline Monitoring Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend the Go Agent in Master mode to act as a local hospital coordinator that dynamically discovers clients, persists them to a local JSON file, and performs hybrid active checks (Ping + Port check) to diagnose and report offline reasons.

**Architecture:** 
1. The Go Agent in Master mode will serve a new register endpoint `/api/collector/register` to intercept client registrations.
2. The registry state is persisted in `discovered_agents.json` on disk and updated in memory using thread-safe maps.
3. A background sweeper sweeps every 10 seconds. For clients inactive for > 30 seconds, it runs an OS ping command and a TCP handshake to diagnose the fault.
4. Offline events are forwarded to the Central Server using a new `agent_status` category, triggering a critical alert in the `RulesEngine.cs`.

**Tech Stack:** Go (net, net/http, os/exec), ASP.NET Core 8, SQLite, EF Core

## Global Constraints
- Go Version: 1.22+
- .NET version: 8.0 LTS
- Standardize all timestamps to ISO 8601 UTC formats.
- Avoid requiring administrator privileges for execution (use OS ping command execution for ICMP).

---

### Task 1: Load and Save `discovered_agents.json` in Go Agent

**Files:**
- Modify: `agent/main.go`
- Modify: `agent/main_test.go`

**Interfaces:**
- Produces: `loadDiscoveredAgents()`, `saveDiscoveredAgents()`, `DiscoveredAgent` struct.

- [ ] **Step 1: Write the failing test for JSON storage**
  Add the following test to `agent/main_test.go`:
  ```go
  func TestLoadAndSaveDiscoveredAgents(t *testing.T) {
      tempFile := "discovered_agents_test.json"
      defer os.Remove(tempFile)

      agents := map[string]*DiscoveredAgent{
          "test-agent-1": {
              AgentId:      "test-agent-1",
              Hostname:     "host-1",
              IpAddress:    "192.168.1.100",
              Port:         8080,
              HospitalCode: "HOSP_A",
              Status:       "online",
          },
      }

      err := saveDiscoveredAgentsToFile(tempFile, agents)
      if err != nil {
          t.Fatalf("Failed to save agents: %v", err)
      }

      loaded, err := loadDiscoveredAgentsFromFile(tempFile)
      if err != nil {
          t.Fatalf("Failed to load agents: %v", err)
      }

      if len(loaded) != 1 || loaded["test-agent-1"].Hostname != "host-1" {
          t.Errorf("Loaded agents mismatch. Got %+v", loaded)
      }
  }
  ```

- [ ] **Step 2: Run test to verify it fails**
  Run: `powershell -ExecutionPolicy Bypass -Command ". ..\load_env.ps1; go test -v -run TestLoadAndSaveDiscoveredAgents"`
  Expected: FAIL with undefined struct/functions.

- [ ] **Step 3: Define Struct and Load/Save implementation in `agent/main.go`**
  Add the struct and storage functions to `agent/main.go` (before the web server code):
  ```go
  type DiscoveredAgent struct {
  	AgentId      string    `json:"agentId"`
  	Hostname     string    `json:"hostname"`
  	IpAddress    string    `json:"ipAddress"`
  	Port         int       `json:"port"`
  	HospitalCode string    `json:"hospitalCode"`
  	Status       string    `json:"status"`
  	LastSeen     time.Time `json:"-"`
  }

  var (
  	discoveredAgents = make(map[string]*DiscoveredAgent)
  	agentsMutex      sync.RWMutex
  	discoveredFile   = "discovered_agents.json"
  )

  func loadDiscoveredAgentsFromFile(filePath string) (map[string]*DiscoveredAgent, error) {
  	file, err := os.Open(filePath)
  	if err != nil {
  		if os.IsNotExist(err) {
  			return make(map[string]*DiscoveredAgent), nil
  		}
  		return nil, err
  	}
  	defer file.Close()

  	var list []*DiscoveredAgent
  	err = json.NewDecoder(file).Decode(&list)
  	if err != nil {
  		return nil, err
  	}

  	m := make(map[string]*DiscoveredAgent)
  	for _, a := range list {
  		a.LastSeen = time.Now() // assume seen now on load
  		m[a.AgentId] = a
  	}
  	return m, nil
  }

  func saveDiscoveredAgentsToFile(filePath string, agents map[string]*DiscoveredAgent) error {
  	agentsMutex.RLock()
  	var list []*DiscoveredAgent
  	for _, a := range agents {
  		list = append(list, a)
  	}
  	agentsMutex.RUnlock()

  	data, err := json.MarshalIndent(list, "", "  ")
  	if err != nil {
  		return err
  	}

  	return os.WriteFile(filePath, data, 0644)
  }
  ```
  Also, initialize the registry inside `main()` of `agent/main.go` if running in master mode:
  ```go
  	// Start Collector Server if in Master mode
  	if strings.ToLower(globalConfig.Mode) == "master" {
  		loaded, err := loadDiscoveredAgentsFromFile(discoveredFile)
  		if err == nil {
  			agentsMutex.Lock()
  			discoveredAgents = loaded
  			agentsMutex.Unlock()
  			fmt.Printf("Loaded %d discovered agents from registry.\n", len(loaded))
  		}
  ```

- [ ] **Step 4: Run test to verify it passes**
  Run: `powershell -ExecutionPolicy Bypass -Command ". ..\load_env.ps1; go test -v -run TestLoadAndSaveDiscoveredAgents"`
  Expected: PASS

- [ ] **Step 5: Commit**
  Run: `git add agent/main.go agent/main_test.go`
  Run: `git commit -m "feat: implement local persistence for discovered agents"`

---

### Task 2: Dynamic Discovery & Registration Forwarding

**Files:**
- Modify: `agent/main.go`
- Modify: `agent/main_test.go`

**Interfaces:**
- Consumes: `DiscoveredAgent` struct, `saveDiscoveredAgentsToFile`
- Produces: HTTP Endpoint `/api/collector/register` on Master Agent, Client redirects registration to Collector in Client mode.

- [ ] **Step 1: Write the failing test for registration forwarding**
  Add the test in `agent/main_test.go`:
  ```go
  func TestCollectorRegistrationForwarding(t *testing.T) {
      tempFile := "discovered_agents_test.json"
      defer os.Remove(tempFile)
      
      // Clear global state
      agentsMutex.Lock()
      discoveredAgents = make(map[string]*DiscoveredAgent)
      discoveredFile = tempFile
      agentsMutex.Unlock()

      // Mock Central Server
      centralServer := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
          if r.URL.Path != "/api/agents/register" {
              t.Errorf("Unexpected path: %s", r.URL.Path)
          }
          w.WriteHeader(http.StatusOK)
          w.Write([]byte(`{"serverId": 1, "config": {}}`))
      }))
      defer centralServer.Close()

      globalConfig.ServerUrl = centralServer.URL
      globalConfig.ApiKey = "test-api-key"

      // Mock Register Payload
      regReq := RegisterRequest{
          AgentId:      "client-agent-123",
          Hostname:     "client-host",
          OsType:       "windows",
          OsVersion:    "10",
          IpAddress:    "127.0.0.1",
          HospitalCode: "HOSP_A",
      }
      bodyBytes, _ := json.Marshal(regReq)

      req := httptest.NewRequest("POST", "/api/collector/register", bytes.NewBuffer(bodyBytes))
      w := httptest.NewRecorder()

      handleCollectorRegister(w, req)

      if w.Code != http.StatusOK {
          t.Fatalf("Expected code 200, got %d", w.Code)
      }

      agentsMutex.RLock()
      agent, exists := discoveredAgents["client-agent-123"]
      agentsMutex.RUnlock()

      if !exists {
          t.Fatal("Expected agent to be discovered dynamically")
      }
      if agent.Hostname != "client-host" || agent.Port != 8080 {
          t.Errorf("Agent fields incorrect: %+v", agent)
      }
  }
  ```

- [ ] **Step 2: Run test to verify it fails**
  Run: `powershell -ExecutionPolicy Bypass -Command ". ..\load_env.ps1; go test -v -run TestCollectorRegistrationForwarding"`
  Expected: FAIL with undefined `handleCollectorRegister`.

- [ ] **Step 3: Implement handler and route in `agent/main.go`**
  Modify `startCollectorServer(port int)` in `agent/main.go` to add the route:
  ```go
  func startCollectorServer(port int) {
  	mux := http.NewServeMux()
  	mux.HandleFunc("/api/collector/register", handleCollectorRegister)
  	mux.HandleFunc("/api/collector/heartbeat", handleCollectorHeartbeat)
  	mux.HandleFunc("/api/collector/events", handleCollectorEvents)
  ```
  Implement the handler `handleCollectorRegister`:
  ```go
  func handleCollectorRegister(w http.ResponseWriter, r *http.Request) {
  	if r.Method != http.MethodPost {
  		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
  		return
  	}

  	bodyBytes, err := io.ReadAll(r.Body)
  	if err != nil {
  		http.Error(w, "Failed to read request body", http.StatusBadRequest)
  		return
  	}
  	defer r.Body.Close()

  	// Forward to Central Server
  	url := fmt.Sprintf("%s/api/agents/register", globalConfig.ServerUrl)
  	fmt.Println("[Collector] Forwarding registration request to Central Server")

  	req, err := http.NewRequest(http.MethodPost, url, bytes.NewBuffer(bodyBytes))
  	if err != nil {
  		http.Error(w, "Failed to create request", http.StatusInternalServerError)
  		return
  	}

  	req.Header.Set("Content-Type", "application/json")
  	req.Header.Set("X-Api-Key", globalConfig.ApiKey)

  	resp, err := client.Do(req)
  	if err != nil {
  		fmt.Printf("[Collector] Error forwarding registration: %v\n", err)
  		http.Error(w, "Failed to contact Central Server", http.StatusBadGateway)
  		return
  	}
  	defer resp.Body.Close()

  	respBody, _ := io.ReadAll(resp.Body)

  	if resp.StatusCode == http.StatusOK {
  		var regReq RegisterRequest
  		if err := json.Unmarshal(bodyBytes, &regReq); err == nil {
  			agentsMutex.Lock()
  			discoveredAgents[regReq.AgentId] = &DiscoveredAgent{
  				AgentId:      regReq.AgentId,
  				Hostname:     regReq.Hostname,
  				IpAddress:    regReq.IpAddress,
  				Port:         8080, // Default HIS Portal port
  				HospitalCode: regReq.HospitalCode,
  				Status:       "online",
  				LastSeen:     time.Now(),
  			}
  			agentsMutex.Unlock()
  			saveDiscoveredAgentsToFile(discoveredFile, discoveredAgents)
  			fmt.Printf("[Collector] Dynamically discovered agent: %s (%s)\n", regReq.AgentId, regReq.Hostname)
  		}
  	}

  	// Copy response headers and write back body
  	for k, vv := range resp.Header {
  		for _, v := range vv {
  			w.Header().Add(k, v)
  		}
  	}
  	w.WriteHeader(resp.StatusCode)
  	w.Write(respBody)
  }
  ```
  Now, update client registration endpoint redirection in `agent/main.go`:
  ```go
  func registerAgent(ipAddress string) {
  	url := fmt.Sprintf("%s/api/agents/register", globalConfig.ServerUrl)
  	if strings.ToLower(globalConfig.Mode) == "client" && globalConfig.CollectorUrl != "" {
  		url = fmt.Sprintf("%s/api/collector/register", globalConfig.CollectorUrl)
  	}
  ```

- [ ] **Step 4: Run test to verify it passes**
  Run: `powershell -ExecutionPolicy Bypass -Command ". ..\load_env.ps1; go test -v -run TestCollectorRegistrationForwarding"`
  Expected: PASS

- [ ] **Step 5: Commit**
  Run: `git add agent/main.go agent/main_test.go`
  Run: `git commit -m "feat: implement register forwarding and discovery on collector"`

---

### Task 3: Heartbeat & Status Recovery Integration

**Files:**
- Modify: `agent/main.go`
- Modify: `agent/main_test.go`

**Interfaces:**
- Consumes: `discoveredAgents` map, `saveDiscoveredAgentsToFile`
- Produces: Updates `LastSeen` on heartbeats, handles Recovery status transition.

- [ ] **Step 1: Write the failing test for heartbeat status transitions**
  Add the test to `agent/main_test.go`:
  ```go
  func TestCollectorHeartbeatStatusTransitions(t *testing.T) {
      tempFile := "discovered_agents_test.json"
      defer os.Remove(tempFile)

      // Mock Central Server for heartbeat & events forwarding
      eventReceived := false
      centralServer := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
          if strings.Contains(r.URL.Path, "heartbeat") {
              w.WriteHeader(http.StatusOK)
              w.Write([]byte(`{"status":"ok"}`))
          } else if r.URL.Path == "/api/events" {
              eventReceived = true
              w.WriteHeader(http.StatusOK)
              w.Write([]byte(`{"received":1}`))
          }
      }))
      defer centralServer.Close()

      globalConfig.ServerUrl = centralServer.URL
      globalConfig.ApiKey = "test-api-key"
      discoveredFile = tempFile

      agentsMutex.Lock()
      discoveredAgents = map[string]*DiscoveredAgent{
          "offline-agent": {
              AgentId:      "offline-agent",
              Hostname:     "host-offline",
              IpAddress:    "127.0.0.1",
              Port:         8080,
              HospitalCode: "HOSP_A",
              Status:       "offline",
              LastSeen:     time.Now().Add(-10 * time.Minute),
          },
      }
      agentsMutex.Unlock()

      // Trigger heartbeat for the offline agent
      req := httptest.NewRequest("POST", "/api/collector/heartbeat?agentId=offline-agent", bytes.NewBuffer([]byte(`{}`)))
      w := httptest.NewRecorder()

      handleCollectorHeartbeat(w, req)

      if w.Code != http.StatusOK {
          t.Fatalf("Heartbeat failed: %d", w.Code)
      }

      agentsMutex.RLock()
      agent := discoveredAgents["offline-agent"]
      agentsMutex.RUnlock()

      if agent.Status != "online" {
          t.Errorf("Expected agent to recover to online, got %s", agent.Status)
      }
      if !eventReceived {
          t.Error("Expected recovery event to be dispatched to Central Server")
      }
  }
  ```

- [ ] **Step 2: Run test to verify it fails**
  Run: `powershell -ExecutionPolicy Bypass -Command ". ..\load_env.ps1; go test -v -run TestCollectorHeartbeatStatusTransitions"`
  Expected: FAIL (agent does not recover, no event is sent).

- [ ] **Step 3: Modify `handleCollectorHeartbeat` in `agent/main.go`**
  Modify `handleCollectorHeartbeat` in `agent/main.go`:
  ```go
  func handleCollectorHeartbeat(w http.ResponseWriter, r *http.Request) {
  	if r.Method != http.MethodPost {
  		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
  		return
  	}

  	agentId := r.URL.Query().Get("agentId")
  	if agentId == "" {
  		http.Error(w, "Missing agentId parameter", http.StatusBadRequest)
  		return
  	}

  	bodyBytes, err := io.ReadAll(r.Body)
  	if err != nil {
  		http.Error(w, "Failed to read request body", http.StatusBadRequest)
  		return
  	}
  	defer r.Body.Close()

  	// Forward to Central Server
  	url := fmt.Sprintf("%s/api/agents/%s/heartbeat", globalConfig.ServerUrl, agentId)
  	fmt.Printf("[Collector] Forwarding heartbeat for agent %s to Central Server\n", agentId)

  	req, err := http.NewRequest(http.MethodPost, url, bytes.NewBuffer(bodyBytes))
  	if err != nil {
  		http.Error(w, "Failed to create request", http.StatusInternalServerError)
  		return
  	}

  	req.Header.Set("Content-Type", "application/json")
  	req.Header.Set("X-Api-Key", globalConfig.ApiKey)

  	resp, err := client.Do(req)
  	if err != nil {
  		fmt.Printf("[Collector] Error contacting Central Server: %v\n", err)
  		http.Error(w, "Failed to contact Central Server", http.StatusBadGateway)
  		return
  	}
  	defer resp.Body.Close()

  	if resp.StatusCode == http.StatusOK {
  		clientIp, _, _ := net.SplitHostPort(r.RemoteAddr)
  		if clientIp == "" || clientIp == "[::1]" || clientIp == "::1" {
  			clientIp = "127.0.0.1"
  		}

  		agentsMutex.Lock()
  		agent, exists := discoveredAgents[agentId]
  		if !exists {
  			// Discover dynamically
  			agent = &DiscoveredAgent{
  				AgentId:      agentId,
  				Hostname:     "Unknown-" + agentId,
  				IpAddress:    clientIp,
  				Port:         8080,
  				HospitalCode: globalConfig.HospitalCode,
  				Status:       "online",
  				LastSeen:     time.Now(),
  			}
  			discoveredAgents[agentId] = agent
  			agentsMutex.Unlock()
  			saveDiscoveredAgentsToFile(discoveredFile, discoveredAgents)
  		} else {
  			agent.LastSeen = time.Now()
  			if agent.Status == "offline" {
  				agent.Status = "online"
  				agentsMutex.Unlock()
  				saveDiscoveredAgentsToFile(discoveredFile, discoveredAgents)
  				
  				// Send recovery event to central server
  				fmt.Printf("[Collector] Agent %s recovered online. Sending event.\n", agentId)
  				go sendEvent(EventReport{
  					EventId:   generateUUID(),
  					AgentId:   agentId,
  					Timestamp: time.Now().UTC(),
  					Category:  "agent_status",
  					Severity:  "info",
  					Source:    "master_agent",
  					Title:     "Agent Online (Host: " + agent.Hostname + ")",
  					Details:   fmt.Sprintf("Agent %s has recovered. Received active heartbeat.", agentId),
  					RawData:   fmt.Sprintf(`{"AgentId":"%s","Status":"online"}`, agentId),
  				})
  			} else {
  				agentsMutex.Unlock()
  			}
  		}
  	}

  	// Copy response headers
  	for k, vv := range resp.Header {
  		for _, v := range vv {
  			w.Header().Add(k, v)
  		}
  	}
  	w.WriteHeader(resp.StatusCode)
  	w.Write([]byte(`{"status":"ok"}`))
  }
  ```

- [ ] **Step 4: Run test to verify it passes**
  Run: `powershell -ExecutionPolicy Bypass -Command ". ..\load_env.ps1; go test -v -run TestCollectorHeartbeatStatusTransitions"`
  Expected: PASS

- [ ] **Step 5: Commit**
  Run: `git add agent/main.go agent/main_test.go`
  Run: `git commit -m "feat: implement dynamic recovery event dispatch on heartbeat"`

---

### Task 4: Active Sweeper & Hybrid Check (Ping + TCP Port Check)

**Files:**
- Modify: `agent/main.go`
- Modify: `agent/main_test.go`

**Interfaces:**
- Consumes: `discoveredAgents` map
- Produces: background Sweeper routine running Ping / TCP connect port checks.

- [ ] **Step 1: Write the failing test for Hybrid Check Offline detection**
  Add the test in `agent/main_test.go`:
  ```go
  func TestSweeperOfflineDetection(t *testing.T) {
      tempFile := "discovered_agents_test.json"
      defer os.Remove(tempFile)

      eventsReceived := 0
      centralServer := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
          if r.URL.Path == "/api/events" {
              eventsReceived++
              w.WriteHeader(http.StatusOK)
              w.Write([]byte(`{"received":1}`))
          }
      }))
      defer centralServer.Close()

      globalConfig.ServerUrl = centralServer.URL
      globalConfig.ApiKey = "test-api-key"
      discoveredFile = tempFile

      agentsMutex.Lock()
      discoveredAgents = map[string]*DiscoveredAgent{
          "crashed-agent": {
              AgentId:      "crashed-agent",
              Hostname:     "host-crash",
              IpAddress:    "127.0.0.1",
              Port:         9999, // closed port to simulate process crash
              HospitalCode: "HOSP_A",
              Status:       "online",
              LastSeen:     time.Now().Add(-1 * time.Hour), // Needs verification
          },
      }
      agentsMutex.Unlock()

      // Run sweeper check once manually
      performRegistrySweep()

      agentsMutex.RLock()
      status := discoveredAgents["crashed-agent"].Status
      agentsMutex.RUnlock()

      if status != "offline" {
          t.Errorf("Expected status to be offline, got %s", status)
      }
      if eventsReceived != 1 {
          t.Errorf("Expected 1 offline event sent, got %d", eventsReceived)
      }
  }
  ```

- [ ] **Step 2: Run test to verify it fails**
  Run: `powershell -ExecutionPolicy Bypass -Command ". ..\load_env.ps1; go test -v -run TestSweeperOfflineDetection"`
  Expected: FAIL with undefined `performRegistrySweep`.

- [ ] **Step 3: Implement Sweeper & Check functions in `agent/main.go`**
  Add the sweeper functions to `agent/main.go`:
  ```go
  func startLocalSweeper() {
  	go func() {
  		ticker := time.NewTicker(10 * time.Second)
  		defer ticker.Stop()
  		for range ticker.C {
  			performRegistrySweep()
  		}
  	}()
  }

  func performRegistrySweep() {
  	agentsMutex.RLock()
  	var expired []*DiscoveredAgent
  	for _, agent := range discoveredAgents {
  		if agent.Status == "online" && time.Since(agent.LastSeen) > 30*time.Second {
  			expired = append(expired, agent)
  		}
  	}
  	agentsMutex.RUnlock()

  	for _, agent := range expired {
  		go checkAgentStatus(agent)
  	}
  }

  func checkAgentStatus(agent *DiscoveredAgent) {
  	pingOk := performPingCheck(agent.IpAddress)
  	portOk := performPortCheck(agent.IpAddress, agent.Port)

  	if !pingOk || !portOk {
  		agentsMutex.Lock()
  		// Check double-locking status since other heartbeat might have arrived
  		if agent.Status != "online" {
  			agentsMutex.Unlock()
  			return
  		}
  		agent.Status = "offline"
  		agentsMutex.Unlock()

  		saveDiscoveredAgentsToFile(discoveredFile, discoveredAgents)

  		var title string
  		var details string
  		var diagnostic string

  		if pingOk && !portOk {
  			title = "Agent Process Stopped (Host: " + agent.Hostname + ")"
  			details = fmt.Sprintf("Ping ICMP was successful (Host is ALIVE), but TCP Port %d connection failed (Agent process has CRASHED/STOPPED).", agent.Port)
  			diagnostic = "process_dead"
  		} else {
  			title = "Host Offline / Unreachable (Host: " + agent.Hostname + ")"
  			details = fmt.Sprintf("Ping ICMP failed and TCP Port %d is unreachable. Server might be down or network disconnected.", agent.Port)
  			diagnostic = "network_down"
  		}

  		fmt.Printf("[Collector] Agent offline detected: %s. %s\n", agent.AgentId, title)
  		sendEvent(EventReport{
  			EventId:   generateUUID(),
  			AgentId:   agent.AgentId,
  			Timestamp: time.Now().UTC(),
  			Category:  "agent_status",
  			Severity:  "critical",
  			Source:    "master_agent",
  			Title:     title,
  			Details:   details,
  			RawData:   fmt.Sprintf(`{"ClientIP":"%s","PingOk":%t,"PortOk":%t,"Diagnostic":"%s"}`, agent.IpAddress, pingOk, portOk, diagnostic),
  		})
  	}
  }

  func performPingCheck(ip string) bool {
  	// Bypass raw sockets admin requirement by running system ping utility
  	var cmd *exec.Cmd
  	if runtime.GOOS == "windows" {
  		cmd = exec.Command("ping", "-n", "1", "-w", "1000", ip)
  	} else {
  		cmd = exec.Command("ping", "-c", "1", "-W", "1", ip)
  	}
  	err := cmd.Run()
  	return err == nil
  }

  func performPortCheck(ip string, port int) bool {
  	address := fmt.Sprintf("%s:%d", ip, port)
  	conn, err := net.DialTimeout("tcp", address, 2*time.Second)
  	if err != nil {
  		return false
  	}
  	conn.Close()
  	return true
  }
  ```
  Ensure `startLocalSweeper()` is initialized in `main()` of `agent/main.go` when in master mode:
  ```go
  	// Start Collector Server if in Master mode
  	if strings.ToLower(globalConfig.Mode) == "master" {
  		loaded, err := loadDiscoveredAgentsFromFile(discoveredFile)
  		if err == nil {
  			agentsMutex.Lock()
  			discoveredAgents = loaded
  			agentsMutex.Unlock()
  			fmt.Printf("Loaded %d discovered agents from registry.\n", len(loaded))
  		}
  		startOfflineRecoveryWorker()
  		startLocalSweeper()
  		go startCollectorServer(collectorPort)
  	}
  ```
  Also import `"os/exec"` in imports section of `agent/main.go` (line 3-19):
  ```go
  import (
  	"bufio"
  	"bytes"
  	"encoding/json"
  	"flag"
  	"fmt"
  	"io"
  	"math/rand"
  	"net"
  	"net/http"
  	"os"
  	"os/exec"
  	"regexp"
  	"runtime"
  	"strings"
  	"sync"
  	"time"
  )
  ```

- [ ] **Step 4: Run test to verify it passes**
  Run: `powershell -ExecutionPolicy Bypass -Command ". ..\load_env.ps1; go test -v -run TestSweeperOfflineDetection"`
  Expected: PASS

- [ ] **Step 5: Run all unit tests to ensure no regressions**
  Run: `powershell -ExecutionPolicy Bypass -Command ". ..\load_env.ps1; go test -v ./..."`
  Expected: All tests PASS.

- [ ] **Step 6: Commit**
  Run: `git add agent/main.go agent/main_test.go`
  Run: `git commit -m "feat: implement active background sweeper and hybrid ping/port prober"`

---

### Task 5: Central Server Integration (`RulesEngine.cs`)

**Files:**
- Modify: `server/Services/RulesEngine.cs:60-75`
- Modify: `server/Services/RulesEngine.cs:300-320`

**Interfaces:**
- Consumes: `"agent_status"` category events from Agent POST API.
- Produces: Triggers critical alerts mapped to `"Agent offline"` rule.

- [ ] **Step 1: Add `CheckAgentStatusRuleAsync` call to `ProcessEventsAsync`**
  Modify [RulesEngine.cs](file:///C:/Users/MinhHQ/.gemini/antigravity/scratch/onesecurity/server/Services/RulesEngine.cs) around line 60:
  ```csharp
                  // Run rules
                  await CheckBruteForceRuleAsync(ev, config);
                  await CheckAfterHoursLoginRuleAsync(ev);
                  await CheckHighPrivilegeRuleAsync(ev);
                  await CheckServiceDownRuleAsync(ev);
                  await CheckBackupMissedRuleAsync(ev);
                  await CheckUserMgmtRuleAsync(ev);
                  await CheckFirewallRuleAsync(ev);
                  await CheckRootSshRuleAsync(ev);
                  await CheckCrontabRuleAsync(ev);
                  await CheckSqlInjectionRuleAsync(ev);
                  await CheckXssRuleAsync(ev);
                  await CheckPathTraversalRuleAsync(ev);
                  await CheckWebFloodRuleAsync(ev);
                  await CheckAgentStatusRuleAsync(ev);
  ```

- [ ] **Step 2: Add `CheckAgentStatusRuleAsync` rule method**
  Add the method to [RulesEngine.cs](file:///C:/Users/MinhHQ/.gemini/antigravity/scratch/onesecurity/server/Services/RulesEngine.cs) (near other rule implementations):
  ```csharp
          private async Task CheckAgentStatusRuleAsync(SecurityEvent ev)
          {
              if (ev.Category != "agent_status") return;

              string serverName = ev.Server?.Hostname ?? "Server";
              await TriggerAlertAsync(
                  ev,
                  "Agent offline",
                  ev.Severity.ToUpper(), // "CRITICAL" or "INFO"
                  ev.Title,
                  ev.Details
              );
          }
  ```

- [ ] **Step 3: Run project build to verify compile**
  Run: `dotnet build` in `C:\Users\MinhHQ\.gemini\antigravity\scratch\onesecurity\server`
  Expected: Build succeeded with 0 errors.

- [ ] **Step 4: Commit**
  Run: `git add server/Services/RulesEngine.cs`
  Run: `git commit -m "feat: integrate agent_status event category into central RulesEngine"`

---

## 5. Verification & Review Handoff

Once completed:
1. Run all agents and servers using CMD: `run_all.bat`.
2. Wait 30 seconds for clients to register and send their initial heartbeats to Master.
3. Check that the file `discovered_agents.json` is created under the agent directory and contains the clients.
4. Stop the client agent (shut down the CMD console running Go Agent client).
5. Watch the Master Agent console. It should log `Agent offline detected`.
6. Open the Web Dashboard. It should show a critical **Agent offline** alert with the specific message: `"Agent Process Stopped ... Ping ICMP was successful, but TCP Port 8080 connection failed..."`.
7. Turn the client agent back on. Verify the recovery message is posted and the alert is cleared.
