package main

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
	"regexp"
	"runtime"
	"strings"
	"sync"
	"time"
)

type Config struct {
	ServerUrl                string `json:"ServerUrl"`
	ApiKey                   string `json:"ApiKey"`
	AgentId                  string `json:"AgentId"`
	Hostname                 string `json:"Hostname"`
	Mode                     string `json:"Mode"`
	CollectorUrl             string `json:"CollectorUrl"`
	HeartbeatIntervalSeconds int    `json:"HeartbeatIntervalSeconds"`
	HospitalCode             string `json:"HospitalCode"`
	CollectorId              int64  `json:"CollectorId"`
	EnrollmentToken          string `json:"EnrollmentToken"`
	AgentVersion             string `json:"AgentVersion"`
	Capabilities             string `json:"Capabilities"`
}

type RegisterRequest struct {
	AgentId          string `json:"agentId"`
	Hostname         string `json:"hostname"`
	OsType           string `json:"osType"`
	OsVersion        string `json:"osVersion"`
	OsInfo           string `json:"osInfo"`
	IpAddress        string `json:"ipAddress"`
	HospitalCode     string `json:"hospitalCode"`
	SupportedActions string `json:"supportedActions"`
	Capabilities     string `json:"capabilities"`
	AgentVersion     string `json:"agentVersion"`
	CollectorVersion string `json:"collectorVersion"`
	EnrollmentToken  string `json:"enrollmentToken"`
	CollectorId      int64  `json:"collectorId"`
}

type HeartbeatRequest struct {
	AgentId     string  `json:"agentId"`
	CpuPercent  float64 `json:"cpuPercent"`
	RamPercent  float64 `json:"ramPercent"`
	DiskPercent float64 `json:"diskPercent"`
	Uptime      float64 `json:"uptime"`
}

type RegisterResponse struct {
	AgentId                 string `json:"agentId"`
	Hostname                string `json:"hostname"`
	Status                  string `json:"status"`
	HeartbeatIntervalSeconds int   `json:"heartbeatIntervalSeconds"`
}

type EventReport struct {
	EventId   string    `json:"eventId"`
	AgentId   string    `json:"agentId"`
	Timestamp time.Time `json:"timestamp"`
	Category  string    `json:"category"`
	Severity  string    `json:"severity"`
	Source    string    `json:"source"`
	Title     string    `json:"title"`
	Details   string    `json:"details"`
	RawData   string    `json:"rawData"`
}

var client = &http.Client{Timeout: 10 * time.Second}
var globalConfig Config
var startTime = time.Now()
var currentHospitalCode = "HOSP_A"
var currentServerCode = "agent-win-his-01"

func main() {
	fmt.Println("Starting OneSecurity Go Agent...")

	// Parse --config flag first (before other flags)
	configFile := "config.json"
	for i, arg := range os.Args[1:] {
		if arg == "--config" || arg == "-config" {
			if i+1 < len(os.Args)-1 {
				configFile = os.Args[i+2]
			}
			break
		}
		if len(arg) > 9 && arg[:9] == "--config=" {
			configFile = arg[9:]
			break
		}
	}

	loadConfig(configFile)

	// Command line flags override config file values
	portFlag := parseFlags(os.Args[1:])

	// Fill hostname if empty
	if globalConfig.Hostname == "" {
		hostname, err := os.Hostname()
		if err == nil {
			globalConfig.Hostname = hostname
		} else {
			globalConfig.Hostname = "Unknown-Host"
		}
	}

	ipAddress := getLocalIP()
	fmt.Printf("Agent Info:\n - Hostname: %s\n - OS: %s (%s)\n - IP Address: %s\n - Agent ID: %s\n - Hospital Code: %s\n\n",
		globalConfig.Hostname, runtime.GOOS, runtime.GOARCH, ipAddress, globalConfig.AgentId, globalConfig.HospitalCode)

	// 1. Register Agent
	registerAgent(ipAddress)

	// Start Command Polling Loop
	go pollCommandsLoop()

	// 2. Start Heartbeat Loop
	go startHeartbeatLoop()

	// 3. Start Mock Hospital Information System (HIS) web server on the specified port
	go startHisWebServer(ipAddress, portFlag)

	// Start Collector Server if in Master mode
	if strings.ToLower(globalConfig.Mode) == "master" {
		collectorPort := 9000
		if globalConfig.CollectorUrl != "" {
			parts := strings.Split(globalConfig.CollectorUrl, ":")
			if len(parts) > 0 {
				portStr := strings.Split(parts[len(parts)-1], "/")[0]
				var p int
				if _, err := fmt.Sscanf(portStr, "%d", &p); err == nil {
					collectorPort = p
				}
			}
		}
		startOfflineRecoveryWorker()
		go startCollectorServer(collectorPort)
	}

	// 4. Start Monitor Mode
	if strings.ToLower(globalConfig.Mode) == "simulator" {
		runSimulator()
	} else {
		runRealMonitor()
	}
}

func loadConfig(configFile string) {
	if configFile == "" {
		configFile = "config.json"
	}
	fmt.Printf("Loading config from: %s\n", configFile)
	file, err := os.Open(configFile)
	if err != nil {
		fmt.Printf("Warning: %s not found, using default configurations.\n", configFile)
		globalConfig = Config{
			ServerUrl:                "http://localhost:5000",
			ApiKey:                   "onesecurity_secret_key_2026",
			AgentId:                  "agent-win-his-01",
			Mode:                     "simulator",
			CollectorUrl:             "",
			HeartbeatIntervalSeconds: 10,
			HospitalCode:             "HOSP_A",
		}
		return
	}
	defer file.Close()

	decoder := json.NewDecoder(file)
	err = decoder.Decode(&globalConfig)
	if err != nil {
		fmt.Println("Error reading config.json, using defaults:", err)
		globalConfig = Config{
			ServerUrl:                "http://localhost:5000",
			ApiKey:                   "onesecurity_secret_key_2026",
			AgentId:                  "agent-win-his-01",
			Mode:                     "simulator",
			CollectorUrl:             "",
			HeartbeatIntervalSeconds: 10,
			HospitalCode:             "HOSP_A",
		}
	}

	if globalConfig.AgentId == "" {
		globalConfig.AgentId = fmt.Sprintf("agent-%d", time.Now().Unix())
	}
}

func parseFlags(args []string) int {
	fs := flag.NewFlagSet("agent", flag.ContinueOnError)
	fs.SetOutput(io.Discard)

	hospitalFlag := fs.String("hospital", "", "Hospital code (e.g. HOSP_A or HOSP_B)")
	portFlag := fs.Int("port", 8080, "Port for Mock Hospital HIS Portal web server (e.g. 8080 or 8081)")
	agentIdFlag := fs.String("agentid", "", "Unique Agent ID")
	hostnameFlag := fs.String("hostname", "", "Agent Hostname override")
	modeFlag := fs.String("mode", "", "Operating mode (master or client)")
	collectorFlag := fs.String("collector", "", "Collector URL")

	_ = fs.Parse(args)

	if *hospitalFlag != "" {
		globalConfig.HospitalCode = *hospitalFlag
	}
	if *agentIdFlag != "" {
		globalConfig.AgentId = *agentIdFlag
	}
	if *hostnameFlag != "" {
		globalConfig.Hostname = *hostnameFlag
	}
	if *modeFlag != "" {
		globalConfig.Mode = *modeFlag
	}
	if *collectorFlag != "" {
		globalConfig.CollectorUrl = *collectorFlag
	}

	return *portFlag
}

func getLocalIP() string {
	// Cách 1: Thử kết nối ra ngoài để biết IP nào đang được dùng
	conn, err := net.Dial("udp", "8.8.8.8:80")
	if err == nil {
		defer conn.Close()
		localAddr := conn.LocalAddr().(*net.UDPAddr)
		ip := localAddr.IP.String()
		// Bỏ qua APIPA (169.254.x.x) và loopback
		if !strings.HasPrefix(ip, "169.254.") && ip != "127.0.0.1" {
			return ip
		}
	}

	// Cách 2: Duyệt interface, ưu tiên IP thật (bỏ APIPA và loopback)
	addrs, err := net.InterfaceAddrs()
	if err != nil {
		return "127.0.0.1"
	}
	var apipaIP string
	for _, address := range addrs {
		if ipnet, ok := address.(*net.IPNet); ok && !ipnet.IP.IsLoopback() {
			if ipnet.IP.To4() != nil {
				ip := ipnet.IP.String()
				if strings.HasPrefix(ip, "169.254.") {
					// Lưu APIPA làm dự phòng
					if apipaIP == "" {
						apipaIP = ip
					}
					continue // Bỏ qua, tìm IP thật hơn
				}
				return ip // IP thật (192.168.x.x, 10.x.x.x, v.v.)
			}
		}
	}

	// Nếu chỉ có APIPA thì báo luôn (laptop chưa vào được LAN)
	if apipaIP != "" {
		fmt.Println("[WARNING] Only APIPA address found (169.254.x.x). Make sure laptop is connected to the same network as the server!")
		return apipaIP
	}
	return "127.0.0.1"
}


func registerAgent(ipAddress string) {
	url := fmt.Sprintf("%s/api/v1/collector/register", globalConfig.ServerUrl)
	reqBody := RegisterRequest{
		AgentId:          globalConfig.AgentId,
		Hostname:         globalConfig.Hostname,
		OsType:           runtime.GOOS,
		OsVersion:        fmt.Sprintf("%s %s", runtime.GOOS, runtime.GOARCH),
		OsInfo:           fmt.Sprintf("%s %s", runtime.GOOS, runtime.GOARCH),
		IpAddress:        ipAddress,
		HospitalCode:     globalConfig.HospitalCode,
		SupportedActions: "Restart,CollectDiagnostics,RunScan,SyncConfiguration",
		Capabilities:     globalConfig.Capabilities,
		AgentVersion:     globalConfig.AgentVersion,
		CollectorVersion: "1.2.0",
		EnrollmentToken:  globalConfig.EnrollmentToken,
		CollectorId:      globalConfig.CollectorId,
	}

	jsonBytes, _ := json.Marshal(reqBody)
	
	for {
		req, err := http.NewRequest("POST", url, bytes.NewBuffer(jsonBytes))
		if err != nil {
			fmt.Println("Error creating registration request:", err)
			time.Sleep(5 * time.Second)
			continue
		}

		req.Header.Set("Content-Type", "application/json")
		req.Header.Set("X-Api-Key", globalConfig.ApiKey)

		resp, err := client.Do(req)
		if err != nil {
			fmt.Printf("Connection to central server failed: %v. Retrying in 5 seconds...\n", err)
			time.Sleep(5 * time.Second)
			continue
		}

		bodyBytes, _ := io.ReadAll(resp.Body)
		resp.Body.Close()

		if resp.StatusCode == http.StatusOK {
			var regResp RegisterResponse
			if err := json.Unmarshal(bodyBytes, &regResp); err == nil && regResp.AgentId != "" {
				// ⭐ Lưu UUID từ server — dùng cho heartbeat về sau
				globalConfig.AgentId = regResp.AgentId
				if regResp.HeartbeatIntervalSeconds > 0 {
					globalConfig.HeartbeatIntervalSeconds = regResp.HeartbeatIntervalSeconds
				}
				fmt.Printf("Registered successfully. Server AgentId: %s\n", globalConfig.AgentId)
			} else {
				fmt.Println("Registered (could not parse AgentId from response):", string(bodyBytes))
			}
			break
		} else if resp.StatusCode == http.StatusConflict {
			fmt.Println("Agent is already registered on Central Server (HTTP 409). Continuing...")
			break
		} else {
			fmt.Printf("Registration failed (HTTP %d): %s. Retrying in 5 seconds...\n", resp.StatusCode, string(bodyBytes))
			time.Sleep(5 * time.Second)
		}
	}
}

func startHeartbeatLoop() {
	interval := time.Duration(globalConfig.HeartbeatIntervalSeconds) * time.Second
	if interval <= 0 {
		interval = 10 * time.Second
	}

	url := getHeartbeatEndpoint(globalConfig.AgentId)
	ticker := time.NewTicker(interval)
	
	fmt.Printf("Heartbeat loop started. Reporting every %v.\n", interval)

	for range ticker.C {
		// Mock CPU/RAM/Disk metrics
		reqBody := HeartbeatRequest{
			AgentId:     globalConfig.AgentId,
			CpuPercent:  10.0 + rand.Float64()*30.0, // 10% - 40%
			RamPercent:  50.0 + rand.Float64()*15.0, // 50% - 65%
			DiskPercent: 72.4,
			Uptime:      time.Since(startTime).Seconds(),
		}

		jsonBytes, _ := json.Marshal(reqBody)
		
		req, err := http.NewRequest("POST", url, bytes.NewBuffer(jsonBytes))
		if err != nil {
			fmt.Println("Heartbeat error:", err)
			continue
		}

		req.Header.Set("Content-Type", "application/json")
		req.Header.Set("X-Api-Key", globalConfig.ApiKey)

		resp, err := client.Do(req)
		if err != nil {
			fmt.Println("Failed to send heartbeat:", err)
		} else {
			fmt.Printf("[%s] Heartbeat sent successfully! Status: %s\n", time.Now().Format("15:04:05"), resp.Status)
			resp.Body.Close()
		}

		// Gửi kèm Metrics để cập nhật đồ thị Dashboard
		metricsUrl := globalConfig.ServerUrl + "/api/v1/collector/metrics"
		metricsPayload := map[string]interface{}{
			"agentId":         globalConfig.AgentId,
			"cpuUsagePercent": reqBody.CpuPercent,
			"ramUsagePercent": reqBody.RamPercent,
			"diskUsagePercent": reqBody.DiskPercent,
			"networkInBytes":   2048576,
			"networkOutBytes":  1024576,
		}
		metricsBytes, _ := json.Marshal(metricsPayload)

		metricsReq, err := http.NewRequest("POST", metricsUrl, bytes.NewBuffer(metricsBytes))
		if err == nil {
			metricsReq.Header.Set("Content-Type", "application/json")
			metricsReq.Header.Set("X-Api-Key", globalConfig.ApiKey)
			
			metricsResp, err := client.Do(metricsReq)
			if err == nil {
				fmt.Printf("[%s] Metrics sent successfully! Status: %s\n", time.Now().Format("15:04:05"), metricsResp.Status)
				metricsResp.Body.Close()
			} else {
				fmt.Println("Failed to send metrics:", err)
			}
		}
	}
}

func sendEvent(ev EventReport) {
	url := getEventEndpoint()
	jsonBytes, _ := json.Marshal(ev)

	req, err := http.NewRequest("POST", url, bytes.NewBuffer(jsonBytes))
	if err != nil {
		fmt.Println("Error creating event request:", err)
		return
	}

	req.Header.Set("Content-Type", "application/json")
	req.Header.Set("X-Api-Key", globalConfig.ApiKey)

	resp, err := client.Do(req)
	if err != nil {
		fmt.Println("Failed to send event:", err)
		return
	}
	defer resp.Body.Close()

	body, _ := io.ReadAll(resp.Body)
	if resp.StatusCode == http.StatusOK {
		fmt.Printf("Event sent successfully! Response: %s\n", string(body))
	} else {
		fmt.Printf("Failed to send event (HTTP %d): %s\n", resp.StatusCode, string(body))
	}
}

func generateUUID() string {
	now := time.Now().UnixNano()
	return fmt.Sprintf("evt-%d-%d", now, rand.Intn(100000))
}

type GoAgentCommand struct {
	CommandId  string `json:"commandId"`
	AgentId    string `json:"agentId"`
	ActionType string `json:"actionType"`
	Metadata   string `json:"metadata"`
}

type CommandReportResult struct {
	CommandId string `json:"commandId"`
	Status    string `json:"status"`
	Message   string `json:"message"`
}

func pollCommandsLoop() {
	ticker := time.NewTicker(5 * time.Second)
	defer ticker.Stop()

	pollUrl := fmt.Sprintf("%s/api/v1/collector/commands?agentId=%s", globalConfig.ServerUrl, globalConfig.AgentId)

	for range ticker.C {
		req, err := http.NewRequest("GET", pollUrl, nil)
		if err != nil {
			continue
		}
		req.Header.Set("X-Api-Key", globalConfig.ApiKey)

		resp, err := client.Do(req)
		if err != nil {
			continue
		}

		if resp.StatusCode == http.StatusNoContent {
			resp.Body.Close()
			continue
		}

		if resp.StatusCode != http.StatusOK {
			resp.Body.Close()
			continue
		}

		var cmd GoAgentCommand
		if err := json.NewDecoder(resp.Body).Decode(&cmd); err != nil {
			resp.Body.Close()
			continue
		}
		resp.Body.Close()

		fmt.Printf("[Response Action] Received command: ID=%s, Action=%s\n", cmd.CommandId, cmd.ActionType)
		go executeAgentCommand(cmd)
	}
}

func reportCommandResult(commandId string, status string, message string) {
	url := fmt.Sprintf("%s/api/v1/collector/commands/result", globalConfig.ServerUrl)
	payload := CommandReportResult{
		CommandId: commandId,
		Status:    status,
		Message:   message,
	}
	
	bytesPayload, _ := json.Marshal(payload)
	
	req, err := http.NewRequest("POST", url, bytes.NewBuffer(bytesPayload))
	if err != nil {
		fmt.Printf("[Response Action] Error creating status report request: %v\n", err)
		return
	}
	
	req.Header.Set("Content-Type", "application/json")
	req.Header.Set("X-Api-Key", globalConfig.ApiKey)
	
	resp, err := client.Do(req)
	if err != nil {
		fmt.Printf("[Response Action] Error sending status report: %v\n", err)
		return
	}
	resp.Body.Close()
}

func executeAgentCommand(cmd GoAgentCommand) {
	// 1. Report executing status
	reportCommandResult(cmd.CommandId, "Executing", "Agent is starting execution of " + cmd.ActionType)

	// Simulate work
	time.Sleep(2 * time.Second)

	status := "Succeeded"
	msg := ""

	switch strings.ToLower(cmd.ActionType) {
	case "restart":
		msg = "Agent restart initiated. Process restarting..."
		fmt.Println("[Response Action] Agent restart simulation started...")
	case "collectdiagnostics":
		diagnostics := map[string]interface{}{
			"timestamp": time.Now().Format(time.RFC3339),
			"os": runtime.GOOS,
			"arch": runtime.GOARCH,
			"numCpu": runtime.NumCPU(),
			"numGoroutine": runtime.NumGoroutine(),
			"mockProcesses": []string{
				"onesecurity-agent.exe",
				"explorer.exe",
				"chrome.exe",
				"sqlservr.exe",
				"w3wp.exe",
			},
			"diskUsagePercent": 42.5,
			"ramUsagePercent": 61.2,
		}
		diagBytes, _ := json.Marshal(diagnostics)
		msg = string(diagBytes)
		fmt.Println("[Response Action] Collected diagnostic data successfully.")
	case "runscan":
		msg = "Quick vulnerability and security scan completed. No active threat detected."
		fmt.Println("[Response Action] Run scan execution completed.")
	case "syncconfiguration":
		msg = "Agent local policies and logging configurations synchronized with server."
		fmt.Println("[Response Action] Configuration synchronization completed.")
	default:
		status = "Failed"
		msg = fmt.Sprintf("Action type '%s' is not implemented on this agent.", cmd.ActionType)
		fmt.Printf("[Response Action] Unsupported command action: %s\n", cmd.ActionType)
	}

	// 2. Report final status
	reportCommandResult(cmd.CommandId, status, msg)
}

// ----------------------------------------------------------------------------
// Mock Hospital Web Application (Cyber Range) Implementation
// ----------------------------------------------------------------------------

func startHisWebServer(localIp string, port int) {
	mux := http.NewServeMux()
	mux.HandleFunc("/", handleHisRoot)
	mux.HandleFunc("/api/his/login", handleHisLogin)
	mux.HandleFunc("/api/his/service/stop", handleHisServiceStop)
	mux.HandleFunc("/api/his/service/start", handleHisServiceStart)
	mux.HandleFunc("/api/his/backup/fail", handleHisBackupFail)

	addr := fmt.Sprintf(":%d", port)
	fmt.Printf("Starting Mock Hospital HIS Portal web server on: http://localhost%s/ (Network: http://%s%s/)\n", addr, localIp, addr)

	err := http.ListenAndServe(addr, mux)
	if err != nil {
		fmt.Printf("Failed to start Mock Hospital Web Server on port %d: %v\n", port, err)
	}
}

func handleHisRoot(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "text/html; charset=utf-8")
	w.Write([]byte(hisHtmlContent))
}

type HisLoginRequest struct {
	Username     string `json:"username"`
	Password     string `json:"password"`
	HospitalCode string `json:"hospitalCode"`
	ServerCode   string `json:"serverCode"`
}

var requestTracker = make(map[string][]time.Time)
var trackerMutex sync.Mutex

func isXss(input string) bool {
	xssRegex := `(?i)(<script.*?>|javascript:|onload\s*=|onerror\s*=|<img\s+src|<iframe|<body\s+onload)`
	matched, _ := regexp.MatchString(xssRegex, input)
	return matched
}

func isPathTraversal(input string) bool {
	traversalRegex := `(?i)(\.\./|\.\.\\|/etc/passwd|/etc/shadow|/win\.ini|/boot\.ini)`
	matched, _ := regexp.MatchString(traversalRegex, input)
	return matched
}

func handleHisLogin(w http.ResponseWriter, r *http.Request) {
	if r.Method != "POST" {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	var req HisLoginRequest
	err := json.NewDecoder(r.Body).Decode(&req)
	if err != nil {
		http.Error(w, "Bad request", http.StatusBadRequest)
		return
	}

	if req.HospitalCode != "" {
		currentHospitalCode = req.HospitalCode
	}
	if req.ServerCode != "" {
		currentServerCode = req.ServerCode
	}

	w.Header().Set("Content-Type", "application/json")
	clientIp, _, _ := net.SplitHostPort(r.RemoteAddr)
	if clientIp == "" || clientIp == "::1" {
		clientIp = "127.0.0.1"
	}

	// 0. Rate limit check (Web Flood DoS)
	trackerMutex.Lock()
	now := time.Now()
	var activeRequests []time.Time
	for _, t := range requestTracker[clientIp] {
		if now.Sub(t) < 5*time.Second {
			activeRequests = append(activeRequests, t)
		}
	}
	activeRequests = append(activeRequests, now)
	requestTracker[clientIp] = activeRequests
	reqCount := len(activeRequests)
	trackerMutex.Unlock()

	if reqCount > 10 {
		fmt.Printf("[ALERT] Web flood detected from IP %s on Server %s. Request Count: %d in 5s\n", clientIp, currentServerCode, reqCount)
		ev := EventReport{
			EventId:   generateUUID(),
			AgentId:   globalConfig.AgentId,
			Timestamp: now.UTC(),
			Category:  "web_flood",
			Severity:  "warning",
			Source:    "web_his_portal",
			Title:     "Web Request Flood (DoS)",
			Details:   fmt.Sprintf("High request rate detected from client IP %s on Server %s. Total requests in 5 seconds: %d.", clientIp, currentServerCode, reqCount),
			RawData:   fmt.Sprintf(`{"ClientIP":"%s","RequestCount":%d,"TimeWindowSeconds":5}`, clientIp, reqCount),
		}
		sendEvent(ev)

		json.NewEncoder(w).Encode(map[string]interface{}{
			"success": false,
			"message": "Too many requests. Access temporarily throttled by OneSecurity Agent.",
			"hacked":  true,
		})
		return
	}

	// 1. Detect SQL Injection payloads
	sqlInjectionRegex := `(?i)(union\s+select|select\s+.*\s+from|'--|--|#|/\*|\*/|\b(or|and)\b\s+['"\s]*\d+['"\s]*\s*=\s*['"\s]*\d+|\b(or|and)\b\s+['"\s]*[a-zA-Z]+['"\s]*\s*=\s*['"\s]*[a-zA-Z]+|'\s*or\s*.*=|\"\s*or\s*.*=)`
	matchedUser, _ := regexp.MatchString(sqlInjectionRegex, req.Username)
	matchedPass, _ := regexp.MatchString(sqlInjectionRegex, req.Password)

	if matchedUser || matchedPass {
		fmt.Printf("[ALERT] SQL Injection detected from IP %s on Server %s. User Input: %s\n", clientIp, currentServerCode, req.Username)

		ev := EventReport{
			EventId:   generateUUID(),
			AgentId:   globalConfig.AgentId,
			Timestamp: time.Now().UTC(),
			Category:  "sql_injection",
			Severity:  "critical",
			Source:    "web_his_portal",
			Title:     "SQL Injection Intrusion Attempt",
			Details:   fmt.Sprintf("Intrusion threat signature detected in Doctor Login Portal. Input: Username='%s', Password='%s' from client IP %s on Server %s.", req.Username, req.Password, clientIp, currentServerCode),
			RawData:   fmt.Sprintf(`{"ClientIP":"%s","Target":"DoctorLogin","UsernamePayload":"%s"}`, clientIp, req.Username),
		}
		sendEvent(ev)

		json.NewEncoder(w).Encode(map[string]interface{}{
			"success": false,
			"message": "Intrusion Threat Signature Detected! Your attempt has been logged by OneSecurity Agent.",
			"hacked":  true,
		})
		return
	}

	// 2. Detect XSS payloads
	if isXss(req.Username) || isXss(req.Password) {
		fmt.Printf("[ALERT] XSS payload detected from IP %s on Server %s. User Input: %s\n", clientIp, currentServerCode, req.Username)

		ev := EventReport{
			EventId:   generateUUID(),
			AgentId:   globalConfig.AgentId,
			Timestamp: time.Now().UTC(),
			Category:  "xss",
			Severity:  "critical",
			Source:    "web_his_portal",
			Title:     "XSS Intrusion Attempt",
			Details:   fmt.Sprintf("Intrusion threat signature (XSS) detected in Doctor Login Portal. Input: Username='%s' from client IP %s on Server %s.", req.Username, clientIp, currentServerCode),
			RawData:   fmt.Sprintf(`{"ClientIP":"%s","Target":"DoctorLogin","UsernamePayload":"%s"}`, clientIp, req.Username),
		}
		sendEvent(ev)

		json.NewEncoder(w).Encode(map[string]interface{}{
			"success": false,
			"message": "XSS Threat Signature Detected! Your attempt has been logged by OneSecurity Agent.",
			"hacked":  true,
		})
		return
	}

	// 3. Detect Path Traversal payloads
	if isPathTraversal(req.Username) || isPathTraversal(req.Password) {
		fmt.Printf("[ALERT] Path Traversal payload detected from IP %s on Server %s. User Input: %s\n", clientIp, currentServerCode, req.Username)

		ev := EventReport{
			EventId:   generateUUID(),
			AgentId:   globalConfig.AgentId,
			Timestamp: time.Now().UTC(),
			Category:  "path_traversal",
			Severity:  "critical",
			Source:    "web_his_portal",
			Title:     "Path Traversal Intrusion Attempt",
			Details:   fmt.Sprintf("Intrusion threat signature (Path Traversal) detected in Doctor Login Portal. Input: Username='%s' from client IP %s on Server %s.", req.Username, clientIp, currentServerCode),
			RawData:   fmt.Sprintf(`{"ClientIP":"%s","Target":"DoctorLogin","UsernamePayload":"%s"}`, clientIp, req.Username),
		}
		sendEvent(ev)

		json.NewEncoder(w).Encode(map[string]interface{}{
			"success": false,
			"message": "Directory Traversal Threat Signature Detected! Your attempt has been logged by OneSecurity Agent.",
			"hacked":  true,
		})
		return
	}

	// 2. Validate Credentials
	if req.Username == "doctor" && req.Password == "password123" {
		json.NewEncoder(w).Encode(map[string]interface{}{
			"success": true,
			"message": "Logged in successfully.",
		})
	} else {
		// Log failed login event
		fmt.Printf("[LOG] Failed login attempt from IP %s on Server %s. Username: %s\n", clientIp, currentServerCode, req.Username)
		
		ev := EventReport{
			EventId:   generateUUID(),
			AgentId:   globalConfig.AgentId,
			Timestamp: time.Now().UTC(),
			Category:  "login",
			Severity:  "warning",
			Source:    "web_his_portal",
			Title:     "Failed Logon - MedConnect Portal",
			Details:   fmt.Sprintf("MedConnect portal logon failed. Account Name: %s, Source Address: %s on Server %s", req.Username, clientIp, currentServerCode),
			RawData:   fmt.Sprintf(`{"EventID":4625,"TargetUserName":"%s","IpAddress":"%s"}`, req.Username, clientIp),
		}
		sendEvent(ev)

		json.NewEncoder(w).Encode(map[string]interface{}{
			"success": false,
			"message": "Invalid doctor credentials. Failed attempt forwarded.",
		})
	}
}

func handleHisServiceStop(w http.ResponseWriter, r *http.Request) {
	var title = "MSSQLSERVER"
	var details = "The SQL Server (MSSQLSERVER) service entered the stopped state."
	var rawData = `{"EventID":7036,"ServiceName":"MSSQLSERVER","State":"Stopped"}`

	if strings.Contains(currentServerCode, "-lnx-") {
		title = "postgresql.service"
		details = "systemd[1]: Stopped PostgreSQL RDBMS Database Server."
		rawData = `{"EventID":0,"ServiceName":"postgresql","State":"Stopped"}`
	}

	ev := EventReport{
		EventId:   generateUUID(),
		AgentId:   globalConfig.AgentId,
		Timestamp: time.Now().UTC(),
		Category:  "service",
		Severity:  "critical",
		Source:    "service",
		Title:     title,
		Details:   details,
		RawData:   rawData,
	}
	sendEvent(ev)

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]interface{}{"success": true, "status": "stopped"})
}

func handleHisServiceStart(w http.ResponseWriter, r *http.Request) {
	var title = "MSSQLSERVER"
	var details = "The SQL Server (MSSQLSERVER) service entered the running state."
	var rawData = `{"EventID":7036,"ServiceName":"MSSQLSERVER","State":"Running"}`

	if strings.Contains(currentServerCode, "-lnx-") {
		title = "postgresql.service"
		details = "systemd[1]: Started PostgreSQL RDBMS Database Server."
		rawData = `{"EventID":0,"ServiceName":"postgresql","State":"Running"}`
	}

	ev := EventReport{
		EventId:   generateUUID(),
		AgentId:   globalConfig.AgentId,
		Timestamp: time.Now().UTC(),
		Category:  "service",
		Severity:  "info",
		Source:    "service",
		Title:     title,
		Details:   details,
		RawData:   rawData,
	}
	sendEvent(ev)

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]interface{}{"success": true, "status": "running"})
}

func handleHisBackupFail(w http.ResponseWriter, r *http.Request) {
	var title = "Backup job failed - Job_DailyDBBackup"
	var details = "SQL Server Agent Job 'Job_DailyDBBackup' failed. Error: Write failure on tape or disk backup file. Status: Failed."
	var rawData = `{"JobName":"Job_DailyDBBackup","Status":"Failed","ErrorCode":3041}`

	var source = "sqlserver"
	if strings.Contains(currentServerCode, "-lnx-") {
		title = "Backup script failed: backup_db.sh"
		details = "Cron job backup_db.sh failed with exit code 1. Output: tar: /var/lib/postgresql: Cannot open: Permission denied."
		rawData = `{"JobName":"backup_db.sh","Status":"Failed","ErrorCode":1}`
		source = "cron"
	}

	ev := EventReport{
		EventId:   generateUUID(),
		AgentId:   globalConfig.AgentId,
		Timestamp: time.Now().UTC(),
		Category:  "backup",
		Severity:  "critical",
		Source:    source,
		Title:     title,
		Details:   details,
		RawData:   rawData,
	}
	sendEvent(ev)

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]interface{}{"success": true})
}

// ----------------------------------------------------------------------------
// Legacy console CLI simulator
// ----------------------------------------------------------------------------

func runSimulator() {
	reader := bufio.NewReader(os.Stdin)
	fmt.Println("\n==================================================")
	fmt.Println("       OneSecurity Go Agent Simulator Mode        ")
	fmt.Println("==================================================")
	fmt.Println("Press a number key + Enter to trigger a security event:")
	fmt.Println(" 1. Brute-force Attack (5 failed logins in 5 sec)")
	fmt.Println(" 2. After-hours Login (Successful login at 23:45)")
	fmt.Println(" 3. High-privilege Usage (SA Database Login)")
	fmt.Println(" 4. Service Stopped (SQL Server service stopped)")
	fmt.Println(" 5. Backup Failed (SQL Server backup job error)")
	fmt.Println(" 6. User Management (New administrator user added)")
	fmt.Println(" 7. Firewall Modification (Rules updated on host)")
	fmt.Println(" 8. Root SSH Login (Direct root SSH connection)")
	fmt.Println(" 9. Crontab scheduled job modified")
	fmt.Println(" 0. Clean exit")
	fmt.Println("==================================================")

	for {
		fmt.Print("\nEnter choice [0-9]: ")
		input, err := reader.ReadString('\n')
		if err != nil {
			break
		}

		choice := strings.TrimSpace(input)
		if choice == "0" {
			fmt.Println("Exiting agent simulator...")
			break
		}

		triggerSimulatedEvent(choice)
	}
}

func triggerSimulatedEvent(choice string) {
	switch choice {
	case "1":
		fmt.Println("Simulating Brute-force attack. Sending 5 failed login attempts...")
		for i := 1; i <= 5; i++ {
			ev := EventReport{
				EventId:   generateUUID(),
				AgentId:   globalConfig.AgentId,
				Timestamp: time.Now().UTC(),
				Category:  "login",
				Severity:  "warning",
				Source:    "eventlog",
				Title:     "Logon Failure - Event 4625",
				Details:   fmt.Sprintf("An account failed to log on. Account Name: Admin, Source Address: 192.168.1.105, Attempt %d", i),
				RawData:   `{"EventID":4625,"TargetUserName":"Admin","IpAddress":"192.168.1.105"}`,
			}
			sendEvent(ev)
			time.Sleep(300 * time.Millisecond)
		}

	case "2":
		fmt.Println("Simulating After-hours login...")
		now := time.Now().UTC()
		simulatedTime := time.Date(now.Year(), now.Month(), now.Day(), 23, 30, 0, 0, time.UTC)
		ev := EventReport{
			EventId:   generateUUID(),
			AgentId:   globalConfig.AgentId,
			Timestamp: simulatedTime,
			Category:  "login",
			Severity:  "info",
			Source:    "eventlog",
			Title:     "Successful Logon - Event 4624",
			Details:   "An account was successfully logged on. Account Name: sysadmin, Logon Type: 10 (RDP), Source Address: 10.0.2.15",
			RawData:   `{"EventID":4624,"TargetUserName":"sysadmin","IpAddress":"10.0.2.15","LogonType":10}`,
		}
		sendEvent(ev)

	case "3":
		fmt.Println("Simulating High-privilege Usage (SA login)...")
		ev := EventReport{
			EventId:   generateUUID(),
			AgentId:   globalConfig.AgentId,
			Timestamp: time.Now().UTC(),
			Category:  "privilege",
			Severity:  "warning",
			Source:    "sqlserver",
			Title:     "SQL Server Login Success",
			Details:   "Login succeeded for user 'sa'. Connection source: 192.168.1.44. Application: SQL Server Management Studio.",
			RawData:   `{"Database":"master","User":"sa","ClientIP":"192.168.1.44"}`,
		}
		sendEvent(ev)

	case "4":
		fmt.Println("Simulating Service Stopped...")
		ev := EventReport{
			EventId:   generateUUID(),
			AgentId:   globalConfig.AgentId,
			Timestamp: time.Now().UTC(),
			Category:  "service",
			Severity:  "critical",
			Source:    "service",
			Title:     "MSSQLSERVER",
			Details:   "The SQL Server (MSSQLSERVER) service entered the stopped state.",
			RawData:   `{"EventID":7036,"ServiceName":"MSSQLSERVER","State":"Stopped"}`,
		}
		sendEvent(ev)

	case "5":
		fmt.Println("Simulating Database Backup Failed...")
		ev := EventReport{
			EventId:   generateUUID(),
			AgentId:   globalConfig.AgentId,
			Timestamp: time.Now().UTC(),
			Category:  "backup",
			Severity:  "critical",
			Source:    "sqlserver",
			Title:     "Backup job failed - Job_DailyDBBackup",
			Details:   "SQL Server Agent Job 'Job_DailyDBBackup' failed. Error: Write failure on tape or disk backup file. Status: Failed.",
			RawData:   `{"JobName":"Job_DailyDBBackup","Status":"Failed","ErrorCode":3041}`,
		}
		sendEvent(ev)

	case "6":
		fmt.Println("Simulating User Management (Add Admin)...")
		ev := EventReport{
			EventId:   generateUUID(),
			AgentId:   globalConfig.AgentId,
			Timestamp: time.Now().UTC(),
			Category:  "user_mgmt",
			Severity:  "warning",
			Source:    "eventlog",
			Title:     "A member was added to a security-enabled global group - Event 4728",
			Details:   "Subject Account: Administrator. Member Name: CN=hacker,CN=Users. Group Name: Administrators.",
			RawData:   `{"EventID":4728,"MemberName":"hacker","GroupName":"Administrators"}`,
		}
		sendEvent(ev)

	case "7":
		fmt.Println("Simulating Firewall Modification...")
		ev := EventReport{
			EventId:   generateUUID(),
			AgentId:   globalConfig.AgentId,
			Timestamp: time.Now().UTC(),
			Category:  "firewall",
			Severity:  "warning",
			Source:    "eventlog",
			Title:     "Windows Defender Firewall rule added - Event 2004",
			Details:   "A rule has been added to the Windows Defender Firewall exception list. Rule Name: Allow Port 4444 Inbound, Action: Allow.",
			RawData:   `{"EventID":2004,"RuleName":"Allow Port 4444 Inbound","Action":"Allow"}`,
		}
		sendEvent(ev)

	case "8":
		fmt.Println("Simulating Root SSH Login...")
		ev := EventReport{
			EventId:   generateUUID(),
			AgentId:   globalConfig.AgentId,
			Timestamp: time.Now().UTC(),
			Category:  "login",
			Severity:  "critical",
			Source:    "sshlog",
			Title:     "Accepted publickey for root from 192.168.1.18 port 52311 ssh2",
			Details:   "Direct SSH session established using Root account. Authorized by RSA key SHA256:abcd1234. Source IP: 192.168.1.18",
			RawData:   `{"User":"root","IP":"192.168.1.18","Port":52311}`,
		}
		sendEvent(ev)

	case "9":
		fmt.Println("Simulating Crontab Change...")
		ev := EventReport{
			EventId:   generateUUID(),
			AgentId:   globalConfig.AgentId,
			Timestamp: time.Now().UTC(),
			Category:  "crontab",
			Severity:  "warning",
			Source:    "fsnotify",
			Title:     "File modified: /var/spool/cron/crontabs/root",
			Details:   "Crontab scheduled task configuration has been modified by user root. Persistence risk check needed.",
			RawData:   `{"FilePath":"/var/spool/cron/crontabs/root","Action":"Modified"}`,
		}
		sendEvent(ev)

	default:
		fmt.Println("Unknown choice.")
	}
}

func runRealMonitor() {
	fmt.Println("Running in REAL monitor mode. Monitoring security logs...")
	for {
		time.Sleep(30 * time.Second)
		fmt.Println("Real monitor loop: scanning logs. No security issues detected.")
	}
}

func startCollectorServer(port int) {
	mux := http.NewServeMux()
	mux.HandleFunc("/api/collector/heartbeat", handleCollectorHeartbeat)
	mux.HandleFunc("/api/collector/events", handleCollectorEvents)

	addr := fmt.Sprintf(":%d", port)
	fmt.Printf("Starting Master Mode Collector Server on: http://localhost%s/\n", addr)

	err := http.ListenAndServe(addr, mux)
	if err != nil {
		fmt.Printf("Failed to start Collector Server on port %d: %v\n", port, err)
	}
}

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

	// Copy response headers
	for k, vv := range resp.Header {
		for _, v := range vv {
			w.Header().Add(k, v)
		}
	}
	w.WriteHeader(resp.StatusCode)
	io.Copy(w, resp.Body)
}

func handleCollectorEvents(w http.ResponseWriter, r *http.Request) {
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
	url := fmt.Sprintf("%s/api/events", globalConfig.ServerUrl)
	fmt.Println("[Collector] Forwarding events to Central Server")

	req, err := http.NewRequest(http.MethodPost, url, bytes.NewBuffer(bodyBytes))
	if err != nil {
		http.Error(w, "Failed to create request", http.StatusInternalServerError)
		return
	}

	req.Header.Set("Content-Type", "application/json")
	req.Header.Set("X-Api-Key", globalConfig.ApiKey)

	resp, err := client.Do(req)
	if err != nil {
		fmt.Printf("[Collector] Error contacting Central Server: %v. Appending to offline buffer.\n", err)
		appendOfflineBuffer(bodyBytes)
		w.Header().Set("Content-Type", "application/json")
		w.WriteHeader(http.StatusOK)
		w.Write([]byte(`{"buffered":true}`))
		return
	}
	defer resp.Body.Close()

	// Copy response headers
	for k, vv := range resp.Header {
		for _, v := range vv {
			w.Header().Add(k, v)
		}
	}
	w.WriteHeader(resp.StatusCode)
	io.Copy(w, resp.Body)
}

var offlineMutex sync.Mutex
var recoveryInterval = 5 * time.Second

func appendOfflineBuffer(eventBytes []byte) {
	offlineMutex.Lock()
	defer offlineMutex.Unlock()

	var events []EventReport
	if err := json.Unmarshal(eventBytes, &events); err != nil {
		var singleEvent EventReport
		if err := json.Unmarshal(eventBytes, &singleEvent); err == nil {
			events = []EventReport{singleEvent}
		} else {
			fmt.Printf("Error unmarshaling event bytes: %v\n", err)
			return
		}
	}

	file, err := os.OpenFile("offline_buffer.json", os.O_CREATE|os.O_WRONLY|os.O_APPEND, 0644)
	if err != nil {
		fmt.Printf("Error opening offline buffer file: %v\n", err)
		return
	}
	defer file.Close()

	for _, event := range events {
		line, err := json.Marshal(event)
		if err != nil {
			fmt.Printf("Error marshaling event to JSON Line: %v\n", err)
			continue
		}
		if _, err := file.Write(append(line, '\n')); err != nil {
			fmt.Printf("Error writing line to offline buffer: %v\n", err)
		}
	}
}

func startOfflineRecoveryWorker() {
	go func() {
		ticker := time.NewTicker(recoveryInterval)
		defer ticker.Stop()

		for range ticker.C {
			if _, err := os.Stat("offline_buffer.json"); os.IsNotExist(err) {
				continue
			}

			// Ping Central Server
			pingUrl := fmt.Sprintf("%s/api/overview", globalConfig.ServerUrl)
			req, err := http.NewRequest("GET", pingUrl, nil)
			if err != nil {
				continue
			}
			req.Header.Set("X-Api-Key", globalConfig.ApiKey)
			resp, err := client.Do(req)
			if err != nil {
				continue
			}
			resp.Body.Close()

			if resp.StatusCode == http.StatusOK {
				flushOfflineBuffer()
			}
		}
	}()
}

func flushOfflineBuffer() {
	offlineMutex.Lock()
	defer offlineMutex.Unlock()

	if _, err := os.Stat("offline_buffer.json"); os.IsNotExist(err) {
		return
	}

	file, err := os.Open("offline_buffer.json")
	if err != nil {
		fmt.Printf("Error opening offline buffer for reading: %v\n", err)
		return
	}

	var events []EventReport
	scanner := bufio.NewScanner(file)
	for scanner.Scan() {
		line := scanner.Bytes()
		if len(bytes.TrimSpace(line)) == 0 {
			continue
		}
		var ev EventReport
		if err := json.Unmarshal(line, &ev); err != nil {
			fmt.Printf("Error unmarshaling buffered event: %v\n", err)
			continue
		}
		events = append(events, ev)
	}
	file.Close()

	if len(events) == 0 {
		os.Remove("offline_buffer.json")
		return
	}

	url := fmt.Sprintf("%s/api/events", globalConfig.ServerUrl)
	jsonBytes, err := json.Marshal(events)
	if err != nil {
		fmt.Printf("Error marshaling events for recovery: %v\n", err)
		return
	}

	req, err := http.NewRequest("POST", url, bytes.NewBuffer(jsonBytes))
	if err != nil {
		fmt.Printf("Error creating recovery request: %v\n", err)
		return
	}
	req.Header.Set("Content-Type", "application/json")
	req.Header.Set("X-Api-Key", globalConfig.ApiKey)

	resp, err := client.Do(req)
	if err != nil {
		fmt.Printf("Error sending recovered events: %v\n", err)
		return
	}
	defer resp.Body.Close()

	if resp.StatusCode == http.StatusOK {
		fmt.Printf("Successfully flushed %d events from offline buffer.\n", len(events))
		os.Remove("offline_buffer.json")
	} else {
		fmt.Printf("Failed to flush offline buffer, status: %d\n", resp.StatusCode)
	}
}

func getEventEndpoint() string {
	if strings.ToLower(globalConfig.Mode) == "client" && globalConfig.CollectorUrl != "" {
		return globalConfig.CollectorUrl + "/api/collector/events"
	}
	return globalConfig.ServerUrl + "/api/v1/collector/events"
}

func getHeartbeatEndpoint(agentId string) string {
	if strings.ToLower(globalConfig.Mode) == "client" && globalConfig.CollectorUrl != "" {
		return globalConfig.CollectorUrl + "/api/collector/heartbeat?agentId=" + agentId
	}
	return globalConfig.ServerUrl + "/api/v1/collector/heartbeat"
}

// Embedded Web Application HTML content
const hisHtmlContent = `
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>MedConnect - Hospital Information System</title>
    <link href="https://fonts.googleapis.com/css2?family=Outfit:wght@300;400;600;700&display=swap" rel="stylesheet">
    <style>
        :root {
            --primary: #0284c7;
            --primary-hover: #0369a1;
            --bg-dark: #0f172a;
            --panel-bg: rgba(30, 41, 59, 0.7);
            --border: rgba(255, 255, 255, 0.08);
            --text: #f8fafc;
            --text-secondary: #94a3b8;
            --success: #10b981;
            --danger: #ef4444;
            --warning: #f59e0b;
        }

        * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
        }

        body {
            font-family: 'Outfit', sans-serif;
            background: radial-gradient(circle at top right, #1e293b, #0f172a);
            color: var(--text);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 1.5rem;
        }

        .container {
            width: 100%;
            max-width: 650px;
            background: var(--panel-bg);
            backdrop-filter: blur(16px);
            border: 1px solid var(--border);
            border-radius: 16px;
            box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5);
            padding: 2.5rem;
            transition: all 0.3s ease;
        }

        .header {
            text-align: center;
            margin-bottom: 2rem;
        }

        .logo {
            font-size: 2.5rem;
            color: var(--primary);
            font-weight: 700;
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 10px;
            margin-bottom: 0.5rem;
        }

        .subtitle {
            color: var(--text-secondary);
            font-size: 0.9rem;
            letter-spacing: 0.5px;
        }

        /* Form styling */
        .form-group {
            margin-bottom: 1.25rem;
            display: flex;
            flex-direction: column;
            gap: 0.5rem;
        }

        label {
            font-size: 0.85rem;
            font-weight: 600;
            color: var(--text-secondary);
            text-transform: uppercase;
        }

        input {
            background: rgba(15, 23, 42, 0.8);
            border: 1px solid var(--border);
            border-radius: 8px;
            padding: 12px 16px;
            color: #fff;
            font-size: 0.95rem;
            outline: none;
            transition: all 0.2s;
        }

        input:focus {
            border-color: var(--primary);
            box-shadow: 0 0 10px rgba(2, 132, 199, 0.3);
        }

        button {
            width: 100%;
            padding: 12px;
            border-radius: 8px;
            border: none;
            background: var(--primary);
            color: #fff;
            font-weight: 600;
            font-size: 1rem;
            cursor: pointer;
            transition: background 0.2s;
            margin-top: 0.75rem;
        }

        button:hover {
            background: var(--primary-hover);
        }

        .notice {
            margin-top: 1rem;
            text-align: center;
            font-size: 0.8rem;
            color: var(--text-secondary);
            background: rgba(255,255,255,0.02);
            padding: 8px;
            border-radius: 6px;
        }

        .error-message {
            background: rgba(239, 68, 68, 0.15);
            border: 1px solid var(--danger);
            color: #fc8181;
            padding: 10px;
            border-radius: 6px;
            font-size: 0.85rem;
            margin-bottom: 1rem;
            display: none;
            text-align: center;
        }

        /* Console styling */
        .console {
            display: none;
        }

        .console-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            border-bottom: 1px solid var(--border);
            padding-bottom: 1rem;
            margin-bottom: 1.5rem;
        }

        .badge {
            font-size: 0.75rem;
            font-weight: 700;
            padding: 4px 10px;
            border-radius: 12px;
            text-transform: uppercase;
        }

        .badge.active {
            background: rgba(16, 185, 129, 0.15);
            color: var(--success);
            border: 1px solid var(--success);
        }

        .badge.inactive {
            background: rgba(239, 68, 68, 0.15);
            color: var(--danger);
            border: 1px solid var(--danger);
        }

        .service-row {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 1rem;
            border: 1px solid var(--border);
            background: rgba(255,255,255,0.01);
            border-radius: 10px;
            margin-bottom: 1rem;
        }

        .service-info h4 {
            font-size: 0.95rem;
            font-weight: 600;
        }

        .service-info p {
            font-size: 0.75rem;
            color: var(--text-secondary);
            margin-top: 2px;
        }

        .btn-action {
            width: auto;
            margin: 0;
            padding: 6px 16px;
            font-size: 0.8rem;
        }

        .btn-action.stop {
            background: var(--danger);
        }

        .btn-action.stop:hover {
            background: #dc2626;
        }

        .btn-action.start {
            background: var(--success);
        }

        .btn-action.start:hover {
            background: #059669;
        }
    </style>
</head>
<body>

    <div class="container">
        <!-- 1. LOGIN SCREEN -->
        <div id="loginSection">
            <div class="header">
                <div class="logo">✙ MedConnect</div>
                <div class="subtitle">HIS Administration & Clinical Portal</div>
            </div>

            <div id="errBox" class="error-message"></div>

            <form id="loginForm" onsubmit="handleLogin(event)">
                <div class="form-group" style="display: flex; flex-direction: row; gap: 10px; margin-bottom: 12px;">
                    <div style="flex: 1; display: flex; flex-direction: column; gap: 4px;">
                        <label>Hospital Code</label>
                        <select id="hospitalCode" style="background: rgba(15, 23, 42, 0.8); border: 1px solid var(--border); border-radius: 8px; padding: 12px; color: #fff; font-size: 0.95rem; outline: none;" onchange="updateServerList()">
                            <option value="HOSP_A">Hospital A (HOSP_A)</option>
                            <option value="HOSP_B">Hospital B (HOSP_B)</option>
                        </select>
                    </div>
                    <div style="flex: 1; display: flex; flex-direction: column; gap: 4px;">
                        <label>Target Server</label>
                        <select id="serverCode" style="background: rgba(15, 23, 42, 0.8); border: 1px solid var(--border); border-radius: 8px; padding: 12px; color: #fff; font-size: 0.95rem; outline: none; width: 100%;">
                            <!-- populated by script -->
                        </select>
                    </div>
                </div>
                <div class="form-group">
                    <label>Doctor Username</label>
                    <input type="text" id="username" placeholder="doctor" required autocomplete="off">
                </div>
                <div class="form-group">
                    <label>Access Password</label>
                    <input type="password" id="password" placeholder="••••••••" required>
                </div>
                <button type="submit" id="loginBtn">Authorize Session</button>
            </form>

            <div class="notice">
                🔑 Hint: <b>doctor</b> / <b>password123</b>. Try inputting wrong credentials to test Brute-force, or SQL Injection payloads like <b>' OR '1'='1</b>.
            </div>
        </div>

        <!-- 2. SYSTEM CONTROL CONSOLE -->
        <div id="consoleSection" class="console">
            <div class="console-header">
                <div>
                    <h3 style="font-size: 1.25rem;">MedConnect HIS Portal</h3>
                    <span style="font-size: 0.75rem; color: var(--text-secondary);">Logged in as: Dr. John Doe (Administrator)</span>
                </div>
                <button onclick="handleLogout()" class="btn-action" style="background: rgba(255,255,255,0.05); border: 1px solid var(--border); font-size: 0.75rem;">Logout</button>
            </div>

            <h4 style="font-size: 0.85rem; color: var(--text-secondary); text-transform: uppercase; margin-bottom: 0.75rem;">System Database Services</h4>

            <!-- SQL Server service control -->
            <div class="service-row">
                <div class="service-info">
                    <h4 id="dbTitle">Central Database (MSSQLSERVER)</h4>
                    <p id="dbDesc">Clinical data repository, patient records, and medical logs</p>
                </div>
                <div style="display: flex; align-items: center; gap: 12px;">
                    <span id="dbBadge" class="badge active">Active</span>
                    <button id="dbBtn" onclick="toggleDbService()" class="btn-action stop">Stop</button>
                </div>
            </div>

            <!-- HL7 Sync Server -->
            <div class="service-row">
                <div class="service-info">
                    <h4>HL7 Interoperability Core Hub</h4>
                    <p>Syncs patient records with Department of Health endpoints</p>
                </div>
                <span class="badge active">Active</span>
            </div>

            <!-- Daily backup control -->
            <div class="service-row">
                <div class="service-info">
                    <h4 id="backupTitle">Automated Backup Engine</h4>
                    <p id="backupDesc">Nightly encrypted backup to NAS</p>
                </div>
                <div style="display: flex; align-items: center; gap: 12px;">
                    <span id="backupBadge" class="badge active">Normal</span>
                    <button id="backupBtn" onclick="triggerBackupFailure()" class="btn-action" style="background: var(--warning)">Fail Job</button>
                </div>
            </div>

            <div class="notice" style="background: rgba(16, 185, 129, 0.05); color: var(--success); border: 1px solid rgba(16, 185, 129, 0.15)">
                🛡️ OneSecurity Agent is actively audit logging this HIS instance. Service status changes are forwarded to the Dashboard.
            </div>
        </div>
    </div>

    <script>
        let dbActive = true;
        let backupFailed = false;

        const serversMap = {
            'HOSP_A': [
                { code: 'agent-win-his-01', name: 'DESKTOP-FC9F6G9 (Windows Server 2022)' },
                { code: 'agent-lnx-db-01', name: 'ubuntu-hosp-a-db (Ubuntu Linux 22.04)' },
                { code: 'agent-win-app-01', name: 'win-hosp-a-app (Windows Server 2019)' }
            ],
            'HOSP_B': [
                { code: 'agent-win-his-02', name: 'DESKTOP-FC9F6G9 (Windows Server 2022)' },
                { code: 'agent-lnx-web-02', name: 'ubuntu-hosp-b-web (Ubuntu Linux 20.04)' }
            ]
        };

        function updateServerList() {
            const hospSelect = document.getElementById('hospitalCode');
            const serverSelect = document.getElementById('serverCode');
            const hospVal = hospSelect.value;
            const servers = serversMap[hospVal] || [];
            
            serverSelect.innerHTML = '';
            servers.forEach(s => {
                const opt = document.createElement('option');
                opt.value = s.code;
                opt.innerText = s.name;
                serverSelect.appendChild(opt);
            });
        }

        window.addEventListener('DOMContentLoaded', updateServerList);

        async function handleLogin(e) {
            e.preventDefault();
            const user = document.getElementById('username').value;
            const pass = document.getElementById('password').value;
            const hospCode = document.getElementById('hospitalCode').value;
            const serverCode = document.getElementById('serverCode').value;
            const errBox = document.getElementById('errBox');
            
            errBox.style.display = 'none';

            try {
                const res = await fetch('/api/his/login', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ 
                        username: user, 
                        password: pass,
                        hospitalCode: hospCode,
                        serverCode: serverCode
                    })
                });
                
                const data = await res.json();
                if (data.success) {
                    // Update UI text dynamically based on Windows/Linux target server OS
                    const isLinux = serverCode.includes('-lnx-');
                    const dbTitle = document.getElementById('dbTitle');
                    const dbDesc = document.getElementById('dbDesc');
                    const backupTitle = document.getElementById('backupTitle');
                    const backupDesc = document.getElementById('backupDesc');

                    if (isLinux) {
                        dbTitle.innerText = 'Clinical Database (postgresql.service)';
                        dbDesc.innerText = 'Clinical PostgreSQL database container (systemd target)';
                        backupTitle.innerText = 'Database Backup Script (backup_db.sh)';
                        backupDesc.innerText = 'Cron scheduled backup to offsite storage';
                    } else {
                        dbTitle.innerText = 'Central Database (MSSQLSERVER)';
                        dbDesc.innerText = 'Clinical data repository, patient records, and medical logs';
                        backupTitle.innerText = 'Automated Backup Engine';
                        backupDesc.innerText = 'Nightly encrypted backup to NAS';
                    }

                    document.getElementById('loginSection').style.display = 'none';
                    document.getElementById('consoleSection').style.display = 'block';
                } else {
                    errBox.innerText = data.message;
                    errBox.style.display = 'block';
                    if (data.hacked) {
                        errBox.style.background = 'rgba(239, 68, 68, 0.2)';
                        errBox.style.color = '#ff9c9c';
                    }
                }
            } catch (err) {
                errBox.innerText = "Error connecting to MedConnect HIS backend service.";
                errBox.style.display = 'block';
            }
        }

        function handleLogout() {
            document.getElementById('username').value = '';
            document.getElementById('password').value = '';
            document.getElementById('loginSection').style.display = 'block';
            document.getElementById('consoleSection').style.display = 'none';
            document.getElementById('errBox').style.display = 'none';
            
            // Reset console buttons
            const dbBtn = document.getElementById('dbBtn');
            const dbBadge = document.getElementById('dbBadge');
            const backupBtn = document.getElementById('backupBtn');
            const backupBadge = document.getElementById('backupBadge');

            dbActive = true;
            dbBadge.innerText = 'Active';
            dbBadge.className = 'badge active';
            dbBtn.innerText = 'Stop';
            dbBtn.className = 'btn-action stop';
            dbBtn.disabled = false;

            backupFailed = false;
            backupBadge.innerText = 'Normal';
            backupBadge.className = 'badge active';
            backupBtn.innerText = 'Fail Job';
            backupBtn.style.opacity = '1';
            backupBtn.disabled = false;
        }

        async function toggleDbService() {
            const dbBtn = document.getElementById('dbBtn');
            const dbBadge = document.getElementById('dbBadge');

            if (dbActive) {
                dbBtn.disabled = true;
                const res = await fetch('/api/his/service/stop', { method: 'POST' });
                if (res.ok) {
                    dbActive = false;
                    dbBadge.innerText = 'Stopped';
                    dbBadge.className = 'badge inactive';
                    dbBtn.innerText = 'Start';
                    dbBtn.className = 'btn-action start';
                }
                dbBtn.disabled = false;
            } else {
                dbBtn.disabled = true;
                const res = await fetch('/api/his/service/start', { method: 'POST' });
                if (res.ok) {
                    dbActive = true;
                    dbBadge.innerText = 'Active';
                    dbBadge.className = 'badge active';
                    dbBtn.innerText = 'Stop';
                    dbBtn.className = 'btn-action stop';
                }
                dbBtn.disabled = false;
            }
        }

        async function triggerBackupFailure() {
            const backupBtn = document.getElementById('backupBtn');
            const backupBadge = document.getElementById('backupBadge');

            backupBtn.disabled = true;
            const res = await fetch('/api/his/backup/fail', { method: 'POST' });
            if (res.ok) {
                backupBadge.innerText = 'Failed';
                backupBadge.className = 'badge inactive';
                backupBtn.innerText = 'Failed';
                backupBtn.style.opacity = '0.5';
            }
        }
    </script>
</body>
</html>
`
