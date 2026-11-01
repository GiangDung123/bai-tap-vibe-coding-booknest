$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$configPath = Join-Path $root 'backend\BookNest.Api\appsettings.Local.json'
if (!(Test-Path $configPath)) { throw 'Run scripts/Setup-Local.ps1 first, then confirm SQL Server connection settings.' }
$config = Get-Content $configPath -Raw | ConvertFrom-Json
$configured = $config.ConnectionStrings.BookNest
$database = 'booknest_test_' + [Guid]::NewGuid().ToString('N')
$connection = [regex]::Replace($configured, '(?i)(Initial Catalog|Database)=booknest_db;', "Database=$database;")
if ($connection -eq $configured) { throw 'Expected Database=booknest_db in local SQL Server connection settings.' }
$env:BOOKNEST_TEST_CONNECTION = $connection
try {
    Set-Location $root
    dotnet test backend\BookNest.Tests --logger 'trx;LogFileName=business.trx' --results-directory TestResults
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed. Inspect TestResults. Dedicated test database retained for inspection.' }
    Write-Host "Test database retained: $database (no automatic DROP)."
} finally {
    Remove-Item Env:BOOKNEST_TEST_CONNECTION -ErrorAction SilentlyContinue
}
