$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$config = Get-Content (Join-Path $root 'backend\BookNest.Api\appsettings.Local.json') -Raw | ConvertFrom-Json
$key = $env:AI_API_KEY; if (!$key) { $key = $config.AI_API_KEY }
if (!$key) { throw 'Nhap AI_API_KEY vao file local hoac bien moi truong, khong gui vao chat.' }
try {
    $next = ''; do {
        $uri = 'https://generativelanguage.googleapis.com/v1beta/models?pageSize=1000'
        if ($next) { $uri += '&pageToken=' + [uri]::EscapeDataString($next) }
        $data = Invoke-RestMethod -Uri $uri -Headers @{ 'x-goog-api-key' = $key }
        $data.models | Where-Object { $_.supportedGenerationMethods -contains 'generateContent' } | Select-Object name,displayName
        $next = $data.nextPageToken
    } while ($next)
} catch { Write-Error 'Khong lay duoc danh sach model. Kiem tra key, quyen va ket noi. Khong in response chua thong tin nhay cam.' }