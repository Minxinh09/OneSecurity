# Task 1 Report: CLI Flags, Config Extension, and Test Scaffolding

## Overview
I have completed Task 1 of the EDR Agent Collector architecture. I extended the `Config` struct to include `CollectorUrl`, refactored CLI flag parsing into a testable helper function `parseFlags` using `flag.NewFlagSet`, and implemented unit testing following the Test-Driven Development (TDD) workflow.

---

## 1. TDD Steps Followed

### Step 1: Create a Failing Test
I created `agent/main_test.go` containing `TestParseFlagsOverrides(t *testing.T)` to assert that the custom CLI flags override the config parameters `Mode` and `CollectorUrl`.
At this stage, `parseFlags` did not exist, and the `Config` struct did not have a `CollectorUrl` field.

### Step 2: Verify Test Failure
I executed the tests inside the `agent` directory:
```
C:\Users\MinhHQ\.gemini\antigravity\scratch\onesecurity\tools\go\bin\go.exe test -v .
```
This failed as expected during compilation with the following errors:
```
# onesecurity-agent [onesecurity-agent.test]
.\main_test.go:11:3: unknown field CollectorUrl in struct literal of type Config
.\main_test.go:16:2: undefined: parseFlags
.\main_test.go:22:18: globalConfig.CollectorUrl undefined (type Config has no field or method CollectorUrl)
.\main_test.go:23:90: globalConfig.CollectorUrl undefined (type Config has no field or method CollectorUrl)
FAIL	onesecurity-agent [build failed]
FAIL
```

### Step 3: Implement Code Changes
I modified `agent/main.go` to support the required changes:
1. **Config Extension**: Added `CollectorUrl string `json:"CollectorUrl"`` to the `Config` struct.
2. **Flag Parsing Refactor**: Implemented `parseFlags(args []string) int` using `flag.NewFlagSet` and `flag.ContinueOnError`. The function:
   - Sets up `-hospital`, `-port`, `-agentid`, `-hostname`, `-mode`, and `-collector` flags.
   - Parses the given string arguments.
   - Updates `globalConfig` values accordingly if the flags are non-empty.
   - Returns the parsed port value.
3. **Integration into `main`**: Updated `main()` to invoke `parseFlags(os.Args[1:])` and pass the returned port to `startHisWebServer`.

### Step 4: Verify Test Success
I ran the test command again:
```
C:\Users\MinhHQ\.gemini\antigravity\scratch\onesecurity\tools\go\bin\go.exe test -v .
```
The test suite successfully compiled and passed:
```
=== RUN   TestParseFlagsOverrides
--- PASS: TestParseFlagsOverrides (0.00s)
PASS
ok  	onesecurity-agent	0.290s
```

---

## 2. File Verification Links
- Created: [agent/main_test.go](file:///C:/Users/MinhHQ/.gemini/antigravity/scratch/onesecurity/agent/main_test.go)
- Modified: [agent/main.go](file:///C:/Users/MinhHQ/.gemini/antigravity/scratch/onesecurity/agent/main.go)
