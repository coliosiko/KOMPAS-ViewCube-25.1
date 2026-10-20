# Creates a development-only key. Never use this key for official releases.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$keyDirectory = Join-Path (Split-Path $PSScriptRoot -Parent) '.keys'
$keyPath = Join-Path $keyDirectory 'developer.snk'
if (Test-Path -LiteralPath $keyPath) {
    Write-Host 'Existing development key retained.'
    exit 0
}
New-Item -ItemType Directory -Path $keyDirectory -Force | Out-Null
$parameters = New-Object System.Security.Cryptography.CspParameters
$parameters.KeyNumber = [int][System.Security.Cryptography.KeyNumber]::Signature
$rsa = New-Object System.Security.Cryptography.RSACryptoServiceProvider -ArgumentList 2048, $parameters
$rsa.PersistKeyInCsp = $false
try {
    $keyData = $rsa.ExportCspBlob($true)
    $stream = [System.IO.File]::Open($keyPath, [System.IO.FileMode]::CreateNew)
    try { $stream.Write($keyData, 0, $keyData.Length) } finally { $stream.Dispose() }
} finally { $rsa.Dispose() }
Write-Host 'Development key created in ignored .keys directory.'
