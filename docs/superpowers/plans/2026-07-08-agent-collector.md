# Agent Collector Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a unified Master/Client Go Agent architecture with file-backed offline log buffering.

**Architecture:** Extend `agent/main.go` with CLI flags to support running as a sub-agent (`client`) or log collector (`master`). In `master` mode, the agent exposes collector endpoints, forwards client telemetry, and buffers events to `offline_buffer.json` during central server outtages, auto-flushing them when online.

**Tech Stack:** Go (Standard Library: `flag`, `net/http`, `sync`, `io`, `os`), ASP.NET Core Central Server.

## Global Constraints
- Target workspace directory: `C:\Users\MinhHQ\.gemini\antigravity\scratch\onesecurity`
- Keep existing login regex checks and XSS/Path Traversal logic unchanged.
- Follow Go standard practices (e.g. use explicit `if-else` blocks instead of ternary operators).

---

### Task 1: CLI Flags, Config Extension, and Test Scaffolding

**Files:**
- Create: `agent/main_test.go`
- Modify: `agent/main.go`

**Interfaces:**
- Produces: `parseFlags(args []string)` function in `agent/main.go` to override `globalConfig.Mode` and `globalConfig.CollectorUrl`.

- [ ] **Step 1: Write the failing test**

  Create a new file `agent/main_test.go` with tests verifying flag parsing overrides.
  ```go
  package main

  import (
  	"testing"
  )

  func TestParseFlagsOverrides(t *testing.T) {
  	// Reset globals before test
  	globalConfig.Mode = "client"
  	globalConfig.CollectorUrl = ""

  	// Call flag parsing override helper
  	parseFlags([]string{"-mode", "master", "-collector", "http://localhost:9000"})

  	if globalConfig.Mode != "master" {
  		t.Errorf("Expected Mode to be 'master', got '%s'", globalConfig.Mode)
  	}
  	if globalConfig.CollectorUrl != "http://localhost:9000" {
  		t.Errorf("Expected CollectorUrl to be 'http://localhost:9000', got '%s'", globalConfig.CollectorUrl)
  	}
  }
  ```

- [ ] **Step 2: Run test to verify it fails**

  Run: `..\tools\go\bin\go.exe test -v ./agent`
  Expected: FAIL with "parseFlags not defined"

- [ ] **Step 3: Write minimal implementation**

  Modify `agent/main.go` to add `parseFlags` definition.
  Add `CollectorUrl` field to `Config` struct (around line 40).
  ```go
  type Config struct {
  	ServerUrl                string `json:"serverUrl"`
  	ApiKey                   string `json:"apiKey"`
  	AgentId                  string `json:"agentId"`
  	Mode                     string `json:"mode"`
  	HeartbeatIntervalSeconds int    `json:"heartbeatIntervalSeconds"`
  	HospitalCode             ...
  	CollectorUrl             string `json:"collectorUrl"` // NEW
  }
  ```
  Implement flag override parsing helper:
  ```go
  func parseFlags(args []string) {
  	fs := flag.NewFlagSet("onesecurity-agent", flag.ContinueOnError)
  	modeFlag := fs.String("mode", "", "Agent mode (master or client)")
  	collectorFlag := fs.String("collector", "", "Collector URL for client mode")
  	hospitalFlag := fs.String("hospital", "", "Hospital code")
  	portFlag := fs.Int("port", 8080, "Port for web server")
  	agentIdFlag := fs.String("agentid", "", "Agent ID override")
  	hostnameFlag := fs.String("hostname", "", "Hostname override")

  	err := fs.Parse(args)
  	if err != nil {
  		return
  	}

  	if *modeFlag != "" {
  		globalConfig.Mode = *modeFlag
  	}
  	if *collectorFlag != "" {
  		globalConfig.CollectorUrl = *collectorFlag
  	}
  	if *hospitalFlag != "" {
  		globalConfig.HospitalCode = *hospitalFlag
  	}
  	if *agentIdFlag != "" {
  		globalConfig.AgentId = *agentIdFlag
  	}
  	if *hostnameFlag != "" {
  		globalConfig.Hostname = *hostnameFlag
  	}
  }
  ```
  Call `parseFlags(os.Args[1:])` in `main()` instead of manual flag parsing.

- [ ] **Step 4: Run test to verify it passes**

  Run: `..\tools\go\bin\go.exe test -v ./agent`
  Expected: PASS

- [ ] **Step 5: Commit**

  Run: `git add agent/main.go agent/main_test.go` (if git present, else skip)

---

### Task 2: Implement Master Mode Collector Server

**Files:**
- Modify: `agent/main.go`
- Modify: `agent/main_test.go`

**Interfaces:**
- Consumes: `parseFlags(args []string)`
- Produces: `startCollectorServer(port int)` inside `agent/main.go` exposing HTTP endpoints.

- [ ] **Step 1: Write the failing test**

  Write a test in `agent/main_test.go` to test that the collector HTTP server responds to heartbeat forwards.
  ```go
  func TestCollectorHeartbeatForward(t *testing.T) {
  	// TBD: Mock client heartbeats
  }
  ```

- [ ] **Step 2: Run test to verify it fails**

  Run: `..\tools\go\bin\go.exe test -v ./agent`
  Expected: FAIL

- [ ] **Step 3: Write minimal implementation**

  Inside `agent/main.go`, implement collector routes.
  ```go
  func startCollectorServer(port int) {
  	mux := http.NewServeMux()
  	mux.HandleFunc("/api/collector/heartbeat", handleCollectorHeartbeat)
  	mux.HandleFunc("/api/collector/events", handleCollectorEvents)

  	addr := fmt.Sprintf(":%d", port)
  	fmt.Printf("[MASTER] Starting log collector on port %s\n", addr)
  	go http.ListenAndServe(addr, mux)
  }

  func handleCollectorHeartbeat(w http.ResponseWriter, r *http.Request) {
  	if r.Method != "POST" {
  		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
  		return
  	}
  	// Forward heartbeat to Central Server
  	subAgentId := r.URL.Query().Get("agentId")
  	if subAgentId == "" {
  		subAgentId = "unknown-sub-agent"
  	}
  	url := fmt.Sprintf("%s/api/agents/%s/heartbeat", globalConfig.ServerUrl, subAgentId)
  	
  	bodyBytes, _ := io.ReadAll(r.Body)
  	req, _ := http.NewRequest("POST", url, bytes.NewBuffer(bodyBytes))
  	req.Header.Set("Content-Type", "application/json")
  	req.Header.Set("X-Api-Key", globalConfig.ApiKey)

  	resp, err := client.Do(req)
  	if err != nil {
  		http.Error(w, err.Error(), http.StatusInternalServerError)
  		return
  	}
  	defer resp.Body.Close()
  	w.WriteHeader(resp.StatusCode)
  }

  func handleCollectorEvents(w http.ResponseWriter, r *http.Request) {
  	if r.Method != "POST" {
  		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
  		return
  	}
  	// Forward events to Central Server
  	url := fmt.Sprintf("%s/api/events", globalConfig.ServerUrl)
  	bodyBytes, _ := io.ReadAll(r.Body)
  	
  	// Check if server is online
  	req, _ := http.NewRequest("POST", url, bytes.NewBuffer(bodyBytes))
  	req.Header.Set("Content-Type", "application/json")
  	req.Header.Set("X-Api-Key", globalConfig.ApiKey)

  	resp, err := client.Do(req)
  	if err != nil {
  		// OFFLINE: Save to buffer file
  		appendOfflineBuffer(bodyBytes)
  		w.WriteHeader(http.StatusOK)
  		json.NewEncoder(w).Encode(map[string]interface{}{"buffered": true})
  		return
  	}
  	defer resp.Body.Close()
  	w.WriteHeader(resp.StatusCode)
  }
  ```

- [ ] **Step 4: Run test to verify it passes**

  Run: `..\tools\go\bin\go.exe test -v ./agent`
  Expected: PASS

---

### Task 3: Client Mode Telemetry Redirection

**Files:**
- Modify: `agent/main.go`

**Interfaces:**
- Consumes: `parseFlags` config changes.
- Produces: Dynamic selection of API endpoints (Collector URL if client mode + collector set, else Central Server directly).

- [ ] **Step 1: Write the failing test**

  Write a test in `agent/main_test.go` verifying that `sendEvent` sends to Collector URL when configured.

- [ ] **Step 2: Run test to verify it fails**

  Run: `..\tools\go\bin\go.exe test -v ./agent`
  Expected: FAIL

- [ ] **Step 3: Write minimal implementation**

  Update `sendEvent` (around line 231) and `startHeartbeatLoop` (around line 200) inside `agent/main.go` to dynamically select the target URL:
  ```go
  func getTargetEndpoint(path string) string {
  	if strings.ToLower(globalConfig.Mode) == "client" && globalConfig.CollectorUrl != "" {
  		return fmt.Sprintf("%s%s", globalConfig.CollectorUrl, path)
  	}
  	return fmt.Sprintf("%s%s", globalConfig.ServerUrl, path)
  }
  ```
  Inside `sendEvent`:
  ```go
  url := getTargetEndpoint("/api/events") // Wait, if sending to collector, path is /api/collector/events!
  ```
  Let's refine:
  ```go
  func getEventEndpoint() string {
  	if strings.ToLower(globalConfig.Mode) == "client" && globalConfig.CollectorUrl != "" {
  		return fmt.Sprintf("%s/api/collector/events", globalConfig.CollectorUrl)
  	}
  	return fmt.Sprintf("%s/api/events", globalConfig.ServerUrl)
  }

  func getHeartbeatEndpoint(agentId string) string {
  	if strings.ToLower(globalConfig.Mode) == "client" && globalConfig.CollectorUrl != "" {
  		return fmt.Sprintf("%s/api/collector/heartbeat?agentId=%s", globalConfig.CollectorUrl, agentId)
  	}
  	return fmt.Sprintf("%s/api/agents/%s/heartbeat", globalConfig.ServerUrl, agentId)
  }
  ```

- [ ] **Step 4: Run test to verify it passes**

  Run: `..\tools\go\bin\go.exe test -v ./agent`
  Expected: PASS

---

### Task 4: Implement Offline Queue buffering and worker recovery

**Files:**
- Modify: `agent/main.go`

**Interfaces:**
- Consumes: Collector events handler.
- Produces: `appendOfflineBuffer(events []byte)` and `startOfflineRecoveryWorker()` running in background.

- [ ] **Step 1: Write the failing test**

  Write a test checking that `appendOfflineBuffer` writes events correctly to `offline_buffer.json`.

- [ ] **Step 2: Run test to verify it fails**

  Run: `..\tools\go\bin\go.exe test -v ./agent`
  Expected: FAIL

- [ ] **Step 3: Write minimal implementation**

  Implement buffering and worker in `agent/main.go`:
  ```go
  var bufferMutex sync.Mutex

  func appendOfflineBuffer(eventBytes []byte) {
  	bufferMutex.Lock()
  	defer bufferMutex.Unlock()

  	f, err := os.OpenFile("offline_buffer.json", os.O_CREATE|os.O_WRONLY|os.O_APPEND, 0666)
  	if err != nil {
  		fmt.Println("Error opening offline buffer file:", err)
  		return
  	}
  	defer f.Close()

  	var reports []EventReport
  	json.Unmarshal(eventBytes, &reports)

  	for _, r := range reports {
  		line, _ := json.Marshal(r)
  		f.Write(append(line, '\n'))
  	}
  	fmt.Printf("[WARNING] Central Server offline. Event buffered in offline queue (File: offline_buffer.json)\n")
  }

  func startOfflineRecoveryWorker() {
  	ticker := time.NewTicker(5 * time.Second)
  	go func() {
  		for range ticker.C {
  			if _, err := os.Stat("offline_buffer.json"); os.IsNotExist(err) {
  				continue
  			}

  			// Check if Central Server is online
  			url := fmt.Sprintf("%s/api/events", globalConfig.ServerUrl)
  			resp, err := client.Get(fmt.Sprintf("%s/api/overview", globalConfig.ServerUrl)) // Ping endpoint
  			if err != nil {
  				continue
  			}
  			resp.Body.Close()

  			// Online: flush buffer
  			flushOfflineBuffer(url)
  		}
  	}()
  }

  func flushOfflineBuffer(url string) {
  	bufferMutex.Lock()
  	defer bufferMutex.Unlock()

  	f, err := os.Open("offline_buffer.json")
  	if err != nil {
  		return
  	}
  	defer f.Close()

  	var events []EventReport
  	scanner := bufio.NewScanner(f)
  	for scanner.Scan() {
  		var ev EventReport
  		if err := json.Unmarshal(scanner.Bytes(), &ev); err == nil {
  			events = append(events, ev)
  		}
  	}

  	if len(events) == 0 {
  		os.Remove("offline_buffer.json")
  		return
  	}

  	jsonBytes, _ := json.Marshal(events)
  	req, _ := http.NewRequest("POST", url, bytes.NewBuffer(jsonBytes))
  	req.Header.Set("Content-Type", "application/json")
  	req.Header.Set("X-Api-Key", globalConfig.ApiKey)

  	resp, err := client.Do(req)
  	if err == nil && resp.StatusCode == http.StatusOK {
  		resp.Body.Close()
  		f.Close() // Close before removal
  		os.Remove("offline_buffer.json")
  		fmt.Printf("[INFO] Connection to Central Server restored. Flushed %d buffered events successfully.\n", len(events))
  	}
  }
  ```
  Start the background worker in `main()` if mode is `master`:
  ```go
  if strings.ToLower(globalConfig.Mode) == "master" {
  	startCollectorServer(*portFlag)
  	startOfflineRecoveryWorker()
  }
  ```

- [ ] **Step 4: Run test to verify it passes**

  Run: `..\tools\go\bin\go.exe test -v ./agent`
  Expected: PASS
