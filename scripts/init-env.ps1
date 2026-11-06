$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$targetPath = Join-Path $repositoryRoot '.env'

if (Test-Path -LiteralPath $targetPath)
{
    $confirmation = Read-Host '.env already exists. Replace it? [y/N]'
    if ($confirmation -ne 'y')
    {
        Write-Host '.env was not changed.'
        exit 0
    }
}

$environmentName = (Read-Host 'Environment name (dev, prod, etc)').Trim()
if ($environmentName -notmatch '^[a-zA-Z0-9][a-zA-Z0-9._-]*$')
{
    throw 'Enter an environment name, such as dev or prod.'
}

$sourcePath = Join-Path $repositoryRoot ".env.$environmentName"
if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf))
{
    throw "Environment file does not exist: .env.$environmentName"
}

Copy-Item -LiteralPath $sourcePath -Destination $targetPath -Force
Write-Host ".env created from .env.$environmentName."
