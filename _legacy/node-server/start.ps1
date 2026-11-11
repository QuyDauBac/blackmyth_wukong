$ErrorActionPreference = "Stop"
$serverScript = Join-Path $PSScriptRoot "server.js"
node $serverScript
