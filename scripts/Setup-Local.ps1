param([string]$ServerName = 'ADMIN')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$file = Join-Path $root 'backend\BookNest.Api\appsettings.Local.json'
if (Test-Path $file) { throw 'Cau hinh local da ton tai. Khong ghi de. Sua file cuc bo neu can.' }
$bytes = New-Object byte[] 48
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($bytes); $jwt = [Convert]::ToBase64String($bytes)
$rng.GetBytes($bytes); $admin = [Convert]::ToBase64String($bytes).Substring(0,24) + 'aA1!'
$rng.GetBytes($bytes); $customer = [Convert]::ToBase64String($bytes).Substring(0,24) + 'bB2!'
$rng.Dispose()
$config = @{
    ConnectionStrings = @{
        BookNest = "Server=$ServerName;Database=booknest_db;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;"
    }
    Jwt = @{ Secret = $jwt }
    Seed = @{ AdminPassword = $admin; CustomerPassword = $customer }
    AI_PROVIDER = 'Gemini'
    AI_API_KEY = ''
    AI_MODEL = ''
}
$config | ConvertTo-Json -Depth 5 | Set-Content -Encoding UTF8 $file
Write-Host "Da tao cau hinh local cho SQL Server $ServerName. Secret khong duoc in ra terminal."
Write-Host 'Tai khoan: admin@booknest.local / customer@booknest.local. Xem mat khau trong Seed cua file local bang VS Code.'
