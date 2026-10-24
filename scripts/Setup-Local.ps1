param([int]$Port = 0, [switch]$PromptDatabasePassword)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if ($Port -eq 0) {
    $ini = 'C:\xampp\mysql\bin\my.ini'
    $section = ''; foreach ($line in Get-Content $ini) { if ($line -match '^\s*\[(.+)\]') { $section = $Matches[1] }; if ($section -eq 'mysqld' -and $line -match '^\s*port\s*=\s*(\d+)') { $Port = [int]$Matches[1]; break } }
    if (!$Port) { throw 'Khong tim thay port trong [mysqld]. Truyen -Port da xac nhan.' }
}
$file = Join-Path $root 'backend\BookNest.Api\appsettings.Local.json'
if (Test-Path $file) { throw 'Cau hinh local da ton tai. Khong ghi de. Sua file cuc bo neu can.' }
$password = ''
if ($PromptDatabasePassword) { $secure = Read-Host 'Mat khau MariaDB root (khong gui vao chat)' -AsSecureString; $password = [System.Net.NetworkCredential]::new('', $secure).Password }
$bytes = New-Object byte[] 48; $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create(); $rng.GetBytes($bytes); $jwt = [Convert]::ToBase64String($bytes)
$rng.GetBytes($bytes); $admin = [Convert]::ToBase64String($bytes).Substring(0,24) + 'aA1!'
$rng.GetBytes($bytes); $customer = [Convert]::ToBase64String($bytes).Substring(0,24) + 'bB2!'; $rng.Dispose()
$escaped = $password.Replace('"', '""')
$config = @{ ConnectionStrings = @{ BookNest = "Server=localhost;Port=$Port;Database=booknest_db;User=root;Password=`"$escaped`";" }; Jwt = @{ Secret = $jwt }; Seed = @{ AdminPassword = $admin; CustomerPassword = $customer }; AI_PROVIDER = 'Gemini'; AI_API_KEY = ''; AI_MODEL = '' }
$config | ConvertTo-Json -Depth 5 | Set-Content -Encoding UTF8 $file
Write-Host "Da tao cau hinh local voi port $Port. Secret khong duoc in ra terminal."
Write-Host 'Tai khoan: admin@booknest.local / customer@booknest.local. Xem mat khau trong Seed cua file local bang VS Code.'