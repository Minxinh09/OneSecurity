# Task 1 Brief: CLI Flags, Config Extension, and Test Scaffolding

## Goal:
Extend config structure, implement flag overriding via `parseFlags(args []string)`, and write automated unit tests to verify the override logic.

## Scope:
- Define `CollectorUrl` in `Config` struct inside `agent/main.go`.
- Expose helper `parseFlags(args []string)` in `agent/main.go` that parses CLI flags and updates `globalConfig` values.
- Call `parseFlags(os.Args[1:])` in `main()`.
- Create `agent/main_test.go` and write `TestParseFlagsOverrides(t *testing.T)` to verify that passing flags correctly overrides `globalConfig.Mode` and `globalConfig.CollectorUrl`.
- Run test using `..\tools\go\bin\go.exe test -v ./agent` and ensure it passes.
