# Task 4 Brief: Implement Offline Queue buffering and worker recovery

## Goal:
Implement local file-backed offline buffering for event reports when the Central Server is offline, and a background recovery worker to flush the buffer when the connection is restored.

## Scope:
- In `agent/main.go`, implement helper functions:
  - `appendOfflineBuffer(eventBytes []byte)`: Appends incoming events to `offline_buffer.json` (JSON Lines format) using thread-safe write locks (`sync.Mutex`).
  - `startOfflineRecoveryWorker()`: Starts a background worker (goroutine) that pings the Central Server (e.g. `http://localhost:5000/api/overview`) every 5 seconds. If the Central Server is online and `offline_buffer.json` exists:
    - It reads the file, parses all buffered events, sends them in a single batch to the Central Server events endpoint (`http://localhost:5000/api/events`), and deletes the `offline_buffer.json` file.
- Update `handleCollectorEvents` in `agent/main.go` so that if forwarding events to the Central Server fails with an error (e.g. connection refused), it calls `appendOfflineBuffer` and returns `200 OK` (with JSON `{"buffered":true}`) to the client.
- Update `main()` in `agent/main.go` to call `startOfflineRecoveryWorker()` and `startCollectorServer` if `Mode` is `"master"`.
- Write unit tests in `agent/main_test.go` to verify:
  - Writing events to the offline buffer.
  - Flushing the offline buffer when recovery happens.
- Run tests using `..\tools\go\bin\go.exe test -v ./agent` and ensure they pass.
