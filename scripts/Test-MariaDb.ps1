param([int]$Port = 0)
$ErrorActionPreference = 'Stop'; $root = Split-Path $PSScriptRoot -Parent
$config = Get-Content (Join-Path $root 'backend\BookNest.Api\appsettings.Local.json') -Raw | ConvertFrom-Json
# Derive dedicated database, retaining the local credentials without logging them.
$database = 'booknest_test_' + [Guid]::NewGuid().ToString('N')
$connection = $config.ConnectionStrings.BookNest -replace 'Database=booknest_db;', "Database=$database;"
if ($connection -eq $config.ConnectionStrings.BookNest) { throw 'Expected explicit Database=booknest_db in local configuration; configure test connection manually otherwise.' }
$env:BOOKNEST_TEST_CONNECTION = $connection
try {
    Set-Location $root
    $ErrorActionPreference = 'Continue'
    dotnet test backend\BookNest.Tests --logger 'trx;LogFileName=business.trx' --results-directory TestResults
    $ErrorActionPreference = 'Stop'
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed. Inspect TestResults. Dedicated test database retained for inspection.' }
    Write-Host "Test database retained: $database (no automatic DROP)."
} finally { Remove-Item Env:BOOKNEST_TEST_CONNECTION -ErrorAction SilentlyContinue }