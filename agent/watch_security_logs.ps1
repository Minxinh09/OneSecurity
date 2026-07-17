# watch_security_logs.ps1
# Real-time Windows Security Event Log Monitor
# Run this script as ADMINISTRATOR on the Main Computer

$ErrorActionPreference = "Stop"
$lastCheckTime = [DateTime]::UtcNow

$serverUrl = "http://localhost:5000/api/events"
$apiKey = "onesecurity_secret_key_2026"
$agentId = "agent-win-his-01"

Write-Host "==================================================" -ForegroundColor Green
Write-Host "   OneSecurity Windows Security Log Live Monitor   " -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Green
Write-Host "Server URL: $serverUrl" -ForegroundColor Cyan
Write-Host "Status: Scanning Windows Security EventLog..." -ForegroundColor Cyan
Write-Host "Watching Event ID 4625 (Failed) & 4624 (Success)..." -ForegroundColor Cyan
Write-Host "Press Ctrl+C to stop." -ForegroundColor DarkGray
Write-Host "==================================================" -ForegroundColor Green

while ($true) {
    try {
        # Query Security Log for events generated after the last check time
        $events = Get-WinEvent -FilterHashtable @{
            LogName = 'Security'
            Id = 4624, 4625
            StartTime = $lastCheckTime.ToLocalTime()
        } -ErrorAction SilentlyContinue | Sort-Object TimeCreated

        if ($events) {
            $payload = @()
            foreach ($event in $events) {
                # Advance tracking time to avoid reprocessing
                if ($event.TimeCreated.ToUniversalTime() -ge $lastCheckTime) {
                    $lastCheckTime = $event.TimeCreated.ToUniversalTime().AddSeconds(1)
                }

                # Parse event XML payload
                $xml = [xml]$event.ToXml()
                $eventData = $xml.Event.EventData.Data
                
                # Extract attributes
                $targetUser = ""
                $ipAddress = ""
                foreach ($data in $eventData) {
                    if ($data.Name -eq "TargetUserName") { $targetUser = $data.'#text' }
                    if ($data.Name -eq "IpAddress") { $ipAddress = $data.'#text' }
                }

                # Skip local machine system-level accounts to reduce noise
                if ($targetUser -eq "SYSTEM" -or $targetUser.EndsWith("$")) {
                    continue;
                }

                # Default local IP if empty
                if ([string]::IsNullOrEmpty($ipAddress) -or $ipAddress -eq "-") {
                    $ipAddress = "127.0.0.1"
                }

                $category = "login"
                $severity = "info"
                $title = "Successful Logon - Event 4624"
                if ($event.Id -eq 4625) {
                    $severity = "warning"
                    $title = "Logon Failure - Event 4625"
                }

                $details = "Account Name: $targetUser, Source Address: $ipAddress, Event ID: $($event.Id)"
                $rawData = "{`"EventID`":$($event.Id),`"TargetUserName`":`"$targetUser`",`"IpAddress`":`"$ipAddress`"}"

                $payload += @{
                    eventId = "win-evt-" + $event.RecordId + "-" + (Get-Random)
                    agentId = $agentId
                    timestamp = $event.TimeCreated.ToUniversalTime().ToString("o")
                    category = $category
                    severity = $severity
                    source = "eventlog"
                    title = $title
                    details = $details
                    rawData = $rawData
                }

                Write-Host "$(Get-Date -Format 'HH:mm:ss') - Detected Logon Event $($event.Id) for '$targetUser' from $ipAddress" -ForegroundColor Yellow
            }

            # Forward events to central server
            if ($payload.Count -gt 0) {
                $json = $payload | ConvertTo-Json
                $headers = @{ "X-Api-Key" = $apiKey }
                Invoke-RestMethod -Uri $serverUrl -Method Post -Body $json -ContentType "application/json" -Headers $headers | Out-Null
                Write-Host "Successfully forwarded $($payload.Count) logon event(s) to Server." -ForegroundColor Green
            }
        }
    } catch {
        # Event log query sometimes throws if no logs exist, ignore it
        if ($_.Exception.Message -notlike "*No events were found*") {
            Write-Warning "Error reading EventLog: $($_.Exception.Message)"
        }
    }

    Start-Sleep -Seconds 2
}
