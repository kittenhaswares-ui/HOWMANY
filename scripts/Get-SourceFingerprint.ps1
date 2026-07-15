param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$sourceRoots = @(
    (Join-Path $RepositoryRoot 'src/HOWMANY.Core'),
    (Join-Path $RepositoryRoot 'src/HOWMANY.Plugin')
)

$lines = foreach ($root in $sourceRoots) {
    Get-ChildItem -LiteralPath $root -Recurse -File |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } |
        ForEach-Object {
            $relative = [System.IO.Path]::GetRelativePath($RepositoryRoot, $_.FullName).Replace('\', '/')
            $fileHash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
            "$relative`:$fileHash"
        }
}

$canonical = ($lines | Sort-Object) -join "`n"
$bytes = [System.Text.Encoding]::UTF8.GetBytes($canonical)
$fingerprint = [System.Security.Cryptography.SHA256]::HashData($bytes)
[Convert]::ToHexString($fingerprint).ToLowerInvariant()
