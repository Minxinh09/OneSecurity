package main

import (
	"encoding/json"
	"bytes"
	"fmt"
	"io"
	"net"
	"net/http"
	"net/http/httptest"
	"os"
	"strings"
	"testing"
	"time"
)

func TestParseFlagsOverrides(t *testing.T) {
	// Initialize globalConfig with default/known values
	globalConfig = Config{
		Mode:         "simulator",
		CollectorUrl: "http://localhost:5000",
	}

	// Override mode and collector URL
	args := []string{"-mode", "master", "-collector", "http://localhost:9000"}
	parseFlags(args)

	if globalConfig.Mode != "master" {
		t.Errorf("Expected Mode to be 'master', got '%s'", globalConfig.Mode)
	}

	if globalConfig.CollectorUrl != "http://localhost:9000" {
		t.Errorf("Expected CollectorUrl to be 'http://localhost:9000', got '%s'", globalConfig.CollectorUrl)
	}
}

func getFreePort() (int, error) {
	addr, err := net.ResolveTCPAddr("tcp", "localhost:0")
	if err != nil {
		return 0, err
	}
	l, err := net.ListenTCP("tcp", addr)
	if err != nil {
		return 0, err
	}
	defer l.Close()
	return l.Addr().(*net.TCPAddr).Port, nil
}

func TestCollectorServer(t *testing.T) {
	// 1. Start a mock Central Server using httptest
	mockCentralReceivedHeartbeat := false
	mockCentralReceivedEvents := false
	var receivedApiKey string
	var receivedAgentId string

	mockCentral := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		receivedApiKey = r.Header.Get("X-Api-Key")
		if r.Method != "POST" {
			http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
			return
		}

		if strings.HasPrefix(r.URL.Path, "/api/agents/") && strings.HasSuffix(r.URL.Path, "/heartbeat") {
			mockCentralReceivedHeartbeat = true
			parts := strings.Split(r.URL.Path, "/")
			if len(parts) >= 4 {
				receivedAgentId = parts[3]
			}
			w.WriteHeader(http.StatusOK)
			w.Write([]byte(`{"success":true,"message":"heartbeat received"}`))
			return
		}

		if r.URL.Path == "/api/events" {
			mockCentralReceivedEvents = true
			w.WriteHeader(http.StatusOK)
			w.Write([]byte(`{"success":true,"message":"events received"}`))
			return
		}

		http.Error(w, "Not found", http.StatusNotFound)
	}))
	defer mockCentral.Close()

	// 2. Configure globalConfig
	originalConfig := globalConfig
	defer func() { globalConfig = originalConfig }()

	globalConfig = Config{
		ServerUrl:    mockCentral.URL,
		ApiKey:       "test-api-key-123",
		CollectorUrl: "",
		Mode:         "master",
	}

	// 3. Find a free port and start collector server
	port, err := getFreePort()
	if err != nil {
		t.Fatalf("Failed to get free port: %v", err)
	}

	// Start collector server in background
	go startCollectorServer(port)

	// Wait a moment for server to start
	time.Sleep(100 * time.Millisecond)

	// 4. Test heartbeat forwarding
	heartbeatPayload := []byte(`{"cpuPercent":22.5,"ramPercent":60.1,"diskPercent":75.0,"uptime":120.0}`)
	client := &http.Client{Timeout: 2 * time.Second}

	hbUrl := fmt.Sprintf("http://localhost:%d/api/collector/heartbeat?agentId=client-agent-007", port)
	req, err := http.NewRequest("POST", hbUrl, bytes.NewBuffer(heartbeatPayload))
	if err != nil {
		t.Fatalf("Failed to create heartbeat request: %v", err)
	}
	req.Header.Set("Content-Type", "application/json")

	resp, err := client.Do(req)
	if err != nil {
		t.Fatalf("Failed to send heartbeat to collector: %v", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		t.Errorf("Expected heartbeat response status 200, got %d", resp.StatusCode)
	}
	body, _ := io.ReadAll(resp.Body)
	if !strings.Contains(string(body), "heartbeat received") {
		t.Errorf("Expected heartbeat response to contain 'heartbeat received', got '%s'", string(body))
	}

	if !mockCentralReceivedHeartbeat {
		t.Error("Mock central server did not receive forwarded heartbeat")
	}
	if receivedApiKey != "test-api-key-123" {
		t.Errorf("Expected X-Api-Key to be 'test-api-key-123', got '%s'", receivedApiKey)
	}
	if receivedAgentId != "client-agent-007" {
		t.Errorf("Expected AgentId in forwarding path to be 'client-agent-007', got '%s'", receivedAgentId)
	}

	// 5. Test event forwarding
	eventPayload := []byte(`[{"eventId":"evt-1","agentId":"client-agent-007","category":"login","severity":"warning","source":"sshlog","title":"Failed login","details":"some details","rawData":"{}"}]`)
	eventUrl := fmt.Sprintf("http://localhost:%d/api/collector/events", port)
	req, err = http.NewRequest("POST", eventUrl, bytes.NewBuffer(eventPayload))
	if err != nil {
		t.Fatalf("Failed to create events request: %v", err)
	}
	req.Header.Set("Content-Type", "application/json")

	resp, err = client.Do(req)
	if err != nil {
		t.Fatalf("Failed to send events to collector: %v", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		t.Errorf("Expected events response status 200, got %d", resp.StatusCode)
	}
	body, _ = io.ReadAll(resp.Body)
	if !strings.Contains(string(body), "events received") {
		t.Errorf("Expected events response to contain 'events received', got '%s'", string(body))
	}

	if !mockCentralReceivedEvents {
		t.Error("Mock central server did not receive forwarded events")
	}
}

func TestGetEndpoints(t *testing.T) {
	originalConfig := globalConfig
	defer func() { globalConfig = originalConfig }()

	tests := []struct {
		name              string
		mode              string
		collectorUrl      string
		serverUrl         string
		expectedEvent     string
		expectedHeartbeat string
	}{
		{
			name:              "Client mode with CollectorUrl",
			mode:              "client",
			collectorUrl:      "http://collector:9000",
			serverUrl:         "http://central:5000",
			expectedEvent:     "http://collector:9000/api/collector/events",
			expectedHeartbeat: "http://collector:9000/api/collector/heartbeat?agentId=test-agent",
		},
		{
			name:              "Client mode with empty CollectorUrl",
			mode:              "client",
			collectorUrl:      "",
			serverUrl:         "http://central:5000",
			expectedEvent:     "http://central:5000/api/events",
			expectedHeartbeat: "http://central:5000/api/agents/test-agent/heartbeat",
		},
		{
			name:              "Master mode with CollectorUrl",
			mode:              "master",
			collectorUrl:      "http://collector:9000",
			serverUrl:         "http://central:5000",
			expectedEvent:     "http://central:5000/api/events",
			expectedHeartbeat: "http://central:5000/api/agents/test-agent/heartbeat",
		},
		{
			name:              "Master mode with empty CollectorUrl",
			mode:              "master",
			collectorUrl:      "",
			serverUrl:         "http://central:5000",
			expectedEvent:     "http://central:5000/api/events",
			expectedHeartbeat: "http://central:5000/api/agents/test-agent/heartbeat",
		},
		{
			name:              "Simulator mode with CollectorUrl",
			mode:              "simulator",
			collectorUrl:      "http://collector:9000",
			serverUrl:         "http://central:5000",
			expectedEvent:     "http://central:5000/api/events",
			expectedHeartbeat: "http://central:5000/api/agents/test-agent/heartbeat",
		},
		{
			name:              "Client mode case insensitive mode test",
			mode:              "CLIENT",
			collectorUrl:      "http://collector:9000",
			serverUrl:         "http://central:5000",
			expectedEvent:     "http://collector:9000/api/collector/events",
			expectedHeartbeat: "http://collector:9000/api/collector/heartbeat?agentId=test-agent",
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			globalConfig.Mode = tt.mode
			globalConfig.CollectorUrl = tt.collectorUrl
			globalConfig.ServerUrl = tt.serverUrl

			eventRes := getEventEndpoint()
			if eventRes != tt.expectedEvent {
				t.Errorf("Expected event endpoint '%s', got '%s'", tt.expectedEvent, eventRes)
			}

			hbRes := getHeartbeatEndpoint("test-agent")
			if hbRes != tt.expectedHeartbeat {
				t.Errorf("Expected heartbeat endpoint '%s', got '%s'", tt.expectedHeartbeat, hbRes)
			}
		})
	}
}

func TestOfflineBufferAndRecovery(t *testing.T) {
	// Clean up any existing buffer file
	os.Remove("offline_buffer.json")
	defer os.Remove("offline_buffer.json")

	// 1. Setup invalid config so that forwarding to Central Server fails
	originalConfig := globalConfig
	defer func() { globalConfig = originalConfig }()

	globalConfig = Config{
		ServerUrl:    "http://localhost:59999", // Invalid/closed port
		ApiKey:       "test-api-key-123",
		CollectorUrl: "",
		Mode:         "master",
	}

	// 2. Send events request to collector server handler
	eventPayload := []byte(`[{"eventId":"evt-offline-1","agentId":"client-agent-007","category":"login","severity":"warning","source":"sshlog","title":"Failed login","details":"some details","rawData":"{}"}]`)
	req := httptest.NewRequest("POST", "/api/collector/events", bytes.NewBuffer(eventPayload))
	w := httptest.NewRecorder()

	handleCollectorEvents(w, req)

	// 3. Verify response
	resp := w.Result()
	if resp.StatusCode != http.StatusOK {
		t.Errorf("Expected response status 200, got %d", resp.StatusCode)
	}

	body, _ := io.ReadAll(resp.Body)
	if !strings.Contains(string(body), `"buffered":true`) {
		t.Errorf("Expected body to contain '\"buffered\":true', got '%s'", string(body))
	}

	// 4. Verify offline_buffer.json contains the event
	if _, err := os.Stat("offline_buffer.json"); os.IsNotExist(err) {
		t.Fatal("Expected offline_buffer.json to be created, but it does not exist")
	}

	bufData, err := os.ReadFile("offline_buffer.json")
	if err != nil {
		t.Fatalf("Failed to read offline_buffer.json: %v", err)
	}

	if !strings.Contains(string(bufData), "evt-offline-1") {
		t.Errorf("Expected offline_buffer.json to contain 'evt-offline-1', got '%s'", string(bufData))
	}

	// 5. Setup mock Central Server to receive flushed events
	mockCentralReceivedOverview := false
	mockCentralReceivedEvents := false
	var receivedPayload []byte

	mockCentral := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if r.Method == "GET" && r.URL.Path == "/api/overview" {
			mockCentralReceivedOverview = true
			w.WriteHeader(http.StatusOK)
			w.Write([]byte(`{"status":"ok"}`))
			return
		}
		if r.Method == "POST" && r.URL.Path == "/api/events" {
			mockCentralReceivedEvents = true
			var err error
			receivedPayload, err = io.ReadAll(r.Body)
			if err != nil {
				t.Errorf("Failed to read events payload: %v", err)
			}
			w.WriteHeader(http.StatusOK)
			w.Write([]byte(`{"success":true}`))
			return
		}
		http.Error(w, "Not found", http.StatusNotFound)
	}))
	defer mockCentral.Close()

	// Update config to use mock Central Server
	globalConfig.ServerUrl = mockCentral.URL

	// 6. Start recovery worker with short interval
	originalInterval := recoveryInterval
	recoveryInterval = 100 * time.Millisecond
	defer func() { recoveryInterval = originalInterval }()

	startOfflineRecoveryWorker()

	// Wait for recovery worker to ping, flush and delete the file
	time.Sleep(300 * time.Millisecond)

	// 7. Verify events were successfully received by Central Server
	if !mockCentralReceivedOverview {
		t.Error("Mock central server did not receive overview ping")
	}
	if !mockCentralReceivedEvents {
		t.Error("Mock central server did not receive recovered events")
	}

	var recoveredEvents []EventReport
	if err := json.Unmarshal(receivedPayload, &recoveredEvents); err != nil {
		t.Fatalf("Failed to unmarshal received events: %v", err)
	}

	if len(recoveredEvents) != 1 || recoveredEvents[0].EventId != "evt-offline-1" {
		t.Errorf("Expected recovered event 'evt-offline-1', got %v", recoveredEvents)
	}

	// 8. Verify offline_buffer.json is deleted after successful flush
	if _, err := os.Stat("offline_buffer.json"); !os.IsNotExist(err) {
		t.Error("Expected offline_buffer.json to be deleted after flush, but it still exists")
	}
}



