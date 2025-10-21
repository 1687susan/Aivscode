# Simple mock MCP that echoes JSON lines with a timestamp
while ($line = [Console]::In.ReadLine()) {
    if ($null -eq $line) { break }
    try {
        $obj = $line | ConvertFrom-Json
        $response = @{ received = $obj; ts = (Get-Date).ToString('o') }
        $response | ConvertTo-Json -Compress
    } catch {
        '{"error": "invalid json"}'
    }
}